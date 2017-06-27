using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Amqp;
using Amqp.Framing;
using NMS.AMQP.Util;
using System.Reflection;
using Apache.NMS.Util;

namespace NMS.AMQP
{
    using Message.Factory;
    enum ConnectionState
    {
        UNKNOWN = -1,
        INITIAL = 0,
        CONNECTING = 1,
        CONNECTED = 2,
        CLOSING = 3,
        CLOSED = 4,

    }

    class Connection : NMSResource, IConnection
    {
        private IRedeliveryPolicy redeliveryPolicy;
        private Amqp.Connection impl;
        private ProviderCreateConnection implCreate;
        private ConnectionInfo connInfo;
        private readonly IdGenerator clientIdGenerator;
        private Atomic<bool> clientIdCanSet = new Atomic<bool>(true);
        private Atomic<bool> starting = new Atomic<bool>(false);
        private Atomic<bool> started = new Atomic<bool>(false);
        private Atomic<bool> closing = new Atomic<bool>(false);
        private Atomic<ConnectionState> state = new Atomic<ConnectionState>(ConnectionState.INITIAL);
        private CountDownLatch latch;
        private ConcurrentDictionary<Id, Session> sessions = new ConcurrentDictionary<Id, Session>();
        private IdGenerator sesIdGen = null;
        private StringDictionary properties;
        private Id clientId;
        
        #region Contructor

        internal Connection(Uri addr, IdGenerator clientIdGenerator)
        {
            connInfo = new ConnectionInfo();
            connInfo.remoteHost = addr;
            this.clientIdGenerator = clientIdGenerator;
            latch = new CountDownLatch(1);
            Console.WriteLine("Registering Connection as Message Factory.");
            MessageFactory.Register(this);
        }

        #endregion

        #region Internal Properties

        internal Amqp.Connection innerConnection { get { return this.impl; } }

        internal IdGenerator SessionIdGenerator
        {
            get
            {
                IdGenerator sig = sesIdGen;
                lock (this)
                {
                    if (sig == null)
                    {
                        sig = new NestedIdGenerator("ID:ses", clientId, true);
                        sesIdGen = sig;
                    }
                }
                return sig;
            }
        }

        internal bool IsConnected
        {
            get
            {
                return this.state.Value.Equals(ConnectionState.CONNECTED);
            }
        }

        internal bool IsClosed
        {
            get
            {
                return this.state.Value.Equals(ConnectionState.CLOSED);
            }
        }

        internal ushort MaxChannel
        {
            get { return connInfo.channelMax; }
        }

        internal MessageTransformation TransformFactory
        {
            get
            {
                return MessageFactory.Instance(this).GetTransformFactory();
            }
        }

        #endregion

        #region Internal Methods

        internal void configure(ConnectionFactory cf)
        {
            // get properties from connection factory
            StringDictionary properties = cf.ConnectionProperties;
            StringDictionary AMQPProps = PropertyUtil.GetProperties(cf.impl.AMQP);
            StringDictionary TCPProps = PropertyUtil.GetProperties(cf.impl.TCP);

            // apply user properties last 
            PropertyUtil.SetProperties(connInfo, AMQPProps);
            PropertyUtil.SetProperties(connInfo, TCPProps);
            PropertyUtil.SetProperties(connInfo, properties);

            // Store raw properties for future objects
            this.properties = PropertyUtil.Clone(properties);
            
            this.implCreate = cf.impl.CreateAsync;
            this.consumerTransformer = cf.ConsumerTransformer;
            this.producerTransformer = cf.ProducerTransformer;

        }

        internal StringDictionary Properties
        {
            get { return PropertyUtil.Merge(this.properties, PropertyUtil.GetProperties(this.connInfo)); }
        }

        internal void Remove(Session ses)
        {
            Session result = null;
            if(!sessions.TryRemove(ses.Id, out result))
            {
                Tracer.WarnFormat("Could not disassociate Session {0} with Connection {0}.", ses.Id, ClientId);
            }
        }

        private void checkIfClosed()
        {
            if (this.state.Value.Equals(ConnectionState.CLOSED))
            {
                throw new IllegalStateException("Operation invalid on closed connection.");
            }
        }

        private void openResponse(Amqp.Connection conn, Open openResp)
        {
            Tracer.InfoFormat("Connection {0}, Open {0}", conn.ToString(), openResp.ToString());
            Tracer.DebugFormat("Open Response : \n Hostname = {0},\n ContainerId = {1},\n MaxChannel = {2},\n MaxFrame = {3}\n", openResp.HostName, openResp.ContainerId, openResp.ChannelMax, openResp.MaxFrameSize);
            Tracer.DebugFormat("Open Response Descriptor : \n Descriptor Name = {0},\n Descriptor Code = {1}\n", openResp.Descriptor.Name, openResp.Descriptor.Code);
            
            if (SymbolUtil.CheckAndCompareFields(openResp.Properties, SymbolUtil.CONNECTION_ESTABLISH_FAILED, SymbolUtil.BOOLEAN_TRUE))
            {
                Tracer.InfoFormat("Open response contains {0} property the connection {1} will soon be closed.", SymbolUtil.CONNECTION_ESTABLISH_FAILED, this.ClientId);
            }
            else
            {
                this.latch.countDown();
            }
        }

        private Open createOpenFrame(ConnectionInfo connInfo)
        {
            Open frame = new Open();
            frame.ContainerId = connInfo.clientId;
            frame.ChannelMax = connInfo.channelMax;
            frame.MaxFrameSize = Convert.ToUInt32(connInfo.maxFrameSize);
            frame.HostName = connInfo.remoteHost.Host;
            frame.IdleTimeOut = Convert.ToUInt32(connInfo.idleTimout);
            frame.DesiredCapabilities = new Amqp.Types.Symbol[] {
                SymbolUtil.OPEN_CAPABILITY_SOLE_CONNECTION_FOR_CONTAINER,
                SymbolUtil.OPEN_CAPABILITY_DELAYED_DELIVERY
            };

            return frame;
        }

        internal void connect()
        {
            if (this.state.CompareAndSet(ConnectionState.INITIAL, ConnectionState.CONNECTING))
            {
                Address addr = UriUtil.ToAddress(connInfo.remoteHost, connInfo.username, connInfo.password);
                Tracer.InfoFormat("Creating Address: {0}", addr.Host);
                if (this.clientIdCanSet.CompareAndSet(true, false))
                {
                    if (this.ClientId == null)
                    {
                        clientId = this.clientIdGenerator.GenerateId();
                        connInfo.clientId = clientId.ToString();
                    }
                    else
                    {
                        clientId = new Id(ClientId);
                    }
                    Tracer.InfoFormat("Staring Connection with Client Id : {0}", this.ClientId);
                }
                Open openFrame = createOpenFrame(this.connInfo);
                
                Task<Amqp.Connection> fconn = this.implCreate(addr, openFrame, this.openResponse);

                this.impl = TaskUtil.Wait(fconn, connInfo.connectTimeout);
                this.impl.Closed += OnInternalClosed;
                this.latch = new CountDownLatch(1);

                ConnectionState finishedState = ConnectionState.UNKNOWN;
                // Wait for Open response 
                try
                {
                    bool received = this.latch.await(TimeSpan.FromMilliseconds(this.connInfo.connectTimeout));
                    if (received && this.impl.Error == null && fconn.Exception == null)
                    {

                        Tracer.InfoFormat("Connection {0} has connected.", this.impl.ToString());
                        finishedState = ConnectionState.CONNECTED;

                    }
                    else
                    {
                        if (!received)
                        {
                            // Timeout occured waiting on response
                            Tracer.ErrorFormat("Connection Response Timeout. Failed to receive response from {0} in {1}ms", addr.Host, connInfo.connectTimeout);
                        }
                        finishedState = ConnectionState.INITIAL;
                        
                        if (fconn.Exception == null)
                        {
                            Tracer.ErrorFormat("Connection {0} has Failed to connect. Message: {1}", ClientId, (this.impl.Error == null ? "Unknown" : this.impl.Error.ToString()));
                            throw ExceptionSupport.GetException(this.impl, "Connection {0} has failed to connect.", ClientId);
                        }
                        else
                        {
                            throw ExceptionSupport.Wrap(fconn.Exception, "Connection {0} failed to connect.", ClientId);
                        }

                    }
                }
                finally
                {
                    this.latch = null;
                    this.state.GetAndSet(finishedState);
                    if (finishedState != ConnectionState.CONNECTED)
                    {
                        this.impl.Close(connInfo.closeTimeout);
                    }
                }
            }
        }

        private void OnInternalClosed(AmqpObject sender, Error error)
        {
            if( sender is Amqp.Connection)
            {
                if(error != null)
                {
                    Amqp.Connection conn = sender as Amqp.Connection;
                    if (conn.Equals(this.impl))
                    {
                        Tracer.ErrorFormat("Connection {0} closed unexpectedly with error : {1}", ClientId, error.ToString());
                        if(this.ConnectionInterruptedListener != null)
                        {
                            this.ConnectionInterruptedListener();
                        }
                        
                        if (this.latch != null)
                        {
                            this.latch.countDown();
                        }
                    }
                }
            }
        }

        private void disconnect()
        {
            if(this.state.CompareAndSet(ConnectionState.CONNECTED, ConnectionState.CLOSING) && this.impl!=null && !this.impl.IsClosed)
            {
                this.impl.Close(connInfo.closeTimeout);
                this.state.GetAndSet(ConnectionState.CLOSED);
            }
        }

        internal void OnException(Exception ex)
        {
            
            if (ExceptionListener != null)
            {
                if (ex is NMSException)
                {
                    ExceptionListener(ex);
                }
                else
                {
                    ExceptionListener(ExceptionSupport.Wrap(ex));
                }
            }
        }
        
        #endregion

        #region IConnection methods

        AcknowledgementMode acknowledgementMode = AcknowledgementMode.AutoAcknowledge;
        public AcknowledgementMode AcknowledgementMode
        {
            get { return acknowledgementMode; }
            set { acknowledgementMode = value; }
        }
        
        public string ClientId
        {
            get { return connInfo.clientId; }
            set
            {
                if (this.clientIdCanSet.Value)
                {
                    if (value != null && value.Length > 0)
                    {
                        connInfo.clientId = value;
                        try
                        {
                            this.connect();
                        }
                        catch (NMSException nms)
                        {
                            NMSException ex = nms;
                            if (nms.Message.Contains("invalid-field:container-id"))
                            {
                                ex = new InvalidClientIDException(nms.Message);
                            }
                            throw ex;
                        }
                    }
                }
                else
                {
                    throw new InvalidClientIDException("Client Id can not be set after connection is Started.");
                }
            }
        }

        private ConsumerTransformerDelegate consumerTransformer;
        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.consumerTransformer; }
            set { this.consumerTransformer = value; }
        }

        private ProducerTransformerDelegate producerTransformer;
        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return producerTransformer; }
            set { this.producerTransformer = value; }
        }
        
        public IConnectionMetaData MetaData
        {
            get
            {
                return ConnectionMetaData.Version;
            }
        }

        public IRedeliveryPolicy RedeliveryPolicy
        {
            get { return this.redeliveryPolicy; }
            set
            {
                if (value != null)
                {
                    this.redeliveryPolicy = value;
                }
            }
        }

        public TimeSpan RequestTimeout
        {
            get
            {
                return TimeSpan.FromMilliseconds(this.connInfo.requestTimeout);
            }

            set
            {
                connInfo.requestTimeout = Convert.ToInt64(value.TotalMilliseconds);
            }
        }

        public event ConnectionInterruptedListener ConnectionInterruptedListener;
        public event ConnectionResumedListener ConnectionResumedListener;
        public event ExceptionListener ExceptionListener;

        public ISession CreateSession()
        {
            return CreateSession(acknowledgementMode);
        }

        public ISession CreateSession(AcknowledgementMode acknowledgementMode)
        {
            this.checkIfClosed();
            this.connect();
            Session ses = new Session(this);
            this.sessions.TryAdd(ses.Id, ses);
            return ses;
        }

        public void PurgeTempDestinations()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            Tracer.DebugFormat("Closing of Connection {0}", ClientId);
            this.started.GetAndSet(false);
            if (this.closing.CompareAndSet(false, true))
            {
                foreach(ISession s in sessions.Values)
                {
                    s.Close();
                }
                this.disconnect();
                if (this.state.Value.Equals(ConnectionState.CLOSED))
                {
                    this.impl = null;
                }
            }
        }

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this.Close();
        }

        #endregion

        #region NMSResource Methods

        protected override void throwIfClosed()
        {
            this.checkIfClosed();
        }

        protected override void StartResource()
        {
            this.connect();

            if (!IsConnected)
            {
                throw new NMSConnectionException("Connection Failed to connect to Client.");
            }

            //start sessions here
            foreach (Session s in sessions.Values)
            {
                s.Start();
            }
            
        }

        protected override void StopResource()
        {
            if ( this.impl != null && !this.impl.IsClosed)
            {
                // stop all sessions here.
                foreach (Session s in sessions.Values)
                {
                    s.Stop();
                }
            }
        }

        #endregion

        #region Connection Information inner Class

        protected class ConnectionInfo
        {
            static ConnectionInfo()
            {
                Amqp.ConnectionFactory defaultCF = new Amqp.ConnectionFactory();
                AmqpSettings defaultAMQPSettings = defaultCF.AMQP;
                TcpSettings defaultTCPSettings = defaultCF.TCP;

                DEFAULT_CHANNEL_MAX = defaultAMQPSettings.MaxSessionsPerConnection;
                DEFAULT_MAX_FRAME_SIZE = defaultAMQPSettings.MaxFrameSize;
                DEFAULT_IDLE_TIMEOUT = defaultAMQPSettings.IdleTimeout;

                DEFAULT_SEND_TIMEOUT = defaultTCPSettings.SendTimeout;
                
                DEFAULT_REQUEST_TIMEOUT = Convert.ToInt64(NMSConstants.defaultRequestTimeout.TotalMilliseconds);
                
            }
            public const long INFINITE = -1;
            public const long DEFAULT_CONNECT_TIMEOUT = 15000;
            public const int DEFAULT_CLOSE_TIMEOUT = 15000;
            public static readonly long DEFAULT_SEND_TIMEOUT;
            public static readonly long DEFAULT_REQUEST_TIMEOUT;
            public static readonly long DEFAULT_IDLE_TIMEOUT;

            public static readonly ushort DEFAULT_CHANNEL_MAX;
            public static readonly int DEFAULT_MAX_FRAME_SIZE;

            internal Uri remoteHost { get; set; }
            public string clientId { get; internal set; } = null;
            public string username { get; set; } = null;
            public string password { get; set; } = null;
            
            public long requestTimeout { get; set; } = DEFAULT_REQUEST_TIMEOUT;
            public long connectTimeout { get; set; } = DEFAULT_CONNECT_TIMEOUT;
            public long sendTimeout { get; set; } = DEFAULT_SEND_TIMEOUT;
            public int closeTimeout { get; set; } = DEFAULT_CLOSE_TIMEOUT;
            public long idleTimout { get; set; } = DEFAULT_IDLE_TIMEOUT;

            public ushort channelMax { get; set; } = DEFAULT_CHANNEL_MAX;
            public int maxFrameSize { get; set; } = DEFAULT_MAX_FRAME_SIZE;
            

            public override string ToString()
            {
                string result = "";
                result += "connInfo = [\n";
                foreach (MemberInfo info in this.GetType().GetMembers())
                {
                    if(info is PropertyInfo)
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
