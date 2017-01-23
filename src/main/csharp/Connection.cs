using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
    class Connection : IConnection
    {
        public AcknowledgementMode AcknowledgementMode
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

        public string ClientId
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

        public bool IsStarted
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IConnectionMetaData MetaData
        {
            get
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

        public TimeSpan RequestTimeout
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

        public event ConnectionInterruptedListener ConnectionInterruptedListener;
        public event ConnectionResumedListener ConnectionResumedListener;
        public event ExceptionListener ExceptionListener;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public ISession CreateSession()
        {
            throw new NotImplementedException();
        }

        public ISession CreateSession(AcknowledgementMode acknowledgementMode)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void PurgeTempDestinations()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
