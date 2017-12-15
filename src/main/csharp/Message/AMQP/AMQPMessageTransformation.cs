using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP.Message.Factory;

namespace NMS.AMQP.Message.AMQP
{
    class AMQPMessageTransformation <T> : MessageTransformation where T:ConnectionInfo
    {
        protected readonly Connection connection;
        protected readonly MessageFactory<T> factory;

        public AMQPMessageTransformation(AMQPMessageFactory<T> fact) : base()
        {
            connection = fact.Parent;    
            factory = fact;
        }

        protected override IBytesMessage DoCreateBytesMessage()
        {
            return factory.CreateBytesMessage();
        }

        protected override IMapMessage DoCreateMapMessage()
        {
            return factory.CreateMapMessage();
        }

        protected override IMessage DoCreateMessage()
        {
            return factory.CreateMessage();
        }

        protected override IObjectMessage DoCreateObjectMessage()
        {
            return factory.CreateObjectMessage(null);
        }

        protected override IStreamMessage DoCreateStreamMessage()
        {
            return factory.CreateStreamMessage();
        }

        protected override ITextMessage DoCreateTextMessage()
        {
            return factory.CreateTextMessage();
        }

        protected override void DoPostProcessMessage(IMessage message)
        {
            // nothing for now
        }

        protected override IDestination DoTransformDestination(IDestination destination)
        {
            return DestinationTransformation.Transform(connection, destination);
        }
    }
}
