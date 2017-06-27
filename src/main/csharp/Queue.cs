using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
    class Queue : Destination, IQueue
    {
        
        #region Constructor

        internal Queue(Connection conn, string queueString) : base(conn, queueString, true)
        {}

        #endregion

        #region Destination Methods

        protected override void ValidateName(string name)
        {
            
        }

        #endregion

        #region Destination Properties

        public override DestinationType DestinationType
        {
            get
            {
                return DestinationType.Queue;
            }
        }

        #endregion

        #region IQueue Properties

        public string QueueName
        {
            get
            {
                return destinationName;
            }
        }

        #endregion
        
        #region IDisposable

        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion
    }

    class TemporaryQueue : TemporaryDestination, ITemporaryQueue
    {
        #region Constructor

        internal TemporaryQueue(Connection conn) : base(conn, conn.SessionIdGenerator.generateID(), true) { }

        internal TemporaryQueue(Connection conn, string destinationName) : base(conn, destinationName, true) { }

        #endregion

        #region Destination Methods

        protected override void ValidateName(string name)
        {
            
        }

        #endregion

        #region Destination Properties

        public override DestinationType DestinationType
        {
            get
            {
                return DestinationType.TemporaryQueue;
            }
        }

        #endregion

        #region IQueue Properties

        public string QueueName
        {
            get
            {
                return destinationName;
            }
        }

        #endregion

        #region ITemporaryQueue Methods

        public override void Delete()
        {
            // session.remove(this);
            
        }

        #endregion
    }

}
