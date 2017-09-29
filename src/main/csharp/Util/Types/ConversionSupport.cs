using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amqp.Types;
using Amqp;
using Apache.NMS;

namespace NMS.AMQP.Util.Types.Map.AMQP
{
    internal class ConversionSupport
    {
        public static Amqp.Types.Map MapToAmqp(IDictionary dictionary)
        {
            if (dictionary == null) return null;
            if(dictionary is Amqp.Types.Map)
            {
                Amqp.Types.Map DictMap = dictionary as Amqp.Types.Map;
                return DictMap.Clone() as Amqp.Types.Map;
            }
            Amqp.Types.Map map = new Amqp.Types.Map();
            IEnumerator iterator = dictionary.Keys.GetEnumerator();
            object key = iterator.Current;
            if (key == null) return null;
            object value = null;
            do 
            {
                value = dictionary[key];
                if (value != null)
                {
                    Type valtype = value.GetType();
                    if(value is IDictionary)
                    {
                        map[key] = ConversionSupport.MapToAmqp(value as IDictionary);
                    }
                    else if (value is IList)
                    {
                        map[key] = ConversionSupport.ListToAmqp(value as IList);
                    }
                    else if (valtype.IsPrimitive || value is byte[])
                    {
                        map[key] = value;
                    }
                    else
                    {
                        Tracer.InfoFormat("Failed to convert IDictionary to Map value: Invalid Type: {0}", valtype.Name);
                    }
                    
                }
                
            }
            while (iterator.MoveNext() && (key = iterator.Current) != null) ;
            return map;
        }
        

        public static IDictionary MapToNMS(Amqp.Types.Map map)
        {
            if (map == null) return null;
            IDictionary dictionary = map.Clone() as IDictionary;

            return dictionary;
        }

        public static List ListToAmqp(IList ilist)
        {
            if (ilist == null) return null;
            List list = new List();
            list.AddRange(ilist);
            return list;
        }

        public static IList ListToNMS(List list)
        {
            if (list == null) return null;
            IList ilist = new ArrayList(list);
            return ilist;
        }

        public static string ToString(Amqp.Types.Map map)
        {
            if (map == null) return "{}";
            string result = "{";
            bool first = true;
            foreach (object key in map.Keys)
            {
                if (first) result += "\n";
                first = false;
                result += "key: " + key.ToString() + ", value: " + map[key].ToString() + ";\n";
            }
            result += "}";
            return result;
        }

        public static string ToString(IPrimitiveMap map)
        {
            if (map == null) return "{}";
            string result = "{";
            bool first = true;
            foreach (string key in map.Keys)
            {
                if (first) result += "\n";
                first = false;
                result += "key: " + key.ToString() + ", value: " + map[key].ToString() + ";\n";
            }
            result += "}";
            return result;
        }
        
    }
}
