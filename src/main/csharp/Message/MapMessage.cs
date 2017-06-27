using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP.Message.Cloak;

namespace NMS.AMQP.Message
{
    class MapMessage : Message, IMapMessage
    {
        new private readonly IMapMessageCloak cloak;
        private PrimitiveMapInterceptor map;

        internal MapMessage(IMapMessageCloak message) : base(message)
        {
            cloak = message;
        }

        public override bool IsReadOnly
        {
            get { return base.IsReadOnly; }
            protected set
            {
                if (map != null)
                {
                    map.ReadOnly = value;
                }
                base.IsReadOnly = value;
            }
        }

        public IPrimitiveMap Body
        {
            get
            {
                if(map == null)
                {
                    map = new PrimitiveMapInterceptor(this, cloak.Map, IsReadOnly, true);
                }
                return map;
            }
        }
    }
}
