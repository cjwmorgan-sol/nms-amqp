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
                    if(props[key].SetMethod!=null)
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
        /// <param name="cross">Holds all the properties from the "other" StringDictionary that are not used because one has the properties.</param>
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

        public static string ToString(IList set)
        {
            if (set == null)
            {
                return "null";
            }
            if (set.Count == 0)
            {
                return "[]";
            }
            string result = "[";
            foreach (object o in set)
            {
                result += o.ToString() + ",";
            }
            return result.Substring(0, result.Length - 1) + "]";
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

        public static IDictionary<K,V> Clone<K,V>(IDictionary<K,V> dict)
        {
            if (dict == null) return null;
            Dictionary<K, V> clone = new Dictionary<K, V>(dict.Count);
            dict.CopyTo(clone.ToArray(), 0);
            return clone;
        }
    }


    #region NMS Resource Property Interceptor classes StringDictionary based

    #region abstract Property Interceptor classes

    internal abstract class PropertyInterceptor<T> : StringDictionary where T : class
    {
        protected delegate void ApplyProperty(T instance, string key, string value);
        protected delegate string GetProperty(T instance, string key);

        protected struct Interceptor
        {
            public ApplyProperty Setter;
            public GetProperty Getter;
        }


        private readonly StringDictionary properties;
        private readonly IDictionary<string, Interceptor> interceptors;
        private readonly T instance;
        protected PropertyInterceptor(T instance, StringDictionary properties, IDictionary<string, Interceptor> interceptors) : base()
        {
            this.properties = properties;
            this.interceptors = interceptors;
            this.instance = instance;
        }

        protected T Instance { get { return instance; } }

        protected StringDictionary Properties { get { return properties; } }

        protected void AddInterceptor(string key, Interceptor interceptor)
        {
            this.interceptors.Add(key, interceptor);
            if (this.properties.ContainsKey(key))
            {
                SetProperty(key, properties[key]);
            }
            else
            {
                AddProperty(key, properties[key]);
            }
        }

        protected void AddProperty(string key, string value)
        {
            if (interceptors.ContainsKey(key))
            {
                interceptors[key].Setter(instance, key, value);
            }
            else
            {
                properties.Add(key, value);
            }
        }

        protected void SetProperty(string key, string value)
        {
            if (interceptors.ContainsKey(key))
            {
                interceptors[key].Setter(instance, key, value);
            }
            else
            {
                properties[key] = value;
            }
        }

        protected string GetValue(string key)
        {
            string value = null;
            if (interceptors.ContainsKey(key))
            {
                value = interceptors[key].Getter(instance, key);
            }
            else
            {
                value = properties[key];
            }
            return value;
        }

        #region IDictionary<> Methods

        public override void Add(string key, string value)
        {
            AddProperty(key, value);
        }

        public override string this[string key]
        {
            get
            {
                return GetValue(key);
            }
            set
            {
                SetProperty(key, value);
            }
        }

        #endregion

    }

    internal abstract class NMSResourcePropertyInterceptor<T, I> : PropertyInterceptor<T> where I : ResourceInfo where T : NMSResource<I>
    {
        protected NMSResourcePropertyInterceptor(T instance, StringDictionary properties, IDictionary<string, Interceptor> interceptors) : base(instance, properties, interceptors)
        {

        }
    }

    #endregion

    #region Connnection Property Interceptor Class

    internal class ConnectionPropertyInterceptor : NMSResourcePropertyInterceptor<Connection, ConnectionInfo>
    {
        // TODO add more properties.
        protected static Dictionary<string, Interceptor> connInterceptors = new Dictionary<string, Interceptor>()
        {
            {
                Connection.REQUEST_TIMEOUT_PROP,
                new Interceptor
                {
                    Setter = (c, key, value)=>
                    {
                        c.RequestTimeout = TimeSpan.FromMilliseconds(Convert.ToInt64(value));
                    },
                    Getter = (c, key) => { return Convert.ToInt64(c.RequestTimeout.TotalMilliseconds).ToString(); }
                }
            },
        };

        public ConnectionPropertyInterceptor(Connection connection, StringDictionary properties) : base(connection, properties, connInterceptors)
        {

        }
    }

    #endregion

    #region Session Property Interceptor Class
    //TODO Implement
    #endregion

    #region Producer Property Interceptor Class
    //TODO Implement
    #endregion

    #region Consumer Property Interceptor Class
    //TODO Implement
    #endregion

    #endregion

    #region NMS object Property Interceptor classes IPrimitiveMap based

    #region Abstract Property Interceptor Class
    internal abstract class NMSPropertyInterceptor<T> : Types.Map.PrimitiveMapBase, IPrimitiveMap
    {

        #region Generic delegates and Interceptor struct

        protected delegate void ApplyProperty(T instance, object value);
        protected delegate object GetProperty(T instance);
        protected delegate void ClearProperty(T instance);
        protected delegate bool CheckProperty(T instance);

        // The Interceptor struct is a container of operation delegates 
        // to be used on a specific property of instance T.
        protected struct Interceptor
        {
            public ApplyProperty Setter;
            public GetProperty Getter;
            public ClearProperty Reset;
            public CheckProperty Exists;
        }

        #endregion

        private readonly object SyncLock;
        private readonly IPrimitiveMap properties;
        private readonly IDictionary<string, Interceptor> interceptors;
        private readonly T instance;
        protected NMSPropertyInterceptor(T instance, IPrimitiveMap properties, IDictionary<string, Interceptor> interceptors) : base()
        {
            this.properties = properties ?? new Apache.NMS.Util.PrimitiveMap();
            
            this.instance = instance;
            if (this.properties is Types.Map.PrimitiveMapBase)
            {
                SyncLock = (this.properties as Types.Map.PrimitiveMapBase).SyncRoot;
            }
            else
            {
                SyncLock = new object();
            }

            this.interceptors = new Dictionary<string, Interceptor>();
            foreach(string key in interceptors.Keys)
            {
                AddInterceptor(key, interceptors[key]);
            }
        }

        #region Property Interceptor Methods

        protected T Instance { get { return instance; } }

        protected void AddInterceptor(string key, Interceptor interceptor)
        {
            bool updated = false;
            if(!interceptors.ContainsKey(key))
                this.interceptors.Add(key, interceptor);
            else
            {
                // this allows subs classes to override base classes.
                this.interceptors[key] = interceptor;
                updated = true;
            }
            if (properties.Contains(key) || updated)
            {
                SetProperty(key, properties[key]);
                properties.Remove(key);
            }
                
        }

        protected void SetProperty(string key, object value)
        {
            if (interceptors.ContainsKey(key))
            {
                interceptors[key].Setter(instance, value);
            }
            else
            {
                properties[key] = value;
            }
        }

        protected object GetValue(string key)
        {
            object value = null;
            if (interceptors.ContainsKey(key))
            {
                if(interceptors[key].Exists(instance))
                    value = interceptors[key].Getter(instance);
            }
            else
            {
                value = properties[key];
            }
            return value;
        }

        #endregion

        #region PrimitiveMapBase abstract override methods

        internal override object SyncRoot { get { return SyncLock; } }

        public override bool Contains(object key)
        {
            bool found = properties.Contains(key);
            if (!found && interceptors.ContainsKey(key.ToString()))
            {
                string keystring = key.ToString();
                found = interceptors[keystring].Exists(instance);
            }
            return found;
        }

        public override void Clear()
        {
            properties.Clear();
            foreach(string key in interceptors.Keys)
            {
                interceptors[key].Reset(instance);
            }
        }

        protected int InterceptorCount
        {
            get
            {
                int count = 0;
                foreach(string key in interceptors.Keys)
                {
                    Interceptor i = interceptors[key];
                    if(i.Exists(instance))
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public override int Count => this.properties.Count + InterceptorCount;

        public override void Remove(object key)
        {
            if(properties.Contains(key))
                properties.Remove(key);
            else if(interceptors.ContainsKey(key.ToString()))
            {
                interceptors[key.ToString()].Reset(instance);
            }
        }

        public override ICollection Keys
        {
            get
            {
                ISet<string> keys = new HashSet<string>();
                foreach (string key in interceptors.Keys)
                {
                    Interceptor i = interceptors[key];
                    if (i.Exists(instance))
                    {
                        keys.Add(key);
                    }
                }
                foreach(string key in properties.Keys)
                {
                    keys.Add(key);
                }
                
                return keys.ToList();
            }
        }

        public override ICollection Values
        {
            get
            {
                ISet<object> values = new HashSet<object>();
                foreach (string key in interceptors.Keys)
                {
                    Interceptor i = interceptors[key];
                    if (i.Exists(instance))
                    {
                        values.Add(i.Getter(instance));
                    }
                }
                foreach (object value in properties.Values)
                {
                    values.Add(value);
                }

                return values.ToList();
            }
        }

        protected override void SetObjectProperty(string key, object value)
        {
            SetProperty(key, value);
        }

        protected override object GetObjectProperty(string key)
        {
            return GetValue(key);
        }

        #endregion

    }
    #endregion

    #region Message Property Interceptor Class
    internal class NMSMessagePropertyInterceptor : NMSPropertyInterceptor<Message.Message>
    {
        protected static readonly Dictionary<string, Interceptor> messageInterceptors = new Dictionary<string, Interceptor>()
        {
            {
                Message.Message.MESSAGE_VENDOR_ACK_PROP,
                new Interceptor
                {
                    Setter = (m, value) => 
                    {
                        if(m.GetMessageCloak().AckHandler == null && m.GetMessageCloak().IsReceived)
                        {
                            throw new NMSException("Session Acknowledgement mode does not allow setting " + Message.Message.MESSAGE_VENDOR_ACK_PROP);
                        }
                        int ackType = -1;
                        try
                        {
                            ackType = Types.ConversionSupport.ConvertNMSType<int>(value);
                        }
                        catch (Types.ConversionSupport.NMSTypeConversionException ce)
                        {
                            throw ExceptionSupport.Wrap(ce, "Property {0} cannot be set from a {1}", Message.Message.MESSAGE_VENDOR_ACK_PROP, value?.GetType());
                        }
                        if (ackType != -1)
                            m.GetMessageCloak().AckHandler.AcknowledgementType = (Message.AckType)ackType;

                    },
                    Getter = (m) => 
                    {
                        object acktype = null;
                        if(m.GetMessageCloak().AckHandler != null)
                        {
                            acktype = m.GetMessageCloak().AckHandler.AcknowledgementType;
                        }
                        return acktype;
                    },
                    Exists = (m) =>
                    {
                        Message.Cloak.IMessageCloak cloak = m.GetMessageCloak();
                        if (cloak.AckHandler != null)
                        {
                            return cloak.AckHandler.IsAckTypeSet;
                        }
                        return false;
                    },
                    Reset = (m) =>
                    {
                        Message.Cloak.IMessageCloak cloak = m.GetMessageCloak();
                        if (cloak.AckHandler != null)
                        {
                            cloak.AckHandler.ClearAckType();
                        }
                    }

                }
            },
        };


        public NMSMessagePropertyInterceptor(Message.Message instance, IPrimitiveMap properties) : base (instance, properties, messageInterceptors)
        {

        }
        
    }//*/

    #endregion

    #endregion

}