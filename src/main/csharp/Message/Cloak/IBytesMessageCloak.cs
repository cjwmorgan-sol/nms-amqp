using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NMS.AMQP.Message.Cloak
{
    interface IBytesMessageCloak : IMessageCloak
    {
        BinaryReader DataIn { get; }
        BinaryWriter DataOut { get; }

        new IBytesMessageCloak Copy();

        int BodyLength { get; }
        void Reset();
    }
}
