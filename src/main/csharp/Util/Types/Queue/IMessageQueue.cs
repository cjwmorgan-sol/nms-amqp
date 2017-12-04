using System;
using System.Collections;
using System.Collections.Generic;
using NMS.AMQP;
using NMS.AMQP.Util;
using NMS.AMQP.Message;
using Apache.NMS;

namespace NMS.AMQP.Util.Types.Queue
{
    internal interface IMessageDelivery
    {
        Message.Message Message { get; }

        MsgPriority Priority { get; }

        int DeliveryCount { get; }
        bool EnqueueFirst { get; }
    }

    internal interface IMessageQueue : IStartable, IStoppable, ICollection
    {

        void Enqueue(IMessageDelivery message);

        void EnqueueFirst(IMessageDelivery message);

        IMessageDelivery Dequeue();

        IMessageDelivery Dequeue(int timeout);

        IMessageDelivery DequeueNoWait();

        IMessageDelivery Peek();

        IList<IMessageDelivery> RemoveAll();

        void Clear();
        
        bool IsEmpty { get; }

        bool IsClosed { get; }

        void Close();

    }
}