using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
    abstract class Destination : IDestination
    {

        protected readonly string destinationName;
        protected readonly Connection connection;
        private readonly bool queue;

        #region Constructor

        internal Destination(Connection conn, string name, bool isQ)
        {
            queue = isQ;
            ValidateName(name);
            destinationName = name;
            connection = conn;
            
        }

        internal Destination(Destination other)
        {
            this.queue = other.queue;
            destinationName = other.destinationName;
            connection = other.connection;
        }

        #endregion

        #region Abstract Methods

        protected abstract void ValidateName(string name);

        #endregion

        #region IDestination Properties

        public virtual DestinationType DestinationType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool IsQueue
        {
            get
            {
                return queue;
            }
        }

        public virtual bool IsTemporary
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsTopic
        {
            get
            {
                return !queue;
            }
        }

        #endregion

        #region IDisposable Methods

        public virtual void Dispose()
        {
            
        }

        #endregion
    }

    abstract class TemporaryDestination : Destination
    {
        #region Constructor

        public TemporaryDestination(Connection conn, string name, bool isQ) : base(conn, name, isQ)
        {
        }

        internal TemporaryDestination(TemporaryDestination other) : base(other) { }

        #endregion

        internal Connection Connection
        {
            get { return connection; }
        }

        #region Absract Methods

        public abstract void Delete();

        #endregion

        #region IDestination Methods

        public override bool IsTemporary
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region IDisposable Methods

        public override void Dispose()
        {
            this.Delete();
            base.Dispose();
        }

        #endregion
    }
}
