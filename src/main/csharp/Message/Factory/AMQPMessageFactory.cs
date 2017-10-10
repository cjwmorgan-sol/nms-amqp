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
    class AMQPMessageFactory : MessageFactory
    {

        protected readonly AMQPMessageTransformation transformFactory;

        internal AMQPMessageFactory(NMSResource resource) : base(resource)
        {
            transformFactory = new AMQPMessageTransformation(this);
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
            return null;
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

    }
}
