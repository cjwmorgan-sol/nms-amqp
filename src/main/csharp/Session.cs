using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Apache.NMS;
using Apache.NMS.Util;
using Amqp.Framing;
using NMS.AMQP.Util;
using NMS.AMQP.Message;
using NMS.AMQP.Message.Factory;


namespace NMS.AMQP
{
    enum SessionState
    {
        UNKNOWN,
        INITIAL,
        BEGINSENT,
        OPENED,
        ENDSENT,
        CLOSED
    }

    /// <summary>
    /// NMS.AMQP.Session facilitates management and creates the underlying Amqp.Session protocol engine object.
    /// NMS.AMQP.Session is also a Factory for NMS.AMQP.MessageProcuder, NMS.AMQP.MessageConsumer, NMS.AMQP.Message.Message, NMS.AMQP.Destination, etc.
    /// </summary>
    class Session : NMSResource, ISession
    {

        private Connection connection;
        private Amqp.Session impl;
        private Dictionary<string, MessageConsumer> consumers;
        private Dictionary<string, MessageProducer> producers;
        private SessionInfo sessInfo;
        private CountDownLatch responseLatch;
        private Atomic<SessionState> state = new Atomic<SessionState>(SessionState.INITIAL);
        private MessageDispatcher dispatcher;
        private readonly IdGenerator prodIdGen;
        private readonly IdGenerator consIdGen;

        #region Constructor

        internal Session(Connection conn)
        {
            consumers = new Dictionary<string, MessageConsumer>();
            producers = new Dictionary<string, MessageProducer>();
            sessInfo = new SessionInfo(conn.SessionIdGenerator.GenerateId());
            dispatcher = new MessageDispatcher();
            this.configure(conn);
            prodIdGen = new NestedIdGenerator("ID:producer", this.sessInfo.Id, true);
            consIdGen = new NestedIdGenerator("ID:consumer", this.sessInfo.Id, true);

            this.Begin();
        }

        #endregion

        #region Internal Properties

        internal Connection Connection { get { return connection; } }

        internal IdGenerator ProducerIdGenerator
        {
            get { return prodIdGen; }
        }

        internal IdGenerator ConsumerIdGenerator
        {
            get { return consIdGen; }
        }

        internal Amqp.Session InnerSession { get { return this.impl; } }

        internal Id Id { get { return sessInfo.Id; } }

        internal string SessionId
        {
            get
            {
                return sessInfo.sessionId;
            }
        }

        internal StringDictionary Properties
        {
            get { return PropertyUtil.GetProperties(this.sessInfo); }
        }

        private object ThisProducerLock { get { return producers; } }
        private object ThisConsumerLock { get { return consumers; } }

        #endregion

        #region Internal/Private Methods

        internal void configure(Connection conn)
        {
            this.connection = conn;
            
            PropertyUtil.SetProperties(this.sessInfo, conn.Properties);
            AcknowledgementMode = conn.AcknowledgementMode;
            this.RequestTimeout = conn.RequestTimeout;
            sessInfo.maxHandle = conn.MaxChannel;
            
        }
        
        internal void OnException(Exception e)
        {
            Connection.OnException(e);
        }

        private void Begin()
        {
            if (this.connection.IsConnected && this.state.CompareAndSet(SessionState.INITIAL, SessionState.BEGINSENT))
            {
                this.responseLatch = new CountDownLatch(1);
                this.impl = new Amqp.Session(this.connection.innerConnection as Amqp.Connection, this.createBeginFrame(), this.onBeginResp);
                impl.Closed += this.onInternalClosed;
                SessionState finishedState = SessionState.UNKNOWN;
                try
                {
                    bool received = this.responseLatch.await(TimeSpan.FromMilliseconds(sessInfo.sendTimeout));
                    if (received && this.impl.Error == null)
                    {
                        finishedState = SessionState.OPENED;
                    }
                    else
                    {
                        finishedState = SessionState.INITIAL;
                        if (!received)
                        {
                            Tracer.InfoFormat("Session {0} Begin timeout in {1}ms", sessInfo.nextOutgoingId, sessInfo.sendTimeout);
                            throw ExceptionSupport.GetTimeoutException(this.impl, "Performative Begin Timeout while waiting for response.");
                        }
                        else 
                        {
                            Tracer.InfoFormat("Session {0} Begin error: {1}", sessInfo.nextOutgoingId, this.impl.Error);
                            throw ExceptionSupport.GetException(this.impl, "Performative Begin Error.");
                        }
                    }
                }
                finally
                {
                    this.responseLatch = null;
                    this.state.GetAndSet(finishedState);
                    if(finishedState != SessionState.OPENED && !this.impl.IsClosed)
                    {
                        this.impl.Close();
                    }

                }
            }
        }

        private void End()
        {
            if(this.impl!=null && !this.impl.IsClosed && this.state.CompareAndSet(SessionState.OPENED, SessionState.ENDSENT))
            {
                this.dispatcher.Close();

                lock (ThisProducerLock)
                {
                    foreach (MessageProducer p in producers.Values)
                    {
                        p.Close();
                    }
                }
                lock (ThisConsumerLock)
                {
                    foreach (MessageConsumer c in consumers.Values)
                    {
                        //c.Close();
                    }
                }

                this.impl.Close(this.sessInfo.closeTimeout,null);
                
                this.state.GetAndSet(SessionState.CLOSED);
            }
        }

        private Begin createBeginFrame()
        {
            Begin begin = new Begin();
            
            begin.HandleMax = this.sessInfo.maxHandle;
            begin.IncomingWindow = this.sessInfo.incomingWindow;
            begin.OutgoingWindow = this.sessInfo.outgoingWindow;
            begin.NextOutgoingId = this.sessInfo.nextOutgoingId;

            return begin;
        }

        private void onBeginResp(Amqp.Session session, Begin resp)
        {
            Tracer.DebugFormat("Received Begin for Session {0}, Response: {1}", session, resp);
            
            this.sessInfo.remoteChannel = resp.RemoteChannel;
            this.responseLatch.countDown();
            
        }

        private void onInternalClosed(Amqp.AmqpObject sender, Error error)
        {
            if (error != null)
            {
                Tracer.ErrorFormat("Session Unexpectedly closed with error: {0}", error);
                if(this.responseLatch != null)
                {
                    this.responseLatch.countDown();
                }   
            }
        }

        #endregion

        #region NMSResource Methods

        protected override void ThrowIfClosed()
        {
            if (state.Value.Equals(SessionState.CLOSED))
            {
                throw new IllegalStateException("Invalid Operation on Closed session.");
            }
        }

        protected override void StartResource()
        {
            this.Begin();
            // start all producers and consumers here
            lock (ThisProducerLock)
            {
                foreach (MessageProducer p in producers.Values)
                {
                    p.Start();
                }
            }
            lock (ThisConsumerLock)
            {
                foreach (MessageConsumer c in consumers.Values)
                {
                    //c.Start();
                }
            }
            dispatcher.Start();
        }

        protected override void StopResource()
        {
            // stop all producers and consumers here
            dispatcher.Stop();
        }

        #endregion

        #region ISession Property Fields

        public AcknowledgementMode AcknowledgementMode
        {
            get
            {
                return sessInfo.ackMode;
            }
            internal set
            {
                if(value.Equals(AcknowledgementMode.Transactional))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    sessInfo.ackMode = value;
                }
            }
        }

        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ProducerTransformerDelegate ProducerTransformer
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan RequestTimeout
        {
            get
            {
                return TimeSpan.FromMilliseconds(this.sessInfo.requestTimeout);
            }

            set
            {
                sessInfo.requestTimeout = Convert.ToInt64(value.TotalMilliseconds);
            }
        }

        public bool Transacted
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region ISession Events

        public event SessionTxEventDelegate TransactionCommittedListener;
        public event SessionTxEventDelegate TransactionRolledBackListener;
        public event SessionTxEventDelegate TransactionStartedListener;

        #endregion

        #region ISession Methods

        public void Close()
        {
            bool wasClosed = this.state.Value.Equals(SessionState.CLOSED);
            this.End();
            if (!wasClosed && this.state.Value.Equals(SessionState.CLOSED))
            {
                Connection.Remove(this);
                this.impl = null;
            }
        }
        
        public IQueueBrowser CreateBrowser(IQueue queue)
        {
            throw new NotImplementedException();
        }

        public IQueueBrowser CreateBrowser(IQueue queue, string selector)
        {
            throw new NotImplementedException();
        }

        public IBytesMessage CreateBytesMessage()
        {
            ThrowIfClosed();
            return MessageFactory.Instance(Connection).CreateBytesMessage();
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            IBytesMessage msg = CreateBytesMessage();
            msg.WriteBytes(body);
            return msg;
        }

        public IMessageConsumer CreateConsumer(IDestination destination)
        {
            return CreateConsumer(destination, null);
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector)
        {
            return CreateConsumer(destination, selector, true);
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
        {
            this.ThrowIfClosed();
            return new MessageConsumer(this, destination);
        }

        public IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal)
        {
            this.ThrowIfClosed();
            throw new NotImplementedException();
        }

        public IMapMessage CreateMapMessage()
        {
            this.ThrowIfClosed();
            return MessageFactory.Instance(Connection).CreateMapMessage();
        }

        public IMessage CreateMessage()
        {
            this.ThrowIfClosed();
            return MessageFactory.Instance(Connection).CreateMessage();
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            this.ThrowIfClosed();
            return MessageFactory.Instance(Connection).CreateObjectMessage(body);
        }

        public IMessageProducer CreateProducer()
        {
            return CreateProducer(null);
        }

        public IMessageProducer CreateProducer(IDestination destination)
        {
            ThrowIfClosed();
            MessageProducer prod = new MessageProducer(this, destination);
            lock (ThisProducerLock)
            {
                //Todo Fix adding multiple producers
                producers.Add(prod.ProducerId.ToString(), prod);
            }
            if (IsStarted)
            {
                prod.Start();
            }
            return prod;
        }

        public IStreamMessage CreateStreamMessage()
        {
            this.ThrowIfClosed();
            return MessageFactory.Instance(Connection).CreateStreamMessage();
        }

        public ITemporaryQueue CreateTemporaryQueue()
        {
            this.ThrowIfClosed();
            return new TemporaryQueue(Connection);
        }

        public ITemporaryTopic CreateTemporaryTopic()
        {
            this.ThrowIfClosed();
            return new TemporaryTopic(Connection);
        }

        public ITextMessage CreateTextMessage()
        {
            ThrowIfClosed();
            return MessageFactory.Instance(Connection).CreateTextMessage();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            ITextMessage msg = CreateTextMessage();
            msg.Text = text;
            return msg;
        }

        public void DeleteDestination(IDestination destination)
        {
            throw new NotImplementedException();
        }

        public void DeleteDurableConsumer(string name)
        {
            throw new NotImplementedException();
        }

        public IQueue GetQueue(string name)
        {
            this.ThrowIfClosed();
            return new Queue(Connection, name);
        }

        public ITopic GetTopic(string name)
        {
            this.ThrowIfClosed();
            return new Topic(Connection, name);
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public void Recover()
        {
            throw new NotImplementedException();
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this.Close();
        }

        #endregion

        #region SessionInfo Inner Class

        protected class SessionInfo
        {
            private static readonly uint DEFAULT_INCOMING_WINDOW;
            private static readonly uint DEFAULT_OUTGOING_WINDOW;

            static SessionInfo()
            {
                DEFAULT_INCOMING_WINDOW = 1024 * 2 -1;
                DEFAULT_OUTGOING_WINDOW = uint.MaxValue - 2u;
            }
            
            private readonly Id sesid;
            internal SessionInfo(Id sessionId)
            {
                sesid = sessionId;
                ulong endId = (ulong)sessionId.GetLastComponent(typeof(ulong));
                nextOutgoingId = Convert.ToUInt16(endId);
            }

            internal Id Id { get { return sesid; } }

            public string sessionId { get { return sesid.ToString(); } }
            
            public AcknowledgementMode ackMode { get; set; }
            public ushort remoteChannel { get; set; }
            public uint nextOutgoingId { get; set; }
            public uint incomingWindow { get; set; } = DEFAULT_INCOMING_WINDOW;
            public uint outgoingWindow { get; set; } = DEFAULT_OUTGOING_WINDOW;
            public uint maxHandle { get; set; }
            public bool isTransacted { get { return false; } set { } }
            public long requestTimeout { get; set; }
            public int closeTimeout { get; set; }
            public long sendTimeout { get; set; }

            public override string ToString()
            {
                string result = "";
                result += "sessInfo = [\n";
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
