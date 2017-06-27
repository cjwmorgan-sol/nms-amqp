using Apache.NMS;
using System;
using System.Collections;
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
                    props[key].SetValue(obj, ConvertType(props[key].PropertyType, properties[rawkey]));
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
            foreach (string key in original.Keys)
            {
                clone.Add(key.Clone() as string, original[key].Clone() as string);
            }
            return clone;
        }

        /// <summary>
        /// See <see cref="Merge(StringDictionary, StringDictionary, out StringDictionary, string, string, string)"/>
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <param name="onePrefix"></param>
        /// <param name="otherPrefix"></param>
        /// <param name="mergePrefix"></param>
        /// <returns></returns>
        public static StringDictionary Merge(
            StringDictionary one, 
            StringDictionary other, 
            string onePrefix = PROPERTY_PREFIX, 
            string otherPrefix = PROPERTY_PREFIX,
            string mergePrefix = PROPERTY_PREFIX
            )
        {
            StringDictionary d;
            return Merge(one, other, out d, onePrefix, otherPrefix, mergePrefix);
        }


        /// <summary>
        /// Takes all properties from one StringDictionary and merges them with the other StringDictionary.
        /// The properties in "one" are prefered over the "other".
        /// 
        /// </summary>
        /// <param name="one">StringDictionary containing properties.</param>
        /// <param name="other">Another StringDictionary containing properties.</param>
        /// <param name="cross">Holds all the properties from the "other" StringDictionary that are not used because because one has the properties.</param>
        /// <param name="onePrefix">Optional string prefix for the properties in "one".</param>
        /// <param name="otherPrefix">Optional string prefix for the properties in "other".</param>
        /// <param name="mergePrefix">Optional string prefix for the properties in result.</param>
        /// <returns>Merged StringDictionary with properties from both "one" and "other".</returns>
        public static StringDictionary Merge(
            StringDictionary one, 
            StringDictionary other, 
            out StringDictionary cross, 
            string onePrefix = PROPERTY_PREFIX, 
            string otherPrefix = PROPERTY_PREFIX,
            string mergePrefix = PROPERTY_PREFIX
            )
        {
            if (one == null && other != null)
            {
                cross = null;
                return Clone(other);
            }
            else if (one!=null && other == null)
            {
                cross = null;
                return Clone(one);
            }
            else if (one == null && other == null)
            {
                cross = null;
                return new StringDictionary();
            }
            StringDictionary result = new StringDictionary();
            StringDictionary dups = new StringDictionary();
            
            Array arr = new string[other.Keys.Count];
            other.Keys.CopyTo(arr, 0);
            ArrayList otherKeys = new ArrayList(arr); 

            string mPre = mergePrefix +
                (
                mergePrefix.Length > 0 && !mergePrefix.EndsWith(PROPERTY_TERM_SEPARATOR)
                ?
                    PROPERTY_TERM_SEPARATOR
                :
                    ""
                );
            mergePrefix.ToLower();

            string onePre = onePrefix +
                (
                onePrefix.Length > 0 && !onePrefix.EndsWith(PROPERTY_TERM_SEPARATOR)
                ?
                    PROPERTY_TERM_SEPARATOR
                :
                    ""
                );
            onePre.ToLower();

            string otherPre = otherPrefix +
                (
                otherPrefix.Length > 0 && !otherPrefix.EndsWith(PROPERTY_TERM_SEPARATOR)
                ?
                    PROPERTY_TERM_SEPARATOR
                :
                    ""
                );
            otherPre.ToLower();

            foreach (string rawkey in one.Keys)
            {
                string key = removePrefix(onePre, rawkey);
                string otherkey = (otherPre + key).ToLower();
                string mergekey = (mPre + key).ToLower();
                if (!result.ContainsKey(mergekey))
                {
                    result.Add(mergekey, one[rawkey]);
                }
                if (other.ContainsKey(otherkey))
                {
                    otherKeys.Remove(otherkey);
                    dups.Add(mergekey, other[otherkey]);
                }
            }

            foreach (string rawkey in otherKeys)
            {
                string key = removePrefix(otherPre, rawkey);
                result.Add(mPre+key, other[rawkey]);
            }

            cross = dups.Count == 0 ? null : dups;
            return result;
        }

        private static string ToString(ArrayList set)
        {
            if(set == null)
            {
                return "null";
            }
            if(set.Count == 0)
            {
                return "[]";
            }
            string result = "[";
            foreach(object o in set)
            {
                result += o.ToString() + ",";
            }
            return result.Substring(0,result.Length - 1) + "]";
        }

        public static string ToString(StringDictionary properties)
        {
            if(properties == null)
            {
                return "null";
            }
            if (properties.Count == 0)
            {
                return "[]";
            }
            string result = "[\n";
            foreach(string s in properties.Keys)
            {
                result += string.Format("{0} : {1},\n", s, properties[s]);
            }
            return result.Substring(0,result.Length-2) + "\n]";
        }
    }
}