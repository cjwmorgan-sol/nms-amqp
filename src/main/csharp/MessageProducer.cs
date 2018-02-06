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
using System.Threading;

namespace NMS.AMQP
{
    /// <summary>
    /// NMS.AMQP.MessageProducer facilitates management and creates the underlying Amqp.SenderLink protocol engine object.
    /// NMS.AMQP.MessageProducer is also a Factory for NMS.AMQP.Message.Message types.
    /// </summary>
    class MessageProducer : MessageLink, IMessageProducer
    {
        private IdGenerator msgIdGenerator;
        private ISenderLink link;
        private ProducerInfo producerInfo;
        private Atomic<LinkState> state = new Atomic<LinkState>(LinkState.INITIAL);

        // Stat fields
        private int MsgsSentOnLink = 0;

        #region Constructor

        internal MessageProducer(Session ses, IDestination dest) : base(ses, dest)
        {
            producerInfo = new ProducerInfo(ses.ProducerIdGenerator.GenerateId());
            Info = producerInfo;
            Configure();
            
        }

        #endregion
        
        #region Internal Properties

        internal Session InternalSession { get { return Session; } }

        internal Id ProducerId { get { return producerInfo.Id; } }

        #endregion

        #region Private Methods
        
        private void ConfigureMessage(IMessage msg)
        {
            msg.NMSPriority = Priority;
            msg.NMSDeliveryMode = DeliveryMode;
            msg.NMSDestination = Destination;
        }

        private void OnAttachedResp(ILink link, Attach resp)
        {
            Tracer.InfoFormat("Received Performation Attach response on Link: {0}, Response: {1}", ProducerId, resp.ToString());
            
            OnResponse();
        }
        
        internal void OnException(Exception e)
        {
            Session.OnException(e);
        }

        private Target CreateTarget()
        {
            Target t = new Target();
            
            t.Address = UriUtil.GetAddress(Destination, this.Session.Connection);

            t.Timeout = (uint)producerInfo.sendTimeout;

            // Durable is used for a durable subscription
            t.Durable = (uint)TerminusDurability.NONE;

            if (Destination != null)
            {
                t.Capabilities = new[] { SymbolUtil.GetTerminusCapabilitiesForDestination(Destination) };
            }
            t.Dynamic = false;
            
            return t;
        }

        private Source CreateSource()
        {
            Source s = new Source();
            s.Address = this.ProducerId.ToString();
            s.Timeout = (uint)producerInfo.sendTimeout;
            s.Outcomes = new Amqp.Types.Symbol[]
            {
                SymbolUtil.ATTACH_OUTCOME_ACCEPTED,
                SymbolUtil.ATTACH_OUTCOME_REJECTED,
            };
            return s;
        }

        private Attach CreateAttachFrame()
        {
            Attach frame = new Attach();
            frame.Source = CreateSource();
            frame.Target = CreateTarget();
            frame.SndSettleMode = SenderSettleMode.Unsettled;
            frame.IncompleteUnsettled = false;
            frame.InitialDeliveryCount = 0;
            
            return frame;
        }

        #endregion

        #region MessageLink abstract Methods

        protected override ILink CreateLink()
        {
            Attach frame = CreateAttachFrame();

            string linkName = producerInfo.Id + ":" + UriUtil.GetAddress(Destination, Session.Connection);
            link = new SenderLink(Session.InnerSession as Amqp.Session, linkName, frame, OnAttachedResp);
            
            return link;
        }

        protected override void OnInternalClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                if (sender.Equals(Link))
                {
                    NMSException e = ExceptionSupport.GetException(sender, "MessageProducer {0} Has closed unexpectedly.", this.ProducerId);
                    this.OnException(ExceptionSupport.Wrap(e));
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
            this.ThrowIfClosed();
            return Session.CreateBytesMessage();
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            this.ThrowIfClosed();
            IBytesMessage msg = CreateBytesMessage();
            msg.WriteBytes(body);
            return msg;
        }

        public IMapMessage CreateMapMessage()
        {
            this.ThrowIfClosed();
            return Session.CreateMapMessage();
        }

        public IMessage CreateMessage()
        {
            this.ThrowIfClosed();
            return Session.CreateMessage();
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            this.ThrowIfClosed();
            return Session.CreateObjectMessage(body);
        }

        public IStreamMessage CreateStreamMessage()
        {
            this.ThrowIfClosed();
            return Session.CreateStreamMessage();
        }

        public ITextMessage CreateTextMessage()
        {
            this.ThrowIfClosed();
            return Session.CreateTextMessage();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            ITextMessage msg = CreateTextMessage();
            msg.Text = text;
            return msg;
        }

        public override void Close()
        {
            bool wasNotClosed = !IsClosed;
            base.Close();
            if (IsClosed && wasNotClosed)
            {
                Tracer.InfoFormat("Closing Producer {0}, MsgSentOnLink {1}", Id, MsgsSentOnLink);
            }
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
            this.ThrowIfClosed();
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
            this.ThrowIfClosed();
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
        
        protected override void Configure()
        {
            base.Configure();
        }

        protected IdGenerator MessageIdGenerator
        {
            get
            {
                if (msgIdGenerator == null)
                {
                    msgIdGenerator = new CustomIdGenerator(
                        true,
                        "ID", 
                        MessageSupport.AMQP_STRING_PREFIX.Substring(0,MessageSupport.AMQP_STRING_PREFIX.Length-1),
                        new AtomicSequence()
                        );
                }
                return msgIdGenerator;
            }
        }

        protected void PrepareMessageForSend(Message.Message message)
        {
            if (message == null) return;
            if (message is Message.BytesMessage)
            {
                (message as Message.BytesMessage).Reset();
            }
            else if (message is Message.StreamMessage)
            {
                (message as Message.StreamMessage).Reset();
            }
            else
            {
                message.IsReadOnly = true;
            }
            message.IsReadOnlyProperties = true;
        }

        protected void DoSend(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            this.Attach();
            bool sendSync = deliveryMode.Equals(MsgDeliveryMode.Persistent);
            if(destination.IsTemporary && (destination as TemporaryDestination).IsDeleted)
            {
                throw new InvalidDestinationException("Can not send message on deleted temporary topic.");
            }
            message.NMSDestination = destination;
            message.NMSDeliveryMode = deliveryMode;
            message.NMSPriority = priority;
            if (!DisableMessageTimestamp)
            {
                message.NMSTimestamp = DateTime.UtcNow;
            }

            if (timeToLive != NMSConstants.defaultTimeToLive)
                message.NMSTimeToLive = timeToLive;

            

            if (!DisableMessageID)
            {
                message.NMSMessageId = MessageIdGenerator.GenerateId().ToString();
            }

            Amqp.Message amqpmsg = null;
            if (message is Message.Message)
            {
                Message.Message copy = (message as Message.Message).Copy();
                copy.NMSDestination = DestinationTransformation.Transform(Session.Connection, destination);
                PrepareMessageForSend(copy);
                IMessageCloak cloak = copy.GetMessageCloak();
                if (cloak is AMQPMessageCloak)
                {
                    amqpmsg = (cloak as AMQPMessageCloak).AMQPMessage;
                }
            }
            else
            {
                Message.Message nmsmsg = this.Session.Connection.TransformFactory.TransformMessage<Message.Message>(message);
                PrepareMessageForSend(nmsmsg);
                IMessageCloak cloak = nmsmsg.GetMessageCloak().Copy();
                if (cloak is AMQPMessageCloak)
                {
                    amqpmsg = (cloak as AMQPMessageCloak).AMQPMessage;
                }
            }

            

            if (amqpmsg != null)
            {
                if (Tracer.IsDebugEnabled)
                    Tracer.DebugFormat("Sending message : {0}", message.ToString());
                ManualResetEvent acked = (sendSync) ? new ManualResetEvent(false) : null;
                Outcome outcome = null;
                Exception respException = null;
                OutcomeCallback ocb = (sender, m, o, s) =>
                {
                    outcome = o;
                    
                    if (outcome.Descriptor.Name.Equals("amqp:rejected:list"))
                    {
                        string msgId = MessageSupport.CreateNMSMessageId(m.Properties.GetMessageId());
                        Error err = (outcome as Amqp.Framing.Rejected).Error;

                        respException = ExceptionSupport.GetException(err, "Msg {0} rejected:", msgId);
                    }
                    else if (outcome.Descriptor.Name.Equals("amqp:released:list") && (!IsClosing && !IsClosed))
                    {
                        string msgId = MessageSupport.CreateNMSMessageId(m.Properties.GetMessageId());
                        Error err = new Error { Condition = "amqp:message:released", Description = null };
                        respException = ExceptionSupport.GetException(err, "Msg {0} released:", msgId);
                    }
                    if(s != null)
                    {
                        Tracer.InfoFormat("Message failed to send: {0}", (s as NMSException).Message);
                    }

                    if (sendSync)
                    {
                        acked?.Set();
                    }
                    else if (respException != null)
                    {
                        this.OnException(respException);
                    }
                    
                };
                
                try
                {
                    this.link.Send(amqpmsg, ocb, respException);
                    MsgsSentOnLink++;
                }
                catch(Exception ex)
                {
                    Tracer.ErrorFormat("Encountered Error on sending message from Producer {0}. Message: {1}. Stack : {2}.", Id, ex.Message, ex.StackTrace);
                    throw ExceptionSupport.Wrap(ex);
                }
                
                if(sendSync)
                {
                    Tracer.DebugFormat("Message sent waiting {0}ms for response.", Info.requestTimeout);
                    if (!acked.WaitOne(Convert.ToInt32(Info.requestTimeout)))
                    {
                        throw ExceptionSupport.Wrap(new TimeoutException(string.Format("Sending message: Failed to receive response in {0}ms", Info.requestTimeout)));
                    }

                    Tracer.DebugFormat("Message received response: {0}", outcome.ToString());

                    if (outcome != null && respException != null)
                    {
                        throw respException;
                    }
                }
                
                
            }

        }

        #endregion

        
    }

    #region Producer Info Class

    internal class ProducerInfo : LinkInfo
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

        internal ProducerInfo(Id id) : base(id)
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
