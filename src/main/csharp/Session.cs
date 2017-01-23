using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;


namespace NMS.AMQP
{
    class Session : ISession
    {
        public AcknowledgementMode AcknowledgementMode
        {
            get
            {
                return AcknowledgementMode.AutoAcknowledge;
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

        public bool Transacted
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event SessionTxEventDelegate TransactionCommittedListener;
        public event SessionTxEventDelegate TransactionRolledBackListener;
        public event SessionTxEventDelegate TransactionStartedListener;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public IQueueBrowser CreateBrowser(IQueue queue)
        {
            throw new NotImplementedException();
        }

        public IQueueBrowser CreateBrowser(IQueue queue, string selector)
        {
            throw new NotImplementedException();
        }

        public IBytesMessage CreateBytesMessage()
        {
            throw new NotImplementedException();
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateConsumer(IDestination destination)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal)
        {
            throw new NotImplementedException();
        }

        public IMapMessage CreateMapMessage()
        {
            throw new NotImplementedException();
        }

        public IMessage CreateMessage()
        {
            throw new NotImplementedException();
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateProducer()
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateProducer(IDestination destination)
        {
            throw new NotImplementedException();
        }

        public IStreamMessage CreateStreamMessage()
        {
            throw new NotImplementedException();
        }

        public ITemporaryQueue CreateTemporaryQueue()
        {
            throw new NotImplementedException();
        }

        public ITemporaryTopic CreateTemporaryTopic()
        {
            throw new NotImplementedException();
        }

        public ITextMessage CreateTextMessage()
        {
            throw new NotImplementedException();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            throw new NotImplementedException();
        }

        public void DeleteDestination(IDestination destination)
        {
            throw new NotImplementedException();
        }

        public void DeleteDurableConsumer(string name)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IQueue GetQueue(string name)
        {
            throw new NotImplementedException();
        }

        public ITopic GetTopic(string name)
        {
            throw new NotImplementedException();
        }

        public void Recover()
        {
            throw new NotImplementedException();
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }
    }
}
