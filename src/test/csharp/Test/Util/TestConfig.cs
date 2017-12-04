using System;
using System.IO;
using System.Xml.Serialization;

namespace NMS.AMQP.Test.Util
{
    public class TestConfig
    {
        //public const string DEFAULT_BROKER_IP_ADDRESS = "192.168.133.8";
        public const string DEFAULT_BROKER_IP_ADDRESS = "192.168.2.69";
        public const string DEFAULT_BROKER_ADDRESS_SCHEME = "amqp://";
        public const string DEFAULT_BROKER_PORT = "5672";
        public const string DEFAULT_LOG_LEVEL = "warn";

        public string AddressScheme { get => config?.Broker?.Scheme ?? DEFAULT_BROKER_ADDRESS_SCHEME; }
        public string BrokerIpAddress { get => config?.Broker?.IPAddress ?? DEFAULT_BROKER_IP_ADDRESS; }
        public string BrokerPort { get => config?.Broker?.Port ?? DEFAULT_BROKER_PORT; }
        public string BrokerUsername { get => config?.Broker?.Client?.Username; }
        public string BrokerPassword { get => config?.Broker?.Client?.Password; }
        public string ClientId = null;
        
        protected Uri uri = null; 
        public Uri BrokerUri
        {
            get
            {
                if(uri == null)
                {
                    uri = new Uri(AddressScheme + BrokerIpAddress + ":" + BrokerPort);
                }
                return uri;
            }
        }

        public string LogLevel { get => config?.Global?.LogLevel ?? DEFAULT_LOG_LEVEL; } 
        public bool AmqpFrameTrace { get => config?.Global?.FrameTrace ?? false; }

        protected Configuration config = null;

        private TestConfig()
        {
            try
            {
                this.config = ObjectXMLSerializer<Configuration>.Load(Configuration.CONFIG_FILENAME);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load unit test configuration. Cause : {0}.", e.Message);
                Console.Error.WriteLine(e);
            }
        }

        private static TestConfig inst = null;
        public static TestConfig Instance {
            get
            {
                if(inst == null)
                {
                    inst = new TestConfig();
                }
                return inst;
            }
        }
    }

    #region XML Configuration
    [Serializable]
    [XmlRoot]
    public class Configuration
    {
        public const string CONFIG_FILENAME = "UnitTest.config";

        [XmlElement(Type = typeof(Broker))]
        public Broker Broker = null;

        [XmlElement(Type = typeof(Global))]
        public Global Global = null;

    }

    [Serializable]
    public class Global
    {
        [XmlAttribute]
        public string LogLevel = null;
        [XmlAttribute]
        public bool FrameTrace = false;
    }

    [Serializable]
    public class Broker
    {
        [XmlAttribute]
        public string Scheme = null;
        [XmlAttribute]
        public string IPAddress = null;
        [XmlAttribute]
        public string Port = null;

        [XmlElement(Type = typeof(Client))]
        public Client Client = null;

    }

    public class Client
    {
        [XmlAttribute]
        public string Username = null;
        [XmlAttribute]
        public string Password = null;
    }

    #endregion

    #region XML Object Serializer

    public static class ObjectXMLSerializer<T> where T : class
    {
        public static T Load(string path)
        {
            T serializableObject = LoadFromDocumentFormat(path);
            return serializableObject;
        }

        private static T LoadFromDocumentFormat (string path, Type[] extraTypes = null)
        {
            T SerializableObject = null;
            using(TextReader reader = CreateTextReader(path))
            {
                XmlSerializer serializer = CreateXmlSerializer(extraTypes);
                SerializableObject = serializer.Deserialize(reader) as T;
            }
            return SerializableObject;
        }

        private static TextReader CreateTextReader(string path)
        {
            return new StreamReader(path);
        }

        private static XmlSerializer CreateXmlSerializer(Type[] extraTypes)
        {
            Type objectType = typeof(T);
            XmlSerializer serializer = null;
            if(extraTypes != null)
            {
                serializer = new XmlSerializer(objectType, extraTypes);
            }
            else
            {
                serializer = new XmlSerializer(objectType);
            }
            return serializer;
        }
    }

    #endregion
}



