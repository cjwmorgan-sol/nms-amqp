using System.IO;

namespace NMS.AMQP.Message.Cloak
{
    interface IStreamMessageCloak : IMessageCloak
    {


        new IStreamMessageCloak Copy();

        bool HasNext { get; }

        void Reset();

        void Put(object value);

        object Peek();

        void Pop();

        
    }
}