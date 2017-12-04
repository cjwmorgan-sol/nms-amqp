using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP;
using Amqp;

namespace NMS.AMQP.Message.Factory
{
    using Cloak;
    using AMQP;
    class AMQPMessageFactory<T> : MessageFactory<T> where T : ConnectionInfo
    {

        protected readonly AMQPMessageTransformation<T> transformFactory;
        protected AMQPObjectEncodingType encodingType = AMQPObjectEncodingType.UNKOWN;

        internal AMQPMessageFactory(NMSResource<T> resource) : base(resource)
        {
            transformFactory = new AMQPMessageTransformation<T>(this);
            InitEncodingType();
        }

        internal MessageTransformation TransformFactory { get { return transformFactory; } }
        
        internal Connection Parent { get { return parent as Connection; } }

        public override MessageTransformation GetTransformFactory()
        {
            return transformFactory;
        }

        public override IBytesMessage CreateBytesMessage()
        {
            IBytesMessageCloak cloak = new AMQPBytesMessageCloak(Parent);
            return new BytesMessage(cloak);
        }

        public override IBytesMessage CreateBytesMessage(byte[] body)
        {
            IBytesMessage msg = CreateBytesMessage();
            msg.WriteBytes(body);
            return msg;
        }

        public override IMapMessage CreateMapMessage()
        {
            IMapMessageCloak cloak = new AMQPMapMessageCloak(Parent);
            return new MapMessage(cloak);
        }

        public override IMessage CreateMessage()
        {
            IMessageCloak cloak = new AMQPMessageCloak(Parent);
            return new Message(cloak);
        }

        public override IObjectMessage CreateObjectMessage(object body)
        {
            IObjectMessageCloak cloak = new AMQPObjectMessageCloak(Parent, encodingType);
            return new ObjectMessage(cloak) { Body=body };
        }

        public override IStreamMessage CreateStreamMessage()
        {
            IStreamMessageCloak cloak = new AMQPStreamMessageCloak(Parent);
            return new StreamMessage(cloak);
        }

        public override ITextMessage CreateTextMessage()
        {
            ITextMessageCloak cloak = new AMQPTextMessageCloak(Parent);
            return new TextMessage(cloak);
        }

        public override ITextMessage CreateTextMessage(string text)
        {
            ITextMessage msg = CreateTextMessage();
            msg.Text = text;
            return msg;
        }

        private void InitEncodingType()
        {
            encodingType = ConnectionEncodingType(Parent);
            Tracer.InfoFormat("Message Serialization for connection : {0}, is set to: {1}.", Parent.ClientId, encodingType.ToString());
        }


        private const string AMQP_TYPE = "amqp";
        private const string DOTNET_TYPE = "dotnet";
        private const string JAVA_TYPE = "java";

        private static AMQPObjectEncodingType ConnectionEncodingType(Connection connection)
        {
            string value = connection.Properties[Connection.MESSAGE_OBJECT_SERIALIZATION_PROP];
            if (value == null) return AMQPObjectMessageCloak.DEFAULT_ENCODING_TYPE;
            if (value.ToLower().StartsWith(AMQP_TYPE))
            {
                return AMQPObjectEncodingType.AMQP_TYPE;
            }
            else if (value.ToLower().StartsWith(DOTNET_TYPE))
            {
                return AMQPObjectEncodingType.DOTNET_SERIALIZABLE;
            }
            else if (value.ToLower().StartsWith(JAVA_TYPE))
            {
                return AMQPObjectEncodingType.JAVA_SERIALIZABLE;
            }
            else
            {
                return AMQPObjectMessageCloak.DEFAULT_ENCODING_TYPE;
            }
        }

    }
}
