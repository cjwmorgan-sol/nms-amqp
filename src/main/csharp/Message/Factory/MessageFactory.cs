using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP;
using NMS.AMQP.Util;

namespace NMS.AMQP.Message.Factory
{
    internal abstract class MessageFactory<T> : IMessageFactory where T : ResourceInfo
    {
        private static readonly IDictionary<Id, IMessageFactory> resgistry;

        static MessageFactory()
        {
            resgistry = new ConcurrentDictionary<Id, IMessageFactory>();
        }

        public static void Register(NMSResource<T> resource)
        {
            if (resource is Connection)
            {
                resgistry.Add(resource.Id, (new AMQPMessageFactory<ConnectionInfo>(resource as Connection)) as IMessageFactory);
            }
            else
            {
                throw new NMSException("Invalid Message Factory Type " + resource.GetType().FullName);
            }
        }

        public static void Unregister(NMSResource<T> resource)
        {
            if(resource != null && resource.Id != null)
            {
                if(!resgistry.Remove(resource.Id))
                {
                    if(resgistry.ContainsKey(resource.Id))
                        Tracer.WarnFormat("MessageFactory was not able to unregister resource {0}.", resource.Id);
                }
            }
        }

        public static IMessageFactory Instance(Connection resource)
        {
            IMessageFactory factory = null;
            resgistry.TryGetValue(resource.Id, out factory);
            if(factory == null)
            {
                throw new NMSException("Resource "+resource+" is not registered as message factory.");
            }
            return factory;
        }
        
        protected readonly NMSResource<T> parent;

        protected  MessageFactory(NMSResource<T> resource)
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
