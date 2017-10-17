using System;
using System.Runtime.Serialization;

namespace NMS.AMQP.Message.Cloak
{
    internal enum AMQPObjectEncodingType
    {
        UNKOWN = -1,
        AMQP_TYPE = 0,
        DOTNET_SERIALIZABLE = 1,
        JAVA_SERIALIZABLE = 2,
    }
    interface IObjectMessageCloak : IMessageCloak
    {
        new IObjectMessageCloak Copy();
        object Body { get; set; }

        AMQPObjectEncodingType Type { get; }
    }
}