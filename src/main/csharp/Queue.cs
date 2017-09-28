﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
    /// <summary>
    /// NMS.AMQP.Queue implements Apache.NMS.IQueue
    /// Queue is an concrete implementation for an abstract Destination.
    /// </summary>
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

    /// <summary>
    /// NMS.AMQP.TemporaryQueue implements Apache.NMS.ITemporaryQueue
    /// TemporaryQueue is an concrete implementation for an abstract TemporaryDestination.
    /// </summary>
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