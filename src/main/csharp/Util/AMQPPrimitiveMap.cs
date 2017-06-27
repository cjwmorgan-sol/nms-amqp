using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using Amqp.Types;
using Amqp.Framing;

namespace NMS.AMQP.Util
{
    class AMQPPrimitiveMap : PrimitiveMapBase
    {

        private readonly object syncLock = new object(); 
        private readonly DescribedMap properties;

        internal AMQPPrimitiveMap(DescribedMap map)
        {
            properties = map;
        }

        public override int Count
        {
            get
            {
                return properties.Map.Count;
            }
        }

        public override ICollection Keys
        {
            get
            {
                lock (SyncRoot)
                {
                    return new ArrayList(properties.Map.Keys);
                }
            }
        }

        public override ICollection Values
        {
            get
            {
                lock (SyncRoot)
                {
                    return new ArrayList(properties.Map.Values);
                }
            }
        }

        public override void Remove(object key)
        {
            properties.Map.Remove(key);
        }

        public override void Clear()
        {
            properties.Map.Clear();
        }

        public override bool Contains(object key)
        {
            return properties.Map.ContainsKey(key);
        }

        protected override object SyncRoot
        {
            get
            {
                return syncLock;
            }
        }

        protected override object GetObjectProperty(string key)
        {
            return this.properties[key];
        }

        protected override void SetObjectProperty(string key, object value)
        {
            this.properties[key] = value;
        }

    }
}
