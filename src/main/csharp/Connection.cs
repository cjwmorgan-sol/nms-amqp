using System;
using System.Collections;
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

    /// <summary>
    /// NMS.AMQP.Connection facilitates management and creates the underlying Amqp.Connection protocol engine object.
    /// NMS.AMQP.Connection is also the NMS.AMQP.Session Factory.
    /// </summary>
    class Connection : NMSResource<ConnectionInfo>, Apache.NMS.IConnection
    {
        public static readonly string MESSAGE_OBJECT_SERIALIZATION_PROP = PropertyUtil.CreateProperty("Message.Serialization");
        public static readonly string REQUEST_TIMEOUT_PROP = PropertyUtil.CreateProperty("RequestTimeout", "Connection");
        private IRedeliveryPolicy redeliveryPolicy;
        private Amqp.IConnection impl;
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
            Info = connInfo;
            this.clientIdGenerator = clientIdGenerator;
            latch = new CountDownLatch(1);
            
        }

        #endregion

        #region Internal Properties

        internal Amqp.IConnection innerConnection { get { return this.impl; } }

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
                return MessageFactory<ConnectionInfo>.Instance(this).GetTransformFactory();
            }
        }

        internal IMessageFactory MessageFactory
        {
            get
            {
                return MessageFactory<ConnectionInfo>.Instance(this);
            }
        }

        internal string TopicPrefix
        {
            get { return connInfo.TopicPrefix; }
        }

        internal string QueuePrefix
        {
            get { return connInfo.QueuePrefix; }
        }

        internal bool IsAnonymousRelay
        {
            get { return connInfo.IsAnonymousRelay; }
        }

        internal bool IsDelayedDelivery
        {
            get { return connInfo.IsDelayedDelivery; }
        }
        
        #endregion

        #region Internal Methods

        internal void Configure(ConnectionFactory cf)
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
                Tracer.WarnFormat("Could not disassociate Session {0} with Connection {1}.", ses.Id, ClientId);
            }
        }

        private void CheckIfClosed()
        {
            if (this.state.Value.Equals(ConnectionState.CLOSED))
            {
                throw new IllegalStateException("Operation invalid on closed connection.");
            }
        }

        private void ProcessCapabilities(Open openResponse)
        {
            if(openResponse.OfferedCapabilities != null || openResponse.OfferedCapabilities.Length > 0)
            {
                foreach(Amqp.Types.Symbol symbol in openResponse.OfferedCapabilities)
                {
                    if (SymbolUtil.OPEN_CAPABILITY_ANONYMOUS_RELAY.Equals(symbol))
                    {
                        connInfo.IsAnonymousRelay = true;
                    }
                    else if (SymbolUtil.OPEN_CAPABILITY_DELAYED_DELIVERY.Equals(symbol))
                    {
                        connInfo.IsDelayedDelivery = true;
                    }
                    else
                    {
                        connInfo.AddCapability(symbol);
                    }
                }
            }
        }

        private void OpenResponse(Amqp.IConnection conn, Open openResp)
        {
            Tracer.InfoFormat("Connection {0}, Open {0}", conn.ToString(), openResp.ToString());
            Tracer.DebugFormat("Open Response : \n Hostname = {0},\n ContainerId = {1},\n MaxChannel = {2},\n MaxFrame = {3}\n", openResp.HostName, openResp.ContainerId, openResp.ChannelMax, openResp.MaxFrameSize);
            Tracer.DebugFormat("Open Response Descriptor : \n Descriptor Name = {0},\n Descriptor Code = {1}\n", openResp.Descriptor.Name, openResp.Descriptor.Code);
            ProcessCapabilities(openResp);
            if (SymbolUtil.CheckAndCompareFields(openResp.Properties, SymbolUtil.CONNECTION_ESTABLISH_FAILED, SymbolUtil.BOOLEAN_TRUE))
            {
                Tracer.InfoFormat("Open response contains {0} property the connection {1} will soon be closed.", SymbolUtil.CONNECTION_ESTABLISH_FAILED, this.ClientId);
            }
            else
            {
                object value = SymbolUtil.GetFromFields(openResp.Properties, SymbolUtil.CONNECTION_PROPERTY_TOPIC_PREFIX);
                if(value != null && value is string)
                {
                    this.connInfo.TopicPrefix = value as string;
                }
                value = SymbolUtil.GetFromFields(openResp.Properties, SymbolUtil.CONNECTION_PROPERTY_QUEUE_PREFIX);
                if (value != null && value is string)
                {
                    this.connInfo.QueuePrefix = value as string;
                }
                this.latch.countDown();
            }
        }

        private Open CreateOpenFrame(ConnectionInfo connInfo)
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

        internal void Connect()
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
                MessageFactory<ConnectionInfo>.Register(this);
                Open openFrame = CreateOpenFrame(this.connInfo);
                
                Task<Amqp.Connection> fconn = this.implCreate(addr, openFrame, this.OpenResponse);

                this.impl = TaskUtil.Wait(fconn, connInfo.connectTimeout);
                this.impl.Closed += OnInternalClosed;
                this.impl.AddClosedCallback(OnInternalClosed);
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
                            Tracer.InfoFormat("Connection Response Timeout. Failed to receive response from {0} in {1}ms", addr.Host, connInfo.connectTimeout);
                        }
                        finishedState = ConnectionState.INITIAL;
                        
                        if (fconn.Exception == null)
                        {
                            if (!received) throw ExceptionSupport.GetTimeoutException(this.impl, "Connection {0} has failed to connect in {1}ms.", ClientId, connInfo.closeTimeout);
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
                        this.impl.Close(TimeSpan.FromMilliseconds(connInfo.closeTimeout),null);
                    }
                }
            }
        }

        private void OnInternalClosed(IAmqpObject sender, Error error)
        {
            
            if( sender is Amqp.Connection)
            {
                Tracer.InfoFormat("Received Close Request for Connection {0}.", this.ClientId);
                if (error != null)
                {
                    Amqp.Connection conn = sender as Amqp.Connection;
                    if (conn.Equals(this.impl))
                    {
                        
                        if (this.latch != null)
                        {
                            this.latch.countDown();
                        }
                        else
                        {
                            Tracer.WarnFormat("Connection {0} closed unexpectedly with error : {1}", ClientId, error.ToString());
                            if (this.ConnectionInterruptedListener != null)
                            {
                                this.ConnectionInterruptedListener();
                            }
                            this.OnException(ExceptionSupport.GetException(error, "Connection {0} closed unexpectedly", ClientId));
                        }
                    }
                }
            }
        }

        private void Disconnect()
        {
            if(this.state.CompareAndSet(ConnectionState.CONNECTED, ConnectionState.CLOSING) && this.impl!=null && !this.impl.IsClosed)
            {
                Tracer.InfoFormat("Sending Close Request On Connection {0}.", ClientId);
                this.impl.Close(TimeSpan.FromMilliseconds(connInfo.closeTimeout), null);
                this.state.GetAndSet(ConnectionState.CLOSED);
            }
        }

        internal void OnException(Exception ex)
        {
            
            if (ExceptionListener != null)
            {
                if (ex is NMSAggregateException)
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
                            this.Connect();
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

        public Apache.NMS.ISession CreateSession()
        {
            return CreateSession(acknowledgementMode);
        }

        public Apache.NMS.ISession CreateSession(AcknowledgementMode acknowledgementMode)
        {
            this.CheckIfClosed();
            this.Connect();
            Session ses = new Session(this);
            if(!this.sessions.TryAdd(ses.Id, ses))
            {
                Tracer.ErrorFormat("Failed to add Session {0}.", ses.Id);
            }
            Tracer.InfoFormat("Created Session {0} on connection {1}.", ses.Id, this.ClientId);
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
            Exception error = null;
            if (this.closing.CompareAndSet(false, true))
            {
                foreach(Apache.NMS.ISession s in sessions.Values)
                {
                    try
                    {
                        s.Close();
                    } catch(NMSException ex)
                    {
                        error = ex;
                        break;
                    }
                    finally
                    {
                        if(error != null)
                            this.closing.GetAndSet(false);
                    }
                    
                }
                if(error != null)
                {
                    throw error;
                }
                sessions?.Clear();
                this.Disconnect();
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
            MessageFactory<ConnectionInfo>.Unregister(this);
        }

        #endregion

        #region NMSResource Methods

        public override bool IsStarted { get { return !mode.Value.Equals(Resource.Mode.Stopped); } }

        protected override void ThrowIfClosed()
        {
            this.CheckIfClosed();
        }

        protected override void StartResource()
        {
            this.Connect();

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

        public override string ToString()
        {
            return "Connection:\nConnection Info:"+connInfo.ToString();
        }

        
    }

    #region Connection Information inner Class

    internal class ConnectionInfo : ResourceInfo
    {
        static ConnectionInfo()
        {
            Amqp.ConnectionFactory defaultCF = new Amqp.ConnectionFactory();
            AmqpSettings defaultAMQPSettings = defaultCF.AMQP;
            TcpSettings defaultTCPSettings = defaultCF.TCP;

            DEFAULT_CHANNEL_MAX = defaultAMQPSettings.MaxSessionsPerConnection;
            DEFAULT_MAX_FRAME_SIZE = defaultAMQPSettings.MaxFrameSize;
            DEFAULT_IDLE_TIMEOUT = defaultAMQPSettings.IdleTimeout;

            DEFAULT_SEND_TIMEOUT = 30000;//defaultTCPSettings.SendTimeout;

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

        public ConnectionInfo() : this(null) { }
        public ConnectionInfo(Id clientId) : base(clientId)
        {
            if (clientId != null)
                this.clientId = clientId.ToString();
        }

        private Id ClientId = null;

        public override Id Id
        {
            get
            {
                if (base.Id == null)
                {
                    if (ClientId == null)
                    {
                        ClientId = new Id(clientId);
                    }
                    return ClientId;
                }
                else
                {
                    return base.Id;
                }
            }
        }

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

        public string TopicPrefix { get; internal set; } = null;

        public string QueuePrefix { get; internal set; } = null;

        public bool IsAnonymousRelay { get; internal set; } = false;

        public bool IsDelayedDelivery { get; internal set; } = false;

        public Message.Cloak.AMQPObjectEncodingType? EncodingType { get; internal set; } = null;


        public IList<string> Capabilities { get { return new List<string>(capabilities); } }

        public bool HasCapability(string capability)
        {
            return capabilities.Contains(capability);
        }

        public void AddCapability(string capability)
        {
            if (capability != null && capability.Length > 0)
                capabilities.Add(capability);
        }

        private List<string> capabilities = new List<string>();

        public override string ToString()
        {
            string result = "";
            result += "connInfo = [\n";
            foreach (MemberInfo info in this.GetType().GetMembers())
            {
                if (info is PropertyInfo)
                {
                    PropertyInfo prop = info as PropertyInfo;

                    if (prop.GetGetMethod(true).IsPublic)
                    {
                        if (prop.GetGetMethod(true).ReturnParameter.ParameterType.IsEquivalentTo(typeof(List<string>)))
                        {
                            result += string.Format("{0} = {1},\n", prop.Name, PropertyUtil.ToString(prop.GetValue(this) as IList));
                        }
                        else
                        {
                            result += string.Format("{0} = {1},\n", prop.Name, prop.GetValue(this));
                        }

                    }
                }
            }
            result = result.Substring(0, result.Length - 2) + "\n]";
            return result;
        }

    }

    #endregion

}
