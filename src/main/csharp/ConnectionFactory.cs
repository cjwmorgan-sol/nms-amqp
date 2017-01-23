using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
    class ConnectionFactory : IConnectionFactory
    {
        public Uri BrokerUri
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

        public ConsumerTransformerDelegate ConsumerTransformer
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

        public IRedeliveryPolicy RedeliveryPolicy
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

        public IConnection CreateConnection()
        {
            throw new NotImplementedException();
        }

        public IConnection CreateConnection(string userName, string password)
        {
            throw new NotImplementedException();
        }
    }
}
