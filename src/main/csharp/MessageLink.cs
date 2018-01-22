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
using System.Collections.Specialized;

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

    internal enum TerminusDurability
    {
        NONE = 0,
        CONFIGURATION = 1,
        UNSETTLED_STATE = 2,
    }
    
    /// <summary>
    /// Abstract for AmqpNetLite Amqp.Link container.
    /// This class handles the performative Attach and Detached for the amqp procotol engine.
    /// </summary>
    abstract class MessageLink : NMSResource<LinkInfo>
    {
        private CountDownLatch responseLatch=null;
        private ILink impl;
        private Atomic<LinkState> state = new Atomic<LinkState>(LinkState.INITIAL);
        private readonly Session session;
        private readonly IDestination destination;
        private System.Threading.ManualResetEvent PerformativeOpenEvent = new System.Threading.ManualResetEvent(false);

        protected MessageLink(Session ses, Destination dest)
        {
            session = ses;
            destination = dest;
        }

        protected MessageLink(Session ses, IDestination dest)
        {
            session = ses;
            if(dest is Destination || dest == null)
            {
                destination = dest as Destination;
            }
            else
            {
                if (!dest.IsTemporary)
                {
                    if(dest.IsQueue)
                    {
                        destination = Session.GetQueue((dest as IQueue).QueueName) as Destination;
                    }
                    else
                    {
                        destination = Session.GetQueue((dest as ITopic).TopicName) as Destination;
                    }
                    
                }
                else
                {
                    throw new NotImplementedException("Foreign temporary Destination Implementation Not Supported.");
                }
            }
            
        }

        internal virtual Session Session { get { return session; } }
        
        protected IDestination Destination { get { return destination; } }

        protected ILink Link
        {
            get { return impl; }
            private set {  }
        }
        

        internal bool IsClosing { get { return state.Value.Equals(LinkState.DETACHSENT); } }

        internal bool IsClosed { get { return state.Value.Equals(LinkState.DETACHED); } }

        protected bool IsConfigurable { get { return state.Value.Equals(LinkState.INITIAL); } }

        protected virtual void Attach()
        {
            if (state.CompareAndSet(LinkState.INITIAL, LinkState.ATTACHSENT))
            {
                PerformativeOpenEvent.Reset();
                responseLatch = new CountDownLatch(1);
                impl = CreateLink();
                this.Link.AddClosedCallback(this.OnInternalClosed);
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
                            Tracer.InfoFormat("Link {0} Attach error: {1}", Info.Id, this.impl.Error);
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
                    PerformativeOpenEvent.Set();
                }
                
            }
        }

        protected virtual void Detach()
        {
            if (state.CompareAndSet(LinkState.ATTACHED, LinkState.DETACHSENT))
            {
                this.impl.Close(TimeSpan.FromMilliseconds(Info.closeTimeout), null);
                state.GetAndSet(LinkState.DETACHED);
            }
            else if (state.CompareAndSet(LinkState.INITIAL, LinkState.DETACHED))
            {
                // Link has not been established yet set state to dettached.
            }
            else if (state.Value.Equals(LinkState.ATTACHSENT))
            {
                // The Message Link is trying to estalish a link. It should wait until the Attach response is processed.
                bool signaled = this.PerformativeOpenEvent.WaitOne(this.RequestTimeout);
                if (signaled)
                {
                    if (state.CompareAndSet(LinkState.ATTACHED, LinkState.DETACHSENT))
                    {
                        // The Attach request completed succesfully establishing a link.
                        // Now Close link.
                        this.impl.Close(TimeSpan.FromMilliseconds(Info.closeTimeout), null);
                        state.GetAndSet(LinkState.DETACHED);
                    }
                    else if (state.CompareAndSet(LinkState.INITIAL, LinkState.DETACHED))
                    {
                        // Failed to establish a link set state to Detached.
                    }
                }
                else
                {
                    // Failed to receive establishment event signal.
                    state.GetAndSet(LinkState.DETACHED);
                }
                    

            }
        }

        protected abstract void OnInternalClosed(Amqp.IAmqpObject sender, Error error);

        protected abstract ILink CreateLink();

        protected virtual void OnResponse()
        {
            if (responseLatch != null)
            {
                responseLatch.countDown();
            }
        }

        protected virtual void Configure()
        {
            StringDictionary connProps = Session.Connection.Properties;
            StringDictionary sessProps = Session.Properties;
            PropertyUtil.SetProperties(Info, connProps);
            PropertyUtil.SetProperties(Info, sessProps);

        }


        #region NMSResource Methhods

        protected override void StartResource()
        {
            Attach();
        }
        
        protected override void ThrowIfClosed()
        {
            if (state.Value.Equals(LinkState.DETACHED))
            {
                throw new Apache.NMS.IllegalStateException("Illegal operation on closed I" + this.GetType().Name + ".");
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

        
    }

    #region LinkInfo Class

    internal abstract class LinkInfo : ResourceInfo
    {
        protected static readonly long DEFAULT_REQUEST_TIMEOUT;
        static LinkInfo()
        {
            DEFAULT_REQUEST_TIMEOUT = Convert.ToInt64(NMSConstants.defaultRequestTimeout.TotalMilliseconds);
        }

        protected LinkInfo(Id linkId) : base(linkId)
        {

        }

        public long requestTimeout { get; set; } = DEFAULT_REQUEST_TIMEOUT;
        public int closeTimeout { get; set; }
        public long sendTimeout { get; set; }

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
                        result += string.Format("{0} = {1},\n", prop.Name, prop.GetValue(this, null));
                    }
                }
            }
            result = result.Substring(0, result.Length - 2) + "\n]";
            return result;
        }

    }

    #endregion
}
