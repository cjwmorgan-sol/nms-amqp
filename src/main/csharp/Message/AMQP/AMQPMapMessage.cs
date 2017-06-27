using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using Amqp.Types;
using Amqp.Framing;

namespace NMS.AMQP.Message.AMQP
{
    using Cloak;
    using Factory;
    using Util;
    class AMQPMapMessage : AMQPMessageCloak, IMapMessageCloak
    {
        private IPrimitiveMap map = null;
        

        internal AMQPMapMessage(Connection conn) : base(conn)
        {
            InitializeMapBody();
        }

        internal AMQPMapMessage(MessageConsumer c, Amqp.Message msg) : base(c, msg)
        {
            InitializeMapBody();
        }

        internal override byte JMSMessageType { get { return MessageSupport.JMS_TYPE_MAP; } }

        private void InitializeMapBody()
        {
            if (message.BodySection == null)
            {
                map = new PrimitiveMap();
                AmqpValue val = new AmqpValue();
                val.Value = map;
                message.BodySection = val;
            }
            else 
            {
                if (message.BodySection is AmqpValue)
                {
                    object obj = (message.BodySection as AmqpValue).Value;
                    if (obj == null)
                    {
                        map = new PrimitiveMap();
                        (message.BodySection as AmqpValue).Value = map;
                    }
                    else if (obj is IPrimitiveMap)
                    {
                        map = obj as IPrimitiveMap;
                    }
                    else
                    {
                        throw new NMSException(string.Format("Invalid message body value type. Type: {0}.", obj.GetType().Name));
                    }
                }
                else
                {
                    throw new NMSException("Invalid message body type.");
                }
            }
            
        }

        IPrimitiveMap IMapMessageCloak.Map
        {
            get
            {
                return map;
            }
        }

        IMapMessageCloak IMapMessageCloak.Copy()
        {
            IMapMessageCloak copy = new AMQPMapMessage(Connection);
            CopyInto(copy);
            return copy;
        }

        protected override void CopyInto(IMessageCloak msg)
        {
            base.CopyInto(msg);
            IPrimitiveMap copy = (msg as IMapMessageCloak).Map;
            foreach(string key in this.map.Keys)
            {
                object value = map[key];
                if (value != null)
                {
                    Type valType = value.GetType();
                    if (valType.IsPrimitive)
                    {
                        // value copy primitive value
                        copy[key] = value;
                    }
                    else if (valType.IsArray && valType.Equals(typeof(byte[])))
                    {
                        // use IPrimitive map SetBytes for most common implementation this is a deep copy.
                        byte[] original = value as byte[];
                        copy.SetBytes(key, original);
                    }
                    else if (valType.Equals(typeof(IDictionary)))
                    {
                        // reference copy
                        copy.SetDictionary(key, value as IDictionary);
                    }
                    else if (valType.Equals(typeof(IList)))
                    {
                        // reference copy
                        copy.SetList(key, value as IList);
                    }
                }
                else
                {
                    copy[key] = value;
                }
                
            }
        }

        

    }
    
}
