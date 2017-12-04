using System;
using System.Collections;
using System.Collections.Generic;
using NMS.AMQP;
using NMS.AMQP.Message;
using System.Threading;

namespace NMS.AMQP.Util.Types.Queue
{
    internal class FIFOMessageQueue : MessageQueueBase
    {
        
        protected LinkedList<IMessageDelivery> list;
        
        internal FIFOMessageQueue() : base()
        {
            list = new LinkedList<IMessageDelivery>();
        }
        

        public override int Count { get { return list.Count; } }

        
        public override void Clear()
        {
            list.Clear();
        }

        public override void CopyTo(Array array, int index)
        {
            int i = index;
            lock (SyncRoot)
            {
                foreach (MessageDelivery message in list)
                {
                    array.SetValue(message, i);
                    i++;
                }
            }
        }

        public override void Enqueue(IMessageDelivery message)
        {
            lock (SyncRoot)
            {
                list.AddLast(message);
                Monitor.PulseAll(SyncRoot);
            }
        }

        public override void EnqueueFirst(IMessageDelivery message)
        {
            lock (SyncRoot)
            {
                list.AddFirst(message);
                Monitor.PulseAll(SyncRoot);
            }
        }

        public override IList<IMessageDelivery> RemoveAll()
        {
            lock (SyncRoot)
            {
                IList<IMessageDelivery> result = new List<IMessageDelivery>(this.Count);
                foreach(MessageDelivery message in list)
                {
                    result.Add(message);
                }
                list.Clear();
                return result;
            }
        }

        protected override IMessageDelivery PeekFirst()
        {
            return list.First.Value;
        }

        protected override IMessageDelivery RemoveFirst()
        {
            IMessageDelivery first = list.First.Value;
            list.RemoveFirst();
            return first;
        }
        
    }
}