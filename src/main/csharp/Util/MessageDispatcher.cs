using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS.Util;
using Apache.NMS;

namespace NMS.AMQP.Util
{
    public delegate void Executable();

    public interface IExecutable
    {
        Executable getExecutable();
        void onFailure(Exception e);
    }

    class MessageDispatchEvent : IExecutable
    {
        private readonly Executable exe;

        internal MessageDispatchEvent(Executable e) { exe = e; }

        public void onFailure(Exception e)
        {

        }

        public Executable getExecutable()
        {
            return exe;
        }
    }

    class MessageDispatcher : NMSResource, IDisposable
    {
        private static AtomicSequence MessageDispatcherId = new AtomicSequence(1);
        private const string MessageDispatcherName = "MessageDispatcher";
        
        private const int DEFAULT_SIZE = 100000;
        private Queue<IExecutable> queue;
        private int maxSize;
        private bool closed=false;
        private Atomic<bool> closeQueued = new Atomic<bool>(false);
        private bool executing=false;
        private Semaphore suspendLock = new Semaphore(0, 1, "Suspend");
        private Thread executingThread;
        private readonly string name;
        private readonly object objLock = new object();

        #region Constructors

        public MessageDispatcher() : this(DEFAULT_SIZE) { }

        public MessageDispatcher(int size)
        {
            this.maxSize = size;
            queue = new Queue<IExecutable>(maxSize);
            executingThread = new Thread(new ThreadStart(this.Dispatch));
            name = MessageDispatcherName + MessageDispatcherId.getAndIncrement();
            executingThread.Name = name;
        }

        #endregion

        #region Properties

        private object thisLock { get { return objLock; } }

        private bool Closing { get { return closeQueued.Value; } }

        #endregion

        #region Private Suspend Resume Methods

        private void Suspend()
        {
            Exception e=null;
            while(!AcquireSuspendLock(out e) && !closed)
            {
                if (e != null)
                {
                    throw e;
                }
            }
        }

        private bool AcquireSuspendLock()
        {
            Exception e;
            return AcquireSuspendLock(out e);
        }

        private bool AcquireSuspendLock(out Exception ex)
        {
            bool signaled = false;
            ex = null;
            try
            {
                signaled = this.suspendLock.WaitOne();
            }catch(Exception e)
            {
                ex = e;
            }

            return signaled;
        }

        private void Resume()
        {
            Exception ex;
            int count = ReleaseSuspendLock(out ex);
            if (ex != null)
            {
                throw ex;
            }
        }

        private int ReleaseSuspendLock()
        {
            Exception e;
            return ReleaseSuspendLock(out e);
        }

        private int ReleaseSuspendLock(out Exception ex)
        {
            ex = null;
            int previous = -1;
            try
            {
                previous = this.suspendLock.Release();
            }
            catch(SemaphoreFullException sfe)
            {
                // ignore multiple resume calls
                // Log for debugging
                Tracer.DebugFormat("Multiple Resume called on running Dispatcher. Cause: {0}", sfe.Message);
            }
            catch(System.IO.IOException ioe)
            {
                Tracer.ErrorFormat("Failed resuming or starting Dispatch thread. Cause: {0}", ioe.Message);
                ex = ioe;
            }
            catch(UnauthorizedAccessException uae)
            {
                Tracer.Error(uae.StackTrace);
                ex =  uae;
            }
            return previous;
        }

        #endregion

        #region Private Dispatch Methods

        private void CloseOnQueue()
        {
            bool ifDrain = false;
            lock (queue)
            {
                if (!closed)
                {
                    Stop();
                    closed = true;
                    executing = false;
                    ifDrain = true;
                    Monitor.PulseAll(queue);
                    Tracer.InfoFormat("MessageDispatcher: {0} Closed.", name);
                }
            }
            if (ifDrain)
            {
                // Drain the rest of the queue before closing
                Drain(true);
            }
        }

        private IExecutable[] DrainOffQueue()
        {
            lock (queue)
            {
                ArrayList list = new ArrayList(queue.Count);
                while (queue.Count > 0)
                {
                    list.Add(queue.Dequeue());
                }
                return (IExecutable[])list.ToArray(typeof(IExecutable));
            }
        }

        private void Drain(bool execute = false)
        {
            IExecutable[] exes = DrainOffQueue();
            if (execute)
            {
                foreach (IExecutable exe in exes)
                {
                    DispatchEvent(exe);
                }
            }
        }

        private void DispatchEvent(IExecutable dispatchEvent)
        {
            Executable exe = dispatchEvent.getExecutable();
            if (exe != null)
            {
                try
                {
                    exe.Invoke();
                }
                catch (Exception e)
                {
                    // connect to exception listener here.

                    Tracer.ErrorFormat("Message Dispatch Error: {0}", e.Message);
                    dispatchEvent.onFailure(e);
                    
                }
            }
        }

        private void Dispatch()
        {
            while (!closed)
            {
                bool locked = false;
                while (!closed && !(locked = this.AcquireSuspendLock())) { }
                if (locked)
                {
                    this.ReleaseSuspendLock();
                }
                if (closed)
                {
                    break;
                }

                while (IsStarted)
                {
                    
                    IExecutable exe;
                    if (TryDequeue(out exe))
                    {
                        
                        DispatchEvent(exe);
                    }
                    else
                    {
                        // queue stopped or timed out
                        Tracer.DebugFormat("Queue {0} did not dispatch due to being Suspended or Closed.", name);
                    }
                }

            }

        }

        #endregion

        #region NMSResource Methods

        protected override void StartResource()
        {
            if (!executing)
            {
                executing = true;
                executingThread.Start();
                Resume();
            }
            else
            {
                Resume();
            }
        }

        protected override void StopResource()
        {
            
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    Monitor.PulseAll(queue);
                }
            }
            Suspend();
        }

        protected override void ThrowIfClosed()
        {
            if (closed)
            {
                throw new Apache.NMS.IllegalStateException("Illegal Operation on closed MesageDispatcher.");
            }
        }
        
        #endregion
        
        #region Public Methods

        public void Close()
        {
            string currentThreadName = Thread.CurrentThread.Name;
            if (currentThreadName != null && currentThreadName.Equals(name))
            {
                // close is called in the Dispatcher Thread so we can just close
                if (false == closeQueued.GetAndSet(true))
                {
                    this.CloseOnQueue();
                }
            }
            else if (closeQueued.CompareAndSet(false, true))
            {
                if (!IsStarted && executing)
                {
                    // resume dispatching thread for Close Message Dispatch Event
                    Start();
                }
                // enqueue close
                this.enqueue(new MessageDispatchEvent(this.CloseOnQueue));

                if (executingThread != null)
                {
                    // thread join must not happen under lock (queue) statement
                    if(!executingThread.ThreadState.Equals(ThreadState.Unstarted))
                        executingThread.Join();
                    executingThread = null;
                }

            }
            
        }

        public void enqueue(IExecutable o)
        {
            if(o == null)
            {
                return;
            }
            lock (queue)
            {
                while (queue.Count >= maxSize)
                {
                    if (closed)
                    {
                        return;
                    }
                    Monitor.Wait(queue);
                }
                queue.Enqueue(o);
                if (queue.Count == 1)
                {
                    Monitor.PulseAll(queue);
                }
                
            }
        }

        public bool TryDequeue(out IExecutable exe, int timeout = -1)
        {
            exe = null;
            lock (queue)
            {
                bool signaled = true;
                while (queue.Count == 0)
                {
                    if (closed || mode.Value.Equals(Resource.Mode.Stopping))
                    {
                        return false;
                    }
                    signaled = (timeout > -1 ) ? Monitor.Wait(queue, timeout) : Monitor.Wait(queue);
                }
                if (!signaled)
                {
                    return false;
                }

                exe = queue.Dequeue();
                if (queue.Count == maxSize - 1)
                {
                    Monitor.PulseAll(queue);
                }
                
            }
            return true;
        }

        #endregion

        #region IDispose Methods

        public void Dispose()
        {
            // remove reference to dispatcher to be garbage collected
            if (executingThread != null && executingThread.ThreadState.Equals(ThreadState.Unstarted))
            {
                executingThread = null;
            } 
            this.Close();
            this.suspendLock.Dispose();
            this.queue = null;
        }

        #endregion
        
    }
}
