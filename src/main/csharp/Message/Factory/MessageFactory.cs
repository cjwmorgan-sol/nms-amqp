using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP;

namespace NMS.AMQP.Message.Factory
{
    internal abstract class MessageFactory : IMessageFactory
    {
        private static readonly IDictionary<NMSResource, IMessageFactory> resgistry;

        static MessageFactory()
        {
            resgistry = new ConcurrentDictionary<NMSResource, IMessageFactory>();
        }

        public static void Register(Connection resource)
        {
            if (resource is Connection)
            {
                resgistry.Add(resource, (new AMQPMessageFactory(resource)) as IMessageFactory);
            }
            else
            {
                throw new NMSException("Invalid Message Factory Type " + resource.GetType().FullName);
            }
        }

        public static IMessageFactory Instance(Connection resource)
        {
            IMessageFactory factory = null;
            resgistry.TryGetValue(resource, out factory);
            if(factory == null)
            {
                throw new NMSException("Resource "+resource+" is not registered as message factory.");
            }
            return factory;
        }


        protected readonly NMSResource parent;

        protected  MessageFactory(NMSResource resource)
        {
            parent = resource;
        }

        public abstract MessageTransformation GetTransformFactory();
        public abstract IMessage CreateMessage();
        public abstract ITextMessage CreateTextMessage();
        public abstract ITextMessage CreateTextMessage(string text);
        public abstract IMapMessage CreateMapMessage();
        public abstract IObjectMessage CreateObjectMessage(object body);
        public abstract IBytesMessage CreateBytesMessage();
        public abstract IBytesMessage CreateBytesMessage(byte[] body);
        public abstract IStreamMessage CreateStreamMessage();
        
    }
}
