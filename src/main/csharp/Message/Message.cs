using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP;
using NMS.AMQP.Util;

namespace NMS.AMQP.Message
{
    using Cloak;
    /// <summary>
    /// NMS.AMQP.Message.Message is the root message class that implements the Apache.NMS.IMessage interface.
    /// NMS.AMQP.Message.Message uses the NMS.AMQP.Message.Cloak.IMessageCloak interface to detach from the underlying AMQP 1.0 engine.
    /// </summary>
    class Message : IMessage
    {
        
        protected readonly IMessageCloak cloak;

        private bool isReadOnlyMsgBody = false;
        private bool isReadOnlyProperties = false;

        #region Constructors

        internal Message(IMessageCloak message)
        {
            this.cloak = message;
        }
        
        #endregion
        
        #region Protected Methods

        protected void FailIfReadOnlyMsgBody()
        {
            if(IsReadOnly == true)
            {
                throw new MessageNotWriteableException("Message is in Read-Only mode.");
            }
        }

        protected void FailIfWriteOnlyMsgBody()
        {
            if (IsReadOnly == false)
            {
                throw new MessageNotReadableException("Message is in Write-Only mode.");
            }
        }

        #endregion

        #region Public Properties

        public virtual byte[] Content
        {
            get
            {
                return cloak.Content;
            }

            set
            {
                cloak.Content = value;
            }
        }

        public virtual bool IsReadOnly
        {
            get { return isReadOnlyMsgBody; }
            protected set { isReadOnlyMsgBody = value; }
        }


        #endregion

        #region IMessage Properties


        public string NMSCorrelationID
        {
            get { return cloak.NMSCorrelationID; }
            set { cloak.NMSCorrelationID = value; }
        }

        public MsgDeliveryMode NMSDeliveryMode
        {
            get { return cloak.NMSDeliveryMode; }
            set { cloak.NMSDeliveryMode = value; }
        }

        public IDestination NMSDestination
        {
            get { return cloak.NMSDestination; }
            set { cloak.NMSDestination = value; }
        }

        public string NMSMessageId
        {
            get { return cloak.NMSMessageId; }
            set { cloak.NMSMessageId = value; }
        }

        public MsgPriority NMSPriority
        {
            get { return cloak.NMSPriority; }
            set { cloak.NMSPriority = value; }
        }

        public bool NMSRedelivered
        {
            get { return cloak.NMSRedelivered; }
            set { cloak.NMSRedelivered = value; }
        }

        public IDestination NMSReplyTo
        {
            get { return cloak.NMSReplyTo; }
            set { cloak.NMSReplyTo = value; }
        }

        public DateTime NMSTimestamp
        {
            get { return cloak.NMSTimestamp; }
            set { cloak.NMSTimestamp = value; }
        }

        public TimeSpan NMSTimeToLive
        {
            get { return cloak.NMSTimeToLive; }
            set { cloak.NMSTimeToLive = value; }
        }

        public string NMSType
        {
            get { return cloak.NMSType; }
            set { cloak.NMSType = value; }
        }

        public IPrimitiveMap Properties
        {
            get
            {
                return cloak.Properties;
            }
        }

        #endregion

        #region IMessage Methods

        public virtual void Acknowledge()
        {
            cloak.Acknowledge();
        }

        public void ClearBody()
        {
            cloak.ClearBody();
        }

        public void ClearProperties()
        {
            cloak.ClearProperties();
        }

        #endregion

        #region Internal Methods

        internal IMessageCloak GetMessageCloak()
        {
            return cloak.Copy();
        }

        #endregion

        public override string ToString()
        {
            return base.ToString() + ":\n Impl Type: " + cloak.ToString();
        }

    }
}
