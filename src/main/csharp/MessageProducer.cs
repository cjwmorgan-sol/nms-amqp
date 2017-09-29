using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP.Util;
using NMS.AMQP.Message;
using NMS.AMQP.Message.AMQP;
using NMS.AMQP.Message.Cloak;
using Amqp;
using Amqp.Framing;

namespace NMS.AMQP
{
    /// <summary>
    /// NMS.AMQP.MessageProducer facilitates management and creates the underlying Amqp.SenderLink protocol engine object.
    /// NMS.AMQP.MessageProducer is also a Factory for NMS.AMQP.Message.Message types.
    /// </summary>
    class MessageProducer : MessageLink, IMessageProducer
    {
        private SenderLink link;
        private ProducerInfo producerInfo;
        private Atomic<LinkState> state = new Atomic<LinkState>(LinkState.INITIAL);

        #region Constructor

        internal MessageProducer(Session ses, IDestination dest) : base(ses, dest as Destination)
        {
            producerInfo = new ProducerInfo(ses.ProducerIdGenerator.GenerateId());
            configure();
            Info = producerInfo;
        }

        #endregion
        
        #region Internal Properties

        internal Session InternalSession { get { return Session; } }

        internal Id ProducerId { get { return producerInfo.Id; } }

        #endregion

        #region Private Methods

        private void configure()
        {
            StringDictionary connProps = Session.Connection.Properties;
            StringDictionary sessProps = Session.Properties;
            PropertyUtil.SetProperties(producerInfo, connProps);
            PropertyUtil.SetProperties(producerInfo, sessProps);

        }

        private void configureMessage(IMessage msg)
        {
            msg.NMSPriority = Priority;
            msg.NMSDeliveryMode = DeliveryMode;
            msg.NMSDestination = Destination;
        }

        private void OnAttachedResp(Link link, Attach resp)
        {
            Tracer.InfoFormat("Received Performation Attach response on Link: {0}, Response: {1}", ProducerId, resp.ToString());
            OnResponse();
        }
        
        internal void onException(Exception e)
        {
            Session.OnException(e);
        }

        private Target CreateTarget()
        {
            Target t = new Target();
            t.Address = UriUtil.GetAddress(Destination);

            t.Timeout = (uint)producerInfo.sendTimeout;


            t.Durable = 0; //(DeliveryMode.Equals( MsgDeliveryMode.Persistent)) ? 1u : 0u;


            t.Capabilities = new[] { SymbolUtil.GetTerminusCapabilitiesForDestination(Destination) };

            t.Dynamic = Destination.IsTemporary;
            if (Destination.IsTemporary)
            {
                t.ExpiryPolicy = SymbolUtil.ATTACH_EXPIRY_POLICY_LINK_DETACH;
                Amqp.Types.Fields dnp = new Amqp.Types.Fields();
                dnp.Add(
                    SymbolUtil.ATTACH_DYNAMIC_NODE_PROPERTY_LIFETIME_POLICY,
                    SymbolUtil.DELETE_ON_CLOSE
                    );
                t.DynamicNodeProperties = dnp;
            }
            else
            {
                t.ExpiryPolicy = SymbolUtil.ATTACH_EXPIRY_POLICY_SESSION_END;
            }

            return t;
        }

        private Source CreateSource()
        {
            Source s = new Source();
            s.Address = Session.Connection.ClientId;
            s.Timeout = (uint)producerInfo.sendTimeout;
            return s;
        }

        private Attach CreateAttachFrame()
        {
            Attach frame = new Attach();
            frame.Source = CreateSource();
            frame.Target = CreateTarget();
            
            return frame;
        }

        #endregion

        #region MessageLink abstract Methods

        protected override Link createLink()
        {
            Attach frame = CreateAttachFrame();

            string linkName = producerInfo.Id + ":" + UriUtil.GetAddress(Destination);
            link = new SenderLink(Session.InnerSession, linkName, frame, OnAttachedResp);
            return link;
        }

        protected override void onInternalClosed(AmqpObject sender, Error error)
        {
            if (error != null)
            {
                if (sender.Equals(Link))
                {
                    NMSException e = ExceptionSupport.GetException(sender, "MessageProducer {0} Has closed unexpectedly.", this.ProducerId);
                    this.onException(e);
                    this.OnResponse();
                }
            }
        }

        #endregion

        #region NMSResource Methods

        protected override void StopResource()
        {
            
        }

        #endregion

        #region IMessageProducer Properties

        public MsgDeliveryMode DeliveryMode
        {
            get { return producerInfo.msgDelMode; }
            set { producerInfo.msgDelMode = value; }
        }

        public bool DisableMessageID
        {
            get { return producerInfo.disableMsgId; }
            set { producerInfo.disableMsgId = value; }
        }

        public bool DisableMessageTimestamp
        {
            get { return producerInfo.disableTimeStamp; }
            set { producerInfo.disableTimeStamp = value; }
        }

        public MsgPriority Priority
        {
            get { return producerInfo.priority; }
            set { producerInfo.priority = value; }
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
        
        public TimeSpan TimeToLive
        {
            get { return TimeSpan.FromMilliseconds(producerInfo.ttl); }
            set { producerInfo.ttl = Convert.ToInt64(value.TotalMilliseconds); }
        }

        #endregion

        #region IMessageProducer Methods
        
        public IBytesMessage CreateBytesMessage()
        {
            this.throwIfClosed();
            return Session.CreateBytesMessage();
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            this.throwIfClosed();
            IBytesMessage msg = CreateBytesMessage();
            msg.WriteBytes(body);
            return msg;
        }

        public IMapMessage CreateMapMessage()
        {
            this.throwIfClosed();
            return Session.CreateMapMessage();
        }

        public IMessage CreateMessage()
        {
            this.throwIfClosed();
            return Session.CreateMessage();
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            this.throwIfClosed();
            throw new NotImplementedException();
        }

        public IStreamMessage CreateStreamMessage()
        {
            this.throwIfClosed();
            throw new NotImplementedException();
        }

        public ITextMessage CreateTextMessage()
        {
            this.throwIfClosed();
            return Session.CreateTextMessage();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            ITextMessage msg = CreateTextMessage();
            msg.Text = text;
            return msg;
        }

        public void Dispose()
        {
            this.Close();
        }

        public void Send(IMessage message)
        {
            Send(message, DeliveryMode, Priority, TimeToLive);
        }

        public void Send(IDestination destination, IMessage message)
        {
            Send(destination, message, DeliveryMode, Priority, TimeToLive);
        }

        public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            this.throwIfClosed();
            if (Destination == null)
            {
                throw new IllegalStateException("Can not Send message on Anonymous Producer (without Destination).");
            }
            if (message == null)
            {
                throw new IllegalStateException("Can not Send a null message.");
            }

            DoSend(Destination, message, deliveryMode, priority, timeToLive);
        }

        public void Send(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            this.throwIfClosed();
            if (Destination != null)
            {
                throw new IllegalStateException("Can not Send message on Fixed Producer (with Destination).");
            }
            if (message == null)
            {
                throw new IllegalStateException("Can not Send a null message.");
            }

            DoSend(destination, message, deliveryMode, priority, timeToLive);
        }

        #endregion

        #region Protected Methods

        private IdGenerator msgIdGenerator;

        protected IdGenerator MessageIdGenerator
        {
            get
            {
                if (msgIdGenerator == null)
                {
                    msgIdGenerator = new CustomIdGenerator(
                        true,
                        "ID", 
                        MessageSupport.AMQP_ULONG_PREFIX.Substring(0,MessageSupport.AMQP_ULONG_PREFIX.Length-1),
                        new AtomicSequence()
                        );
                }
                return msgIdGenerator;
            }
        }

        protected void DoSend(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            message.NMSDestination = destination;
            message.NMSDeliveryMode = deliveryMode;
            message.NMSPriority = priority;
            if(timeToLive != TimeSpan.Zero)
                message.NMSTimeToLive = timeToLive;

            if (!DisableMessageTimestamp)
            {
                message.NMSTimestamp = DateTime.Now;
            }

            if (!DisableMessageID)
            {
                message.NMSMessageId = MessageIdGenerator.GenerateId().ToString();
            }

            Amqp.Message amqpmsg = null;
            if (message is Message.Message)
            {
                IMessageCloak cloak = (message as Message.Message).GetMessageCloak();
                if (cloak is AMQPMessageCloak)
                {
                    amqpmsg = (cloak as AMQPMessageCloak).AMQPMessage;
                }
            }
            else
            {
                Message.Message nmsmsg = this.Session.Connection.TransformFactory.TransformMessage<Message.Message>(message);
                IMessageCloak cloak = nmsmsg.GetMessageCloak();
                if (cloak is AMQPMessageCloak)
                {
                    amqpmsg = (cloak as AMQPMessageCloak).AMQPMessage;
                }
            }
            if (amqpmsg != null)
                this.link.Send(amqpmsg, Convert.ToInt32(Info.sendTimeout));
        }

        #endregion

        #region Inner Producer Info Class

        protected class ProducerInfo : LinkInfo
        {
            protected const bool DEFAULT_DISABLE_MESSAGE_ID = false;
            protected const bool DEFAULT_DISABLE_TIMESTAMP = false;
            protected const MsgDeliveryMode DEFAULT_MSG_DELIVERY_MODE = NMSConstants.defaultDeliveryMode;
            protected const MsgPriority DEFAULT_MSG_PRIORITY = NMSConstants.defaultPriority;
            protected static readonly long DEFAULT_TTL;

            static ProducerInfo()
            {
                DEFAULT_TTL = Convert.ToInt64(NMSConstants.defaultTimeToLive.TotalMilliseconds);
            }

            internal ProducerInfo(Id id):base(id)
            {
            }

            public MsgDeliveryMode msgDelMode { get; set; } = DEFAULT_MSG_DELIVERY_MODE;
            public bool disableMsgId { get; set; } = DEFAULT_DISABLE_MESSAGE_ID;
            public bool disableTimeStamp { get; set; } = DEFAULT_DISABLE_TIMESTAMP;
            public MsgPriority priority { get; set; } = DEFAULT_MSG_PRIORITY;
            public long ttl { get; set; } = DEFAULT_TTL;

            public override string ToString()
            {
                string result = "";
                result += "producerInfo = [\n";
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
