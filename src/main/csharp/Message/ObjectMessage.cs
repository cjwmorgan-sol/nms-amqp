using System;
using System.Runtime.Serialization;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP.Util.Types;

namespace NMS.AMQP.Message
{
    using Cloak;
    class ObjectMessage : Message, IObjectMessage
    {
        protected new readonly IObjectMessageCloak cloak;
        internal ObjectMessage(IObjectMessageCloak message) : base(message)
        {
            this.cloak = message;
        }

        public new byte[] Content
        {
            get
            {
                return cloak.Content;
            }

            set
            {
                
            }
        }

        public object Body
        {
            get
            {
                return cloak.Body;
            }
            set
            {
                
                try
                {
                    cloak.Body = value;
                }
                catch (SerializationException se)
                {
                    throw NMSExceptionSupport.CreateMessageFormatException(se);
                }
                
            }
        }

        internal override Message Copy()
        {
            return new ObjectMessage(this.cloak.Copy());
        }
    }
}