using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;

namespace NMS.AMQP.Message.Factory
{
    interface IMessageFactory 
    {
        MessageTransformation GetTransformFactory();

        // Factory methods to create messages

        /// <summary>
        /// Creates a new message with an empty body
        /// </summary>
        IMessage CreateMessage();

        /// <summary>
        /// Creates a new text message with an empty body
        /// </summary>
        ITextMessage CreateTextMessage();

        /// <summary>
        /// Creates a new text message with the given body
        /// </summary>
        ITextMessage CreateTextMessage(string text);

        /// <summary>
        /// Creates a new Map message which contains primitive key and value pairs
        /// </summary>
        IMapMessage CreateMapMessage();

        /// <summary>
        /// Creates a new Object message containing the given .NET object as the body
        /// </summary>
        IObjectMessage CreateObjectMessage(object body);

        /// <summary>
        /// Creates a new binary message
        /// </summary>
        IBytesMessage CreateBytesMessage();

        /// <summary>
        /// Creates a new binary message with the given body
        /// </summary>
        IBytesMessage CreateBytesMessage(byte[] body);

        /// <summary>
        /// Creates a new stream message
        /// </summary>
        IStreamMessage CreateStreamMessage();

        
    }
}
