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

        bool IsBodyReadOnly { get; set; }

        bool IsPropertiesReadOnly { get; set; }

        bool IsReceived { get; }

        IMessageCloak Copy();

        object GetMessageAnnotation(string symbolKey);

        void SetMessageAnnotation(string symbolKey, object value);

        object GetDeliveryAnnotation(string symbolKey);

        void SetDeliveryAnnotation(string symbolKey, object value);

        int DeliveryCount { get; set; }

        int RedeliveryCount { get; set; }

        MessageAcknowledgementHandler AckHandler { get; set; }
        
    }
}
