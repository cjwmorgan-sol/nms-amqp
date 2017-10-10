using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;

namespace NMS.AMQP
{
    namespace Resource
    {
        public enum Mode
        {
            Stopped,
            Starting,
            Started,
            Stopping
        }
    }

    /// <summary>
    /// NMSResource abstracts the Implementation of IStartable and IStopable for Key NMS class implemetations.
    /// Eg, Connection, Session, MessageConsumer, MessageProducer, etc.
    /// It layouts a foundation for a state machine given by the states in NMS.AMQP.Resource.Mode where 
    /// in general the transitions are Stopped->Starting->Started->Stopping->Stopped->...
    /// </summary>
    internal abstract class NMSResource : IStartable, IStoppable
    {
        protected Atomic<Resource.Mode> mode = new Atomic<Resource.Mode>(Resource.Mode.Stopped);

        public Boolean IsStarted { get { return mode.Value.Equals(Resource.Mode.Started); } }

        protected abstract void StartResource();
        protected abstract void StopResource();
        protected abstract void ThrowIfClosed();

        public void Start()
        {
            ThrowIfClosed();
            if (!IsStarted && mode.CompareAndSet(Resource.Mode.Stopped, Resource.Mode.Starting))
            {
                Resource.Mode finishedMode = Resource.Mode.Stopped;
                try
                {
                    this.StartResource();
                    finishedMode = Resource.Mode.Started;
                }
                catch (Exception e)
                {
                    if(e is NMSException)
                    {
                        throw e;
                    }
                    else
                    {
                        throw new NMSException("Failed to Start resource.", e);
                    }
                }
                finally
                {
                    this.mode.GetAndSet(finishedMode);
                }
            }
        }

        public void Stop()
        {
            ThrowIfClosed();
            if (mode.CompareAndSet(Resource.Mode.Started, Resource.Mode.Stopping))
            {
                Resource.Mode finishedMode = Resource.Mode.Started;
                try
                {
                    this.StopResource();
                    finishedMode = Resource.Mode.Stopped;
                }
                catch (Exception e)
                {
                    if (e is NMSException)
                    {
                        throw e;
                    }
                    else
                    {
                        throw new NMSException("Failed to Stop resource.", e);
                    }
                }
                finally
                {
                    this.mode.GetAndSet(finishedMode);
                }
            }
        }
    }
}
