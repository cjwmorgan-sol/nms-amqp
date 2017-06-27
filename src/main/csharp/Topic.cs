using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP
{
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

    class TemporaryTopic :  TemporaryDestination, ITemporaryTopic
    {
        #region Constructor

        internal TemporaryTopic(Connection conn) : base(conn, conn.SessionIdGenerator.generateID(), false) { }

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
            
        }

        #endregion 
    }
}
