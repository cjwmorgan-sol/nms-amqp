using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
    /// <summary>
    /// NMS.AMQP.Topic implements Apache.NMS.ITopic
    /// Topic is an concrete implementation for an abstract Destination.
    /// </summary>
    class Topic : Destination, ITopic
    {
        
        #region Constructor

        internal Topic(Connection conn, string topicString) : base(conn, topicString, false) {}

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
                return DestinationType.Topic;
            }
        }

        #endregion

        #region ITopic Properties

        public string TopicName
        {
            get
            {
                return destinationName;
            }
        }

        #endregion

        #region IDisposable Methods

        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// NMS.AMQP.TemporaryTopic implements Apache.NMS.ITemporaryTopic.
    /// TemporaryTopic is an concrete implementation for an abstract TemporaryDestination.
    /// </summary>
    class TemporaryTopic :  TemporaryDestination, ITemporaryTopic
    {
        #region Constructor

        internal TemporaryTopic(Connection conn) : base(conn, conn.TemporaryTopicGenerator.GenerateId(), false) { }

        internal TemporaryTopic(Connection conn, string destinationName) : base(conn, destinationName, false) { }

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
                return DestinationType.TemporaryTopic;
            }
        }

        #endregion

        #region ITopic Properties

        public string TopicName
        {
            get
            {
                return destinationName;
            }
        }

        #endregion

        #region TemporaryDestination Methods

        public override void Delete()
        {
            base.Delete();
        }

        #endregion 
    }
}
