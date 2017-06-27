using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMS.AMQP.Message.Cloak
{
    interface ITextMessageCloak : IMessageCloak
    {
        string Text { get; set; }
        new ITextMessageCloak Copy();
    }
}
