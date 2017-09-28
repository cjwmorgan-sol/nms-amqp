using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
    /// <summary>
    /// Stub for now.
    /// </summary>
    class MessageConsumer : IMessageConsumer
    {

        private readonly Session session;
        private readonly IDestination destination;

        #region Constructor

        internal MessageConsumer(Session ses, IDestination dest)
        {
            session = ses;
            destination = dest;
        }

        #endregion

        #region Internal Properties

        internal Session Session { get { return session; } }

        #endregion

        #region IMessageConsumer Properties

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

        #endregion

        #region IMessageConsumer Events

        public event MessageListener Listener;

        #endregion

        #region IMessageConsumer Methods

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IMessage Receive()
        {
            throw new NotImplementedException();
        }

        public IMessage Receive(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public IMessage ReceiveNoWait()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
