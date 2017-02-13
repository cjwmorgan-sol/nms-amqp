using Apache.NMS;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NMS.AMQP.Util
{
    class PropertyUtil
    {
        private const string PROPERTY_TERM_SEPARATOR = ".";
        public const string PROPERTY_PREFIX = "NMS" + PROPERTY_TERM_SEPARATOR;
        

        public static string CreateProperty(string name, string subprefix = "")
        {
            string subPropertyTerm = subprefix + 
                (subprefix.Length > 0 && !subprefix.EndsWith(PROPERTY_TERM_SEPARATOR) 
                    ? 
                    PROPERTY_TERM_SEPARATOR 
                    : 
                    ""
                );
            return PROPERTY_PREFIX + subPropertyTerm + name;
        }

        public static void SetProperties(object obj, StringDictionary properties, string propertyPrefix = PROPERTY_PREFIX)
        {
            Dictionary<string, PropertyInfo> props = getPropertiesForClass(obj);
            foreach (string rawkey in properties.Keys)
            {
                string key = removePrefix(propertyPrefix, rawkey);
                Tracer.DebugFormat("Searching for Property: \"{0}\"", key);
                if (props.ContainsKey(key))
                {
                    Tracer.DebugFormat(
                        "Assigning Property {0} to {1}.{2} with value {3}", 
                        key, obj.GetType().Namespace, obj.GetType().Name, properties[rawkey]
                        );
                    props[key].SetValue(obj, ConvertType(props[key].PropertyType ,properties[rawkey]));
                }

            }
        }

        public static StringDictionary GetProperties(object obj, string propertyPrefix = PROPERTY_PREFIX)
        {
            StringDictionary result = new StringDictionary();
            Dictionary<string, PropertyInfo> props = getPropertiesForClass(obj);
            string propsPrefix = propertyPrefix + 
                ( 
                propertyPrefix.Length > 0 && !propertyPrefix.EndsWith(PROPERTY_TERM_SEPARATOR) 
                ? 
                    PROPERTY_TERM_SEPARATOR 
                : 
                    ""
                );
            foreach (string key in props.Keys)
            {
                object value = props[key].GetValue(obj);
                if (value != null)
                {
                    result[propertyPrefix + key] = value.ToString();
                }
            }
            return result;
        }

        private static object ConvertType(Type targetType, string value)
        {
            if (targetType.IsPrimitive)
            {
                return Convert.ChangeType(value, targetType);
            }
            return null;
        }

        private static string removePrefix(string prefix, string propertyName)
        {
            if (propertyName.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
            {
                return propertyName.Remove(0, prefix.Length);
            }
            else
            {
                return propertyName;
            }
        }

        private static Dictionary<string, PropertyInfo> getPropertiesForClass(object obj)
        {
            MemberInfo[] members = obj.GetType().GetMembers();
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
            foreach (MemberInfo member in members)
            {
                string memberName = member.Name;
                if (member != null && member is PropertyInfo)
                {
                    PropertyInfo prop = member as PropertyInfo;
                    properties.Add(memberName.ToLower(), prop);
                }
            }
            return properties;
        }

        public static StringDictionary Clone(StringDictionary original)
        {
            StringDictionary clone = new StringDictionary();
            foreach(string key in original.Keys)
            {
                clone.Add(key.Clone() as string, original[key].Clone() as string);
            }
            return clone;
        }
    }
}
