using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;

namespace NMS.AMQP.Message.Cloak
{
    interface IMapMessageCloak : IMessageCloak
    {
        IPrimitiveMap Map { get; }
        new IMapMessageCloak Copy();
    }
}
