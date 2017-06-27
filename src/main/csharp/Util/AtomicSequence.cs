using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS.Util;

namespace NMS.AMQP.Util
{
    class AtomicSequence : Atomic<ulong>
    {
        public AtomicSequence() : base()
        {
        }

        public AtomicSequence(ulong defaultValue) : base(defaultValue)
        {
        }

        public ulong getAndIncrement()
        {
            ulong val = 0;
            lock (this)
            {
                val = atomicValue;
                atomicValue++;
            }
            return val;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
