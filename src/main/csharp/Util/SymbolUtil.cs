using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amqp.Types;

namespace NMS.AMQP.Util
{
    class SymbolUtil
    {
        public readonly static Symbol CONNECTION_ESTABLISH_FAILED = new Symbol("amqp:connection-establishment-failed");
        public readonly static Symbol OPEN_CAPABILITY_SOLE_CONNECTION_FOR_CONTAINER = new Symbol("sole-connection-for-container");
        public readonly static Symbol OPEN_CAPABILITY_DELAYED_DELIVERY = new Symbol("DELAYED_DELIVERY");
        public readonly static Symbol BOOLEAN_TRUE = new Symbol("true");
        public readonly static Symbol BOOLEAN_FALSE = new Symbol("false");

        public static bool FieldsHasSymbol(Fields fields, Symbol symbol)
        {
            return (fields!=null && symbol!=null) ? fields.ContainsKey(symbol) : false;
        }

        public static bool CheckAndCompareFields(Fields fields, Symbol key, Symbol expected)
        {
            return (FieldsHasSymbol(fields, key) && expected!=null) ? fields[key].ToString().Equals(expected.ToString()) : false;
        }

        public static Symbol GetSymbolFromFields(Fields fields, Symbol key)
        {
            return (FieldsHasSymbol(fields, key)) ? fields[key] as Symbol : null;
        }
    }
}
