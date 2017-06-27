using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amqp.Types;
using Apache.NMS;

namespace NMS.AMQP.Util
{
    class SymbolUtil
    {
        // Open Frame Property Symbols
        public readonly static Symbol CONNECTION_ESTABLISH_FAILED = new Symbol("amqp:connection-establishment-failed");
        public readonly static Symbol OPEN_CAPABILITY_SOLE_CONNECTION_FOR_CONTAINER = new Symbol("sole-connection-for-container");
        public readonly static Symbol OPEN_CAPABILITY_DELAYED_DELIVERY = new Symbol("DELAYED_DELIVERY");

        // Attach Frame 
        public readonly static Symbol ATTACH_EXPIRY_POLICY_LINK_DETACH = new Symbol("link-detach");
        public readonly static Symbol ATTACH_EXPIRY_POLICY_SESSION_END = new Symbol("session-end");
        public readonly static Symbol ATTACH_CAPABILITIES_QUEUE = new Symbol("QUEUE");
        public readonly static Symbol ATTACH_CAPABILITIES_TOPIC = new Symbol("TOPIC");
        public readonly static Symbol ATTACH_CAPABILITIES_TEMP_TOPIC = new Symbol("TEMPORARY-TOPIC");
        public readonly static Symbol ATTACH_CAPABILITIES_TEMP_QUEUE = new Symbol("TEMPORARY-QUEUE");
        public readonly static Symbol ATTACH_DYNAMIC_NODE_PROPERTY_LIFETIME_POLICY = new Symbol("lifetime-policy");

        //JMS Message Annotation Symbols
        public static readonly Symbol JMSX_OPT_MSG_TYPE = new Symbol("x-opt-jms-msg-type");
        public static readonly Symbol JMSX_OPT_DEST = new Symbol("x-opt-jms-dest");
        public static readonly Symbol JMSX_OPT_REPLY_TO = new Symbol("x-opt-jms-reply-to");

        // Frame Property Value
        public readonly static Symbol BOOLEAN_TRUE = new Symbol("true");
        public readonly static Symbol BOOLEAN_FALSE = new Symbol("false");
        public readonly static Symbol DELETE_ON_CLOSE = new Symbol("delete-on-close");

        // Message Content-Type Symbols
        public static readonly Symbol OCTET_STREAM_CONTENT_TYPE = new Symbol(MessageSupport.OCTET_STREAM_CONTENT_TYPE);
        public static readonly Symbol SERIALIZED_JAVA_OBJECT_CONTENT_TYPE = new Symbol(MessageSupport.SERIALIZED_JAVA_OBJECT_CONTENT_TYPE);

        public static bool FieldsHasSymbol(Fields fields, Symbol symbol)
        {
            return (fields!=null && symbol!=null) ? fields.ContainsKey(symbol) : false;
        }

        public static bool CheckAndCompareFields(Fields fields, Symbol key, Symbol expected)
        {
            return (FieldsHasSymbol(fields, key) && expected!=null) ? fields[key].ToString().Equals(expected.ToString()) : false;
        }

        public static Symbol GetSymbolFromFields(Fields fields, Symbol key)
        {
            return (FieldsHasSymbol(fields, key)) ? fields[key] as Symbol : null;
        }

        public static Symbol GetTerminusCapabilitiesForDestination(IDestination destination)
        {
            if (destination.IsQueue)
            {
                if (destination.IsTemporary)
                {
                    return ATTACH_CAPABILITIES_TEMP_QUEUE;
                }
                else
                {
                    return ATTACH_CAPABILITIES_QUEUE;
                }
            }
            else if(destination.IsTopic)
            {
                if (destination.IsTemporary)
                {
                    return ATTACH_CAPABILITIES_TEMP_TOPIC;
                }
                else
                {
                    return ATTACH_CAPABILITIES_TOPIC;
                }
            }
            // unknown destination type...
            return null;
        }

    }
}
