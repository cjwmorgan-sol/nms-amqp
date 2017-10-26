using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Policies;
using NMS.AMQP.Util;


namespace NMS.AMQP
{
    internal delegate Task<Amqp.Connection> ProviderCreateConnection(Amqp.Address addr, Amqp.Framing.Open open, Amqp.OnOpened onOpened);
    /// <summary>
    /// NMS.AMQP.ConnectionFactory implements Apache.NMS.IConnectionFactory.
    /// NMS.AMQP.ConnectionFactory creates, manages and configures the Amqp.ConnectionFactory used to create Amqp Connections.
    /// </summary>
    public class ConnectionFactory : Apache.NMS.IConnectionFactory
    {

        public const string DEFAULT_BROKER_URL = "tcp://localhost:5672";
        public static readonly string CLIENT_ID_PROP = PropertyUtil.CreateProperty("ClientId");
        public static readonly string USERNAME_PROP = PropertyUtil.CreateProperty("UserName");
        public static readonly string PASSWORD_PROP = PropertyUtil.CreateProperty("Password");

        private Uri brokerUri;
        private string clientId;
        private IdGenerator clientIdGenerator = new IdGenerator();


        private StringDictionary properties = new StringDictionary();
        private IRedeliveryPolicy redeliveryPolicy = new RedeliveryPolicy();

        internal Amqp.ConnectionFactory impl;

        #region Constructor Methods
        
        public ConnectionFactory() 
            : this(DEFAULT_BROKER_URL)
        {
        }

        public ConnectionFactory(string brokerUri) 
            : this(new Uri(brokerUri), null, null)
        {
            
        }

        public ConnectionFactory(string brokerUri, string clientId)
            : this(new Uri(brokerUri), clientId, null)
        {
        }

        public ConnectionFactory(Uri brokerUri) 
            : this(brokerUri, null, null)
        { }

        public ConnectionFactory(Uri brokerUri, StringDictionary props) 
            : this(brokerUri, null, props)
        { }

        public ConnectionFactory(Uri brokerUri, string clientId, StringDictionary props)
        {
            BrokerUri = brokerUri;

            impl = new Amqp.ConnectionFactory();
            impl.AMQP.HostName = BrokerUri.Host;
            this.clientId = clientId;
            this.ConnectionProperties = props;
            impl.SASL.Profile = Amqp.Sasl.SaslProfile.Anonymous;
            impl.TCP.SendTimeout = 30000;


        }

        #endregion

        #region IConnection Members

        public Uri BrokerUri
        {
            get { return brokerUri; }
            set { brokerUri = value; }
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

        public IRedeliveryPolicy RedeliveryPolicy
        {
            get
            {
                if (redeliveryPolicy == null)
                {
                    return new RedeliveryPolicy();
                }
                return this.redeliveryPolicy;
            }
            set
            {
                if (value != null)
                {
                    this.redeliveryPolicy = value;
                }
            }
        }

        public IConnection CreateConnection()
        {
            try
            {
                Connection conn = new Connection(brokerUri, ClientIDGenerator);

                Tracer.Info("Configuring Connection Properties");

                bool shouldSetClientID = this.clientId != null;

                conn.Configure(this);

                if (shouldSetClientID)
                {
                    conn.ClientId = this.clientId;

                    conn.Connect();
                }

                return conn;

            }
            catch (Exception ex)
            {
                if(ex is NMSException)
                {
                    throw ex;
                }
                else
                {
                    throw new NMSException(ex.Message, ex);
                }
            }
        }

        public IConnection CreateConnection(string userName, string password)
        {
            
            ConnectionProperties.Add(USERNAME_PROP, userName);

            ConnectionProperties.Add(PASSWORD_PROP, password);

            return CreateConnection();
        }

        

        #endregion

        #region Connection Properties Methods

        public StringDictionary ConnectionProperties
        {
            get { return properties; }
            set
            {
                StringDictionary props = PropertyUtil.Clone(value);
                if (props.ContainsKey(CLIENT_ID_PROP))
                {
                    this.clientId = props[CLIENT_ID_PROP];
                    props.Remove(CLIENT_ID_PROP);
                }
                properties = props;
                
                // Apply properties to internal connection factory settings
                PropertyUtil.SetProperties(this.impl.TCP, properties);
                PropertyUtil.SetProperties(this.impl.AMQP, properties);
                // Apply properties to this instance
                PropertyUtil.SetProperties(this, properties);
            }
        }

        public bool HasConnecitonProperty(string key)
        {
            return this.properties.ContainsKey(key);
        }

        private IdGenerator ClientIDGenerator 
        {
            get
            {
                IdGenerator cig = clientIdGenerator;
                lock (this)
                {
                    if (cig == null)
                    {
                        clientIdGenerator = new IdGenerator();
                        cig = clientIdGenerator;
                    }
                }
                return cig;
            }
        }

        #endregion
    }
}
