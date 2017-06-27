using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;


namespace NMS.AMQP.Message.Cloak
{
    /// <summary>
    /// Provider specific Cloak Interface from provider implementation.
    /// </summary>
    interface IMessageCloak : IMessage
    {
        byte[] Content
        {
            get;
            set;
        }

        bool IsReceived { get; }

        IMessageCloak Copy();

        object GetMessageId();
        void SetMessageId(object messageId);

        object GetCorrelationId();
        void SetCorrelationId(object correlationId);

    }
}
