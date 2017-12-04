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

namespace NMS.AMQP.Util.Types.Map.AMQP
{
    /// <summary>
    /// A Utility class used to bridge the PrimativeMapBase from Apache.NMS.Util to the AmqpNetLite Map class.
    /// This enables the Apache.NMS.Util Methods/class for IPrimativeMap to interact directly with AmqpNetLite AmqpValue for Maps.
    /// </summary>
    class AMQPValueMap : PrimitiveMapBase
    {

        private readonly object syncLock = new object(); 
        private readonly Amqp.Types.Map value;

        internal AMQPValueMap(Amqp.Types.Map map)
        {
            value = map;
        }

        internal Amqp.Types.Map AmqpMap { get { return value; } }

        public override int Count
        {
            get
            {
                return value.Count;
            }
        }

        public override ICollection Keys
        {
            get
            {
                lock (SyncRoot)
                {
                    return new ArrayList(value.Keys);
                }
            }
        }

        public override ICollection Values
        {
            get
            {
                lock (SyncRoot)
                {
                    return new ArrayList(value.Values);
                }
            }
        }

        public override void Remove(object key)
        {
            value.Remove(key);
        }

        public override void Clear()
        {
            value.Clear();
        }

        public override bool Contains(object key)
        {
            return value.ContainsKey(key);
        }

        internal override object SyncRoot
        {
            get
            {
                return syncLock;
            }
        }

        /// <summary>
        /// Gets associate value from the underlying map implementation.
        /// </summary>
        /// <param name="key">Key to associated value.</param>
        /// <returns>Value for given Key.</returns>
        protected override object GetObjectProperty(string key)
        {
            return this.value[key];
        }

        /// <summary>
        /// Sets associate value to the underlying map implementation.
        /// </summary>
        /// <param name="key">Key to associated value.</param>
        /// <param name="value">Value to set.</param>
        protected override void SetObjectProperty(string key, object value)
        {
            object objval = value;
            if(objval is IDictionary)
            {
                objval = ConversionSupport.MapToAmqp(value as IDictionary);
            }
            else if (objval is IList)
            {
                objval = ConversionSupport.ListToAmqp(value as IList);
            }
            this.value[key] = objval;
        }

    }
}
