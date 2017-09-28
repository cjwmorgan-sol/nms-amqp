using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using Amqp;
using Amqp.Framing;
using System.Reflection;
using NMS.AMQP.Util;

namespace NMS.AMQP
{

    internal enum LinkState
    {
        UNKNOWN = -1,
        INITIAL = 0,
        ATTACHSENT = 1,
        ATTACHED = 2,
        DETACHSENT = 3,
        DETACHED = 4
    }
    
    /// <summary>
    /// Abstract for AmqpNetLite Amqp.Link container.
    /// This class handles the performative Attach and Detached for the amqp procotol engine.
    /// </summary>
    abstract class MessageLink : NMSResource
    {
        private CountDownLatch responseLatch=null;
        private LinkInfo info;
        private Link impl;
        private Atomic<LinkState> state = new Atomic<LinkState>(LinkState.INITIAL);
        private readonly Session session;
        private readonly Destination destination;

        protected MessageLink(Session ses, Destination dest)
        {
            session = ses;
            destination = dest;
        }
        
        internal virtual Session Session { get { return session; } }

        protected Destination Destination { get { return destination; } }

        protected Link Link
        {
            get { return impl; }
            private set {  }
        }

        protected LinkInfo Info { get { return info; } set { info = value; } }

        protected void Attach()
        {
            if (state.CompareAndSet(LinkState.INITIAL, LinkState.ATTACHSENT))
            {
                responseLatch = new CountDownLatch(1);
                impl = createLink();
                this.Link.Closed += this.onInternalClosed;
                LinkState finishedState = LinkState.UNKNOWN;
                try
                {
                    bool received = responseLatch.await(RequestTimeout);
                    if(received && this.impl.Error == null)
                    {
                        finishedState = LinkState.ATTACHED;
                    }
                    else
                    {
                        finishedState = LinkState.INITIAL;
                        if (!received)
                        {
                            Tracer.InfoFormat("Link {0} Attach timeout", Info.Id);
                            throw ExceptionSupport.GetException(this.impl, "Performative Attach Timeout while waiting for response.");
                        }
                        else
                        {
                            Tracer.InfoFormat("Link {0} Begin error: {1}", Info.Id, this.impl.Error);
                            throw ExceptionSupport.GetException(this.impl, "Performative Attach Error.");
                        }
                    }


                }
                finally
                {
                    responseLatch = null;
                    state.GetAndSet(finishedState);
                    if(!state.Value.Equals(LinkState.ATTACHED) && !this.impl.IsClosed)
                    {
                        this.impl.Close();
                    }
                }
                
            }
        }

        protected void Detach()
        {
            if (state.CompareAndSet(LinkState.ATTACHED, LinkState.DETACHSENT))
            {
                this.impl.Close(info.closeTimeout);
                state.GetAndSet(LinkState.DETACHED);
            }
        }

        protected abstract void onInternalClosed(Amqp.AmqpObject sender, Error error);

        protected abstract Link createLink();

        protected virtual void OnResponse()
        {
            if (responseLatch != null)
            {
                responseLatch.countDown();
            }
        }
        

        #region NMSResource Methhods

        protected override void StartResource()
        {
            Attach();
        }
        
        protected override void throwIfClosed()
        {
            if (state.Value.Equals(LinkState.DETACHED))
            {
                throw new Apache.NMS.IllegalStateException("Illegal operation on closed IMessageProducer.");
            }
        }

        #endregion

        #region Public Inheritable Properties

        public TimeSpan RequestTimeout
        {
            get { return TimeSpan.FromMilliseconds(Info.requestTimeout); }
            set { Info.requestTimeout = Convert.ToInt64(value.TotalMilliseconds); }
        }

        #endregion

        #region Public Inheritable Methods

        public virtual void Close()
        {
            this.Detach();
            if (state.Value.Equals(LinkState.DETACHED) && this.impl!=null && this.impl.IsClosed)
            {
                this.impl = null;
            }
        }

        #endregion

        #region Inner LinkInfo Class

        protected abstract class LinkInfo
        {
            protected static readonly long DEFAULT_REQUEST_TIMEOUT;
            static LinkInfo()
            {
                DEFAULT_REQUEST_TIMEOUT = Convert.ToInt64(NMSConstants.defaultRequestTimeout.TotalMilliseconds);
            }

            private readonly Id id;

            protected LinkInfo(Id linkId)
            {
                id = linkId;
            }

            public long requestTimeout { get; set; } = DEFAULT_REQUEST_TIMEOUT;
            public int closeTimeout { get; set; }
            public long sendTimeout { get; set; }

            public Id Id { get { return id; } }
            
            public override string ToString()
            {
                string result = "";
                result += "LinkInfo = [\n";
                foreach (MemberInfo info in this.GetType().GetMembers())
                {
                    if (info is PropertyInfo)
                    {
                        PropertyInfo prop = info as PropertyInfo;
                        if (prop.GetGetMethod(true).IsPublic)
                        {
                            result += string.Format("{0} = {1},\n", prop.Name, prop.GetValue(this));
                        }
                    }
                }
                result = result.Substring(0, result.Length - 2) + "\n]";
                return result;
            }

        }

        #endregion
    }
}
