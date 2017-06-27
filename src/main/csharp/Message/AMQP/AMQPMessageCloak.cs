using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using Amqp;
using Amqp.Types;
using Amqp.Framing;

namespace NMS.AMQP.Message.AMQP
{
    using Util;
    using Cloak;
    using Factory;
    
    class AMQPMessageCloak : IMessageCloak
    {
        private TimeSpan timeToLive;
        private IDestination replyTo;
        private bool redelivered = false;
        private string msgId;
        private IDestination destination;
        private string correlationId;
        private IPrimitiveMap properties;
        private MessagePropertyIntercepter propertyHelper;

        private Header messageHeader = null;
        private DeliveryAnnotations deliveryAnnontations = null;
        private MessageAnnotations messageAnnontations = null;
        private ApplicationProperties applicationProperties = null;
        private Properties messageProperties = null;
        private Footer messageFooter = null;

        private byte[] content;

        protected Amqp.Message message;
        protected readonly Connection connection;
        protected MessageConsumer consumer;

        internal AMQPMessageCloak(Connection c) 
        {
            message = new Amqp.Message();
            connection = c;
            InitMessage();
        }

        internal AMQPMessageCloak(MessageConsumer c, Amqp.Message msg)
        {
            message = msg;
            consumer = c;
            connection = c.Session.Connection;
            InitMessage();
        }


        #region Internal Properties

        internal Connection Connection {  get { return connection; } }

        internal Amqp.Message AMQPMessage { get { return message; } }

        internal virtual byte JMSMessageType { get { return MessageSupport.JMS_TYPE_MSG; } }

        #endregion

        private void InitMessage()
        {
            InitMessageHeader();
            InitMessageProperties();
            SetMessageAnnotation(SymbolUtil.JMSX_OPT_MSG_TYPE, (sbyte)JMSMessageType);
        }

        protected virtual void CopyInto(IMessageCloak msg)
        {
            MessageTransformation.CopyNMSMessageProperties(this, msg);
        }

        #region Protected Amqp.Message Initialize/Accessor

        protected void InitMessageHeader()
        {
            if (this.messageHeader == null && this.message.Header == null)
            {
                this.messageHeader = new Header();
                this.message.Header = this.messageHeader;
            }
            else if (this.messageHeader == null && this.message.Header != null)
            {
                this.messageHeader = this.message.Header;
            }
            else if (this.messageHeader != null && this.message.Header == null)
            {
                this.message.Header = this.messageHeader;
            }
        }

        protected void InitMessageProperties()
        {
            if (this.messageProperties == null && this.message.Properties == null)
            {
                this.messageProperties = new Properties();
                this.message.Properties = this.messageProperties;
            }
            else if (this.messageProperties == null && this.message.Properties != null)
            {
                this.messageProperties = this.message.Properties;
            }
            else if (this.messageProperties !=null && this.message.Properties == null)
            {
                this.message.Properties = this.messageProperties;
            }
        }

        protected void InitApplicationProperties()
        {
            if (this.applicationProperties == null && this.message.ApplicationProperties == null)
            {
                this.applicationProperties = new ApplicationProperties();
                this.message.ApplicationProperties = this.applicationProperties;
            }
            else if (this.applicationProperties == null && this.message.ApplicationProperties != null)
            {
                this.applicationProperties = this.message.ApplicationProperties;
            }
            else if (this.applicationProperties != null && this.message.ApplicationProperties == null)
            {
                this.message.ApplicationProperties = this.applicationProperties;

            }
        }

        protected void InitDeliveryAnnotations()
        {
            if (this.deliveryAnnontations == null && this.message.DeliveryAnnotations == null)
            {
                this.deliveryAnnontations = new DeliveryAnnotations();
                this.message.DeliveryAnnotations = this.deliveryAnnontations;
            }
            else if (this.deliveryAnnontations == null && this.message.DeliveryAnnotations != null)
            {
                this.deliveryAnnontations = this.message.DeliveryAnnotations;
            }
            else if (this.deliveryAnnontations != null && this.message.DeliveryAnnotations == null)
            {
                this.message.DeliveryAnnotations = this.deliveryAnnontations;
            }
        }

        protected void InitMessageAnnontations()
        {
            if(this.messageAnnontations == null && this.message.MessageAnnotations == null)
            {
                this.messageAnnontations = new MessageAnnotations();
                this.message.MessageAnnotations = messageAnnontations;
            }
            else if (this.messageAnnontations == null && this.message.MessageAnnotations != null)
            {
                this.messageAnnontations = this.message.MessageAnnotations;
            }
            else if (this.messageAnnontations != null && this.message.MessageAnnotations == null)
            {
                this.message.MessageAnnotations = this.messageAnnontations;
            }
        }

        protected void SetDeliveryAnnotation(Symbol key, object value)
        {
            InitDeliveryAnnotations();
            this.deliveryAnnontations[key] = value;
        }

        protected void SetMessageAnnotation(Symbol key, object value)
        {
            InitMessageAnnontations();
            messageAnnontations[key] = value;
        }

        protected object GetMessageAnnotation(Symbol key)
        {
            InitMessageAnnontations();
            return messageAnnontations[key];
        }

        #endregion

        #region IMessageCloak Properties

        public bool IsReceived { get { return consumer != null; } }

        public byte[] Content
        {
            get
            {
                return content;
            }

            set
            {
                content = value;
            }
        }

        public string NMSCorrelationID
        {
            get
            {
                /*
                 *  this is available in the release of AMQP.NetLite 1.2.3
                 */
                object objId = this.messageProperties.GetCorrelationId();
                if(this.correlationId == null && objId != null)
                {
                    this.correlationId = MessageSupport.CreateNMSMessageId(objId);
                }
                
                return this.correlationId;
            }
            set
            {
                /* this is available in the next release of AMQP.NetLite */
                object objId = MessageSupport.CreateAMQPMessageId(value);
                this.messageProperties.SetCorrelationId(objId);
                this.correlationId = value;
                
                //object objId = MessageSupport.CreateAMQPMessageId(value);
                //if (objId == null || objId is string)
                //{
                //    this.messageProperties.CorrelationId = (string)objId;
                //    this.correlationId = value;
                //}
                //else
                //{
                //    throw new NotImplementedException(string.Format("Correlation ID : {0} not supported. Only String type Correlation IDs are supported.", value));
                //}
            }
        }

        public MsgDeliveryMode NMSDeliveryMode
        {
            get
            {
                if (this.messageHeader.Durable)
                {
                    return MsgDeliveryMode.Persistent;
                }
                else
                {
                    return MsgDeliveryMode.NonPersistent;
                }
                
            }
            set
            {
                if (value.Equals(MsgDeliveryMode.Persistent))
                {
                    this.messageHeader.Durable = true;
                }
                else
                {
                    this.messageHeader.Durable = false;
                }
            }
        }

        public IDestination NMSDestination
        {
            get
            {
                if(destination == null && consumer != null)
                {
                    object typeObj = GetMessageAnnotation(SymbolUtil.JMSX_OPT_DEST);
                    if(typeObj != null)
                    {
                        byte type = (byte)typeObj;
                        destination = MessageSupport.CreateDestinationFromMessage(consumer, messageProperties, type);
                    }
                }
                return destination;
            }
            set
            {
                string destString = null;
                IDestination dest = null;
                if (value != null) {
                    destString = (value.IsTopic) ? (value as ITopic).TopicName : (value as IQueue).QueueName; 
                    dest = value;
                }
                this.messageProperties.To = destString;
                SetMessageAnnotation(SymbolUtil.JMSX_OPT_DEST, MessageSupport.GetValueForDestination(dest));
                destination = dest;
            }
        }

        public string NMSMessageId
        {
            get
            {
                object objId = this.messageProperties.GetMessageId();
                if(this.msgId == null && objId != null)
                {
                    this.msgId = MessageSupport.CreateNMSMessageId(objId);
                }
                return this.msgId;
            }
            set
            {
                object msgId = MessageSupport.CreateAMQPMessageId(value);
                //Tracer.InfoFormat("Set message Id to <{0}>: {1}", msgId.GetType().Name, msgId.ToString());
                this.messageProperties.SetMessageId(msgId);
                this.msgId = value;
            }
        }

        public MsgPriority NMSPriority
        {
            get { return MessageSupport.GetPriorityFromValue(this.messageHeader.Priority); }
            set
            {
                this.messageHeader.Priority = MessageSupport.GetValueForPriority(value);
            }
        }

        public bool NMSRedelivered
        {
            get
            {
                if (this.messageHeader.DeliveryCount > 0)
                {
                    redelivered = true;
                }
                return redelivered;
            }
            set { redelivered = value; }
        }

        public IDestination NMSReplyTo
        {
            get
            {
                if (replyTo == null && IsReceived)
                {
                    object typeObj = GetMessageAnnotation(SymbolUtil.JMSX_OPT_REPLY_TO);
                    if(typeObj != null)
                    {
                        byte type = (byte)typeObj;
                        replyTo = MessageSupport.CreateDestinationFromMessage(consumer, messageProperties, type, true);
                    }
                }
                return replyTo;
            }
            set
            {
                IDestination dest = null;
                string destString = null;
                if (value != null)
                {
                    destString = (value.IsTopic) ? (value as ITopic).TopicName : (value as IQueue).QueueName;
                    dest = value;
                    SetMessageAnnotation(SymbolUtil.JMSX_OPT_REPLY_TO, MessageSupport.GetValueForDestination(dest));
                }
                this.messageProperties.ReplyTo = destString;
                
                replyTo = dest;
            }
        }

        public DateTime NMSTimestamp
        {
            get { return messageProperties.CreationTime; }
            set
            {
                messageProperties.CreationTime = value;
            }
        }

        public TimeSpan NMSTimeToLive
        {
            get
            {
                if (messageProperties.AbsoluteExpiryTime == DateTime.MinValue)
                {
                    if (timeToLive != null)
                    {
                        return timeToLive;
                    }
                    else
                    {

                        timeToLive = TimeSpan.FromMilliseconds(Convert.ToDouble(this.messageHeader.Ttl));
                        return timeToLive;

                    }
                }
                else
                {
                    return messageProperties.AbsoluteExpiryTime - DateTime.Now;
                }
            }
            set
            {
                DateTime expireTime = DateTime.MinValue;
                if (value != null && value != TimeSpan.Zero)
                {
                    DateTime createTime = NMSTimestamp;
                    expireTime = createTime + value;
                    messageProperties.AbsoluteExpiryTime = expireTime;
                }
                timeToLive = value;
            }
        }

        public string NMSType
        {
            get { return this.messageProperties.Subject; }
            set { this.messageProperties.Subject = value; }
        }

        public IPrimitiveMap Properties
        {
            get
            {
                if (properties == null)
                {
                    InitApplicationProperties();
                    properties = new AMQPPrimitiveMap(this.applicationProperties);
                    propertyHelper = new MessagePropertyIntercepter(this, properties, false);
                }
                return propertyHelper;
            }
        }
        
        public void Acknowledge()
        {
            if (connection.IsClosed)
            {
                throw new IllegalStateException("Can not acknowledge Message on closed connection.");
            }
        }

        public virtual void ClearBody()
        {
            Content = null;
        }

        public virtual void ClearProperties()
        {
            if (properties != null)
            {
                propertyHelper.Clear();
            }
        }

        public virtual IMessageCloak Copy()
        {
            IMessageCloak copy = null;
            switch(JMSMessageType)
            {
                case MessageSupport.JMS_TYPE_MSG:
                    copy = new AMQPMessageCloak(connection);
                    break;
                case MessageSupport.JMS_TYPE_BYTE:
                    copy = new AMQPBytesMessageCloak(connection);
                    break;
                case MessageSupport.JMS_TYPE_TXT:
                    copy = new AMQPTextMessageCloak(connection);
                    break;
                case MessageSupport.JMS_TYPE_MAP:
                case MessageSupport.JMS_TYPE_OBJ:
                case MessageSupport.JMS_TYPE_STRM:
                default:
                    throw new NMSException("Fatal error Invalid JSM type.");
            }
            
            CopyInto(copy);
            return copy;
        }

        public object GetMessageId()
        {
            return null;
        }

        public void SetMessageId(object messageId) { }

        public object GetCorrelationId()
        {
            return null;
        }

        public void SetCorrelationId(object correlationId) { }

        #endregion
    }
}
