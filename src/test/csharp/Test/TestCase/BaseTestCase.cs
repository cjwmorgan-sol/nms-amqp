using System;
using System.Text;
using System.Collections.Specialized;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using NMS.AMQP.Test.Attribute;

namespace NMS.AMQP.Test.TestCase
{

    internal static class NMSPropertyConstants
    {
        public const string NMS_CONNECTION_ENCODING = "NMS.Message.Serialization";
        public const string NMS_CONNECTION_CLIENT_ID = "NMS.ClientId";
        public const string NMS_CONNECTION_USERNAME = "NMS.Username";
        public const string NMS_CONNECTION_PASSWORD = "NMS.Password";
    }

    #region NMSTestContainer Class

    /// <summary>
    /// NMSTestContainer Root class for all tests. Container NMS object Instances for testing and manages them.
    /// </summary>
    public abstract class NMSTestContainer
    {
        public static StringDictionary Clone(StringDictionary original)
        {
            StringDictionary clone = new StringDictionary();
            foreach (string key in original.Keys)
            {
                clone.Add(key.Clone() as string, original[key].Clone() as string);
            }
            return clone;
        }

        public static string ToString(IDictionary dictionary, int indt = 0)
        {
            if (dictionary == null) return "{}";
            StringBuilder sb = new StringBuilder();

            int indent = Math.Max(0, Math.Min(indt, 16));
            StringBuilder sbTabs = new StringBuilder();
            for (int i = 0; i < indent; i++)
            {
                sbTabs.Append('\t');
            }
            string wspace = sbTabs.ToString();

            sb.AppendFormat("[\n");
            foreach (object key in dictionary.Keys)
            {
                if (key != null)
                {
                    //Console.WriteLine("key: {0}, value: {1}", key, dictionary[key]);
                    
                    sb.AppendFormat("{0}\t[Key:{1}, Value: {2}]\n", wspace, key.ToString(), dictionary[key]?.ToString());
                }
            }
            sb.AppendFormat("{0}]", wspace);
            return sb.ToString();
        }

        public static string ToString(StringDictionary dictionary, int indt = 0)
        {
            if (dictionary == null) return "{}";
            StringBuilder sb = new StringBuilder();
            
            int indent = Math.Max(0, Math.Min(indt, 16));
            StringBuilder sbTabs = new StringBuilder();
            for (int i = 0; i < indent; i++)
            {
                sbTabs.Append('\t');
            }
            string wspace = sbTabs.ToString();
            
            sb.AppendFormat("{0}{", wspace);
            foreach (string key in dictionary.Keys)
            {
                sb.AppendFormat("{0}\t[Key:{1}, Value: {2}]", wspace, key, dictionary[key]);
            }
            sb.AppendFormat("{0}}", wspace);
            return sb.ToString();
        }

        protected StringDictionary properties = null;
        protected StringDictionary DefaultProperties = null;
        private NMSConnectionFactory providerFactory = null;

        private IList<IConnectionFactory> connectionFactories = new List<IConnectionFactory>();
        private IList<IConnection> connections = new List<IConnection>();
        private IList<ISession> sessions = new List<ISession>();
        private IList<IMessageProducer> producers = new List<IMessageProducer>();
        private IList<IMessageConsumer> consumers = new List<IMessageConsumer>();
        private IList<IDestination> destinations = new List<IDestination>();

        private IDictionary<string, int> connectionFactoryIndexMap = new Dictionary<string, int>();
        private IDictionary<string, int> connectionIndexMap = new Dictionary<string, int>();
        private IDictionary<string, int> sessionIndexMap = new Dictionary<string, int>();
        private IDictionary<string, int> producerIndexMap = new Dictionary<string, int>();
        private IDictionary<string, int> consumerIndexMap = new Dictionary<string, int>();
        private IDictionary<string, int> destinationIndexMap = new Dictionary<string, int>();
        private IDictionary<Type, IList> NMSInstanceTypeMap = new Dictionary<Type, IList>();
        private IDictionary<Type, IDictionary> NMSInstanceTypeIndexMap = new Dictionary<Type, IDictionary>();

        protected NMSTestContainer()
        {
            NMSInstanceTypeMap[typeof(IConnectionFactory)] = connectionFactories as List<IConnectionFactory> as IList;
            NMSInstanceTypeMap[typeof(IConnection)] = connections as List<IConnection> as IList;
            NMSInstanceTypeMap[typeof(ISession)] = sessions as List<ISession> as IList;
            NMSInstanceTypeMap[typeof(IMessageProducer)] = producers as List<IMessageProducer> as IList;
            NMSInstanceTypeMap[typeof(IMessageConsumer)] = consumers as List<IMessageConsumer> as IList;
            NMSInstanceTypeMap[typeof(IDestination)] = destinations as List<IDestination> as IList;

            NMSInstanceTypeIndexMap[typeof(IConnectionFactory)] = connectionFactoryIndexMap as Dictionary<string, int>;
            NMSInstanceTypeIndexMap[typeof(IConnection)] = connectionIndexMap as Dictionary<string, int>;
            NMSInstanceTypeIndexMap[typeof(ISession)] = sessionIndexMap as Dictionary<string, int>;
            NMSInstanceTypeIndexMap[typeof(IMessageProducer)] = producerIndexMap as Dictionary<string, int>;
            NMSInstanceTypeIndexMap[typeof(IMessageConsumer)] = consumerIndexMap as Dictionary<string, int>;
            NMSInstanceTypeIndexMap[typeof(IDestination)] = destinationIndexMap as Dictionary<string, int>;

            //try
            //{
            //    Console.WriteLine("TYPE MAP: {0}", ToString(NMSInstanceTypeMap as Dictionary<Type, IList>));
            //    Console.WriteLine("TYPE INDEX MAP: {0}", ToString(NMSInstanceTypeIndexMap as Dictionary<Type, IDictionary>));
            //}
            //catch (Exception e)
            //{
            //    Console.Error.WriteLine("Error: {0}", e.Message);
            //    Console.WriteLine(e);
            //}
        }

        public Uri BrokerURI
        {
            get { return TestConfig.Instance.BrokerUri; }
            //protected set { if (connectionFactory != null) { ConnectionFactory.BrokerUri = value; } }
        }
        internal StringDictionary ConnectionFactoryProperties
        {
            get { return Clone(properties); }
        }

        internal virtual void InitConnectedFactoryProperties(StringDictionary additionalProperties = null)
        {
            bool isDefault = this.properties == null;
            // add properties from the TestConfig first and use as Default Properties.
            properties = new StringDictionary();
            if (TestConfig.Instance.BrokerUsername != null)
            {
                ConnectionFactoryProperties[NMSPropertyConstants.NMS_CONNECTION_USERNAME] = TestConfig.Instance.BrokerUsername;
            }
            if (TestConfig.Instance.BrokerPassword != null)
            {
                ConnectionFactoryProperties[NMSPropertyConstants.NMS_CONNECTION_PASSWORD] = TestConfig.Instance.BrokerPassword;
            }
            if (TestConfig.Instance.ClientId != null)
            {
                ConnectionFactoryProperties[NMSPropertyConstants.NMS_CONNECTION_CLIENT_ID] = TestConfig.Instance.ClientId;
            }
            if (DefaultProperties == null || isDefault)
                DefaultProperties = Clone(properties);

            // add/overwrite Additional properies for unique purposes.
            if (additionalProperties != null)
            {
                foreach (string key in additionalProperties.Keys)
                {
                    properties.Add(key, additionalProperties[key]);
                }
            }

        }

        #region NMS Instance Create Methods
        internal IConnectionFactory CreateConnectionFactory()
        {
            if (providerFactory == null)
            {
                providerFactory = new NMS.AMQP.NMSConnectionFactory(BrokerURI, properties);
            }
            return providerFactory.ConnectionFactory;
        }

        internal IConnection CreateConnection(string nmsConnectionFactoryId)
        {
            return CreateConnection(GetConnectionFactory(nmsConnectionFactoryId));
        }

        internal IConnection CreateConnection(int index = 0)
        {
            return CreateConnection(GetConnectionFactory(index));
        }

        internal IConnection CreateConnection(IConnectionFactory factory)
        {
            return factory.CreateConnection();
        }

        internal ISession CreateSession(string nmsConnectionId)
        {
            return CreateSession(GetConnection(nmsConnectionId));
        }

        internal ISession CreateSession(int index = 0)
        {
            return CreateSession(GetConnection(index));
        }

        internal ISession CreateSession(IConnection connection)
        {
            return connection.CreateSession();
        }

        internal IMessageProducer CreateProducer(string nmsSessionId, string nmsDestinationId)
        {
            return CreateProducer(GetSession(nmsSessionId), GetDestination(nmsDestinationId));
        }


        internal IMessageProducer CreateProducer(int sessionIndex = 0, int destinationIndex = 0)
        {
            return CreateProducer(GetSession(sessionIndex), GetDestination(destinationIndex));
        }

        internal IMessageProducer CreateProducer(ISession session, IDestination destination)
        {
            return session.CreateProducer(destination);
        }

        internal IMessageConsumer CreateConsumer(string nmsSessionId, string nmsDestinationId)
        {
            return CreateConsumer(GetSession(nmsSessionId), GetDestination(nmsDestinationId));
        }


        internal IMessageConsumer CreateConsumer(int sessionIndex = 0, int destinationIndex = 0)
        {
            return CreateConsumer(GetSession(sessionIndex), GetDestination(destinationIndex));
        }

        internal IMessageConsumer CreateConsumer(ISession session, IDestination destination)
        {
            return session.CreateConsumer(destination);
        }

        internal ITopic CreateTopic(string name, string nmsId)
        {
            return CreateTopic(GetSession(nmsId), name);
        }

        internal ITopic CreateTopic(string name, int index = 0)
        {
            return CreateTopic(GetSession(index), name);
        }

        internal ITopic CreateTopic(ISession session, string name)
        {
            return session.GetTopic(name);
        }

        internal ITopic CreateTemporaryTopic(string nmsId)
        {
            return CreateTemporaryTopic(GetSession(nmsId));
        }

        internal ITopic CreateTemporaryTopic(int index = 0)
        {
            return CreateTemporaryTopic(GetSession(index));
        }

        internal ITemporaryTopic CreateTemporaryTopic(ISession session)
        {
            return session.CreateTemporaryTopic();
        }

        internal IQueue CreateQueue(string name, string nmsId)
        {
            return CreateQueue(GetSession(nmsId), name);
        }

        internal IQueue CreateQueue(string name, int index = 0)
        {
            return CreateQueue(GetSession(index), name);
        }

        internal IQueue CreateQueue(ISession session, string name)
        {
            return session.GetQueue(name);
        }

        internal IQueue CreateTemporaryQueue(string nmsId)
        {
            return CreateTemporaryQueue(GetSession(nmsId));
        }

        internal IQueue CreateTemporaryQueue(int index = 0)
        {
            return CreateTemporaryQueue(GetSession(index));
        }

        internal ITemporaryQueue CreateTemporaryQueue(ISession session)
        {
            return session.CreateTemporaryQueue();
        }


        #endregion

        #region NMS Instance Get Methods

        internal IConnectionFactory GetConnectionFactory(int index = 0)
        {
            return LookupNMSInstance(connectionFactories, index);
        }

        internal IConnectionFactory GetConnectionFactory(string nmsId)
        {
            int index = connectionFactoryIndexMap[nmsId];
            return LookupNMSInstance(connectionFactories, index);
        }

        internal IConnection GetConnection(int index = 0)
        {
            return LookupNMSInstance(connections, index);
        }

        internal IConnection GetConnection(string nmsId)
        {
            int index = connectionIndexMap[nmsId];
            return LookupNMSInstance(connections, index);
        }

        internal ISession GetSession(int index = 0)
        {
            return LookupNMSInstance(sessions, index);
        }

        internal ISession GetSession(string nmsId)
        {
            //Console.WriteLine("Trying to find Session {0}, in Index Map {1}.", nmsId, ToString(sessionIndexMap as Dictionary<String, int>));
            int index = sessionIndexMap[nmsId];
            return LookupNMSInstance(sessions, index);
        }

        internal IMessageProducer GetProducer(int index = 0)
        {
            return LookupNMSInstance(producers, index);
        }

        internal IMessageProducer GetProducer(string nmsId)
        {
            int index = producerIndexMap[nmsId];
            return LookupNMSInstance(producers, index);
        }

        internal IMessageConsumer GetConsumer(int index = 0)
        {
            return LookupNMSInstance(consumers, index);
        }
        internal IMessageConsumer GetConsumer(string nmsId)
        {
            int index = consumerIndexMap[nmsId];
            return LookupNMSInstance(consumers, index);
        }

        internal IDestination GetDestination(int index = 0)
        {
            return LookupNMSInstance(destinations, index);
        }

        internal IDestination GetDestination(string nmsId)
        {
            int index = destinationIndexMap[nmsId];
            return LookupNMSInstance(destinations, index);
        }


        private I LookupNMSInstance<I>(IList<I> list, int index = 0)
        {
            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid index {0}, for {1} Collection.", index, typeof(I).Name));
            }
            
            I nmsInstance = list[index];
            if (nmsInstance == null)
            {
                throw new ArgumentException(string.Format("Invalid index {0}, for {1} Collection. Return value is null.", index, typeof(I).Name));
            }
            return nmsInstance;
        }

        #endregion
        
        #region NMS Instance Exists Methods
        
        internal bool NMSInstanceExists<I>(int index)
        {
            IList NMSInstances = NMSInstanceTypeMap[typeof(I)];
            if (index < 0 || index >= NMSInstances.Count) return false;
            return NMSInstances[index] != null;
        }

        internal bool NMSInstanceExists<I>(string nmsId)
        {
            IDictionary NMSInstances = NMSInstanceTypeIndexMap[typeof(I)];
            if (NMSInstances.Contains(nmsId)) return false;
            int index = (int)NMSInstances[nmsId];
            return NMSInstanceExists<I>(index);
        }


        #endregion

        #region NMS Instance Add Methods

        internal void AddConnectionFactory(IConnectionFactory factory, string nmsId=null)
        {
            int index = AddNMSInstance(connectionFactories, factory);
            AddNMSInstanceIndexLookup(connectionFactoryIndexMap, nmsId, index);
        }

        internal void AddConnection(IConnection connection, string nmsId = null)
        {
            int index = AddNMSInstance(connections, connection);
            AddNMSInstanceIndexLookup(connectionIndexMap, nmsId, index);
        }

        internal void AddSession(ISession session, string nmsId = null)
        {
            int index = AddNMSInstance(sessions, session);
            AddNMSInstanceIndexLookup(sessionIndexMap, nmsId, index);
        }

        internal void AddProducer(IMessageProducer producer, string nmsId = null)
        {
            int index = AddNMSInstance(producers, producer);
            AddNMSInstanceIndexLookup(producerIndexMap, nmsId, index);
        }

        internal void AddConsumer(IMessageConsumer consumer, string nmsId = null)
        {
            int index = AddNMSInstance(consumers, consumer);
            AddNMSInstanceIndexLookup(consumerIndexMap, nmsId, index);
        }

        internal void AddDestination(IDestination destination, string nmsId = null)
        {
            int index = AddNMSInstance(destinations, destination);
            AddNMSInstanceIndexLookup(destinationIndexMap, nmsId, index);
        }


        private int AddNMSInstance<I>(IList<I> list, I instance)
        {
            if (instance == null)
            {
                return -1;
            }
            int previousIndex = list.IndexOf(instance);
            if (list.Contains(instance)) return previousIndex;
            list.Add(instance);
            return list.IndexOf(instance);
        }

        private void AddNMSInstanceIndexLookup(IDictionary<string,int>lookupTable, string nmsId, int index)
        {
            if (index != -1 && nmsId != null)
            {
                lookupTable.Add(nmsId, index);
            }
        }

        #endregion

        #region Object Override Methods

        public override string ToString()
        {
            string result = "" + this.GetType().Name + ": [\n";
            result += "\tConnection Factories: " + this.connectionFactories.Count + "\n";
            result += "\tConnections: " + this.connections.Count + "\n";
            result += "\tSessions: " + this.sessions.Count + "\n";
            result += "\tProducers: " + this.producers.Count + "\n";
            result += "\tConsumers: " + this.consumers.Count + "\n";
            result += "\tDestinations: " + this.destinations.Count + "\n";
            result += "\tConnection Factory Index Table: " + ToString(this.connectionFactoryIndexMap as IDictionary, 1) + "\n";
            result += "\tConnection Index Table: " + ToString(this.connectionIndexMap as IDictionary, 1) + "\n";
            result += "\tSession Index Table: " + ToString(this.sessionIndexMap as IDictionary, 1) + "\n";
            result += "\tConsumer Index Table: " + ToString(this.consumerIndexMap as IDictionary, 1) + "\n";
            result += "\tProducer Index Table: " + ToString(this.producerIndexMap as IDictionary, 1) + "\n";
            result += "\tDestination Index Table: " + ToString(this.destinationIndexMap as IDictionary, 1) + "\n";
            result += "]";
            return result;
        }

        #endregion

        #region NMS Instance Management Cleanup

        protected void CleanupInstances(bool dispose = false)
        {
            CloseInstances();
            ClearIndexes();
            ClearInstances(dispose);
        }

        private void CloseInstances()
        {
            if (this.producers != null)
            {
                foreach(IMessageProducer p in producers)
                {
                    p?.Close();
                }
            }

            if (this.consumers != null)
            {
                foreach(IMessageConsumer c in consumers)
                {
                    c?.Close();
                }
            }

            if (this.sessions != null)
            {
                foreach(ISession s in sessions)
                {
                    s?.Close();
                }
            }

            if (this.connections != null)
            {
                foreach(IConnection c in connections)
                {
                    c?.Close();
                }
            }
            
        }

        private void ClearIndexes()
        {
            foreach(IDictionary indexTable in NMSInstanceTypeIndexMap.Values)
            {
                indexTable.Clear();
            }
        }

        private void ClearInstances(bool dispose = false)
        {
            
            if (dispose && this.producers != null)
            {
                foreach (IMessageProducer p in producers)
                {
                    p?.Dispose();
                }
            }
            producers?.Clear();

            if (dispose && this.consumers != null)
            {
                foreach (IMessageConsumer c in consumers)
                {
                    c?.Dispose();
                }
            }
            consumers?.Clear();

            if (dispose && this.sessions != null)
            {
                foreach (ISession s in sessions)
                {
                    s?.Close();
                }
            }
            sessions?.Clear();

            if (dispose && this.connections != null)
            {
                foreach (IConnection c in connections)
                {
                    c?.Close();
                }
            }
            connections?.Clear();

            connectionFactories?.Clear();

            providerFactory = null;
        }

        #endregion

    }

    #endregion

    #region BaseTestCase Class

    public abstract class BaseTestCase : NMSTestContainer
    {
        
        internal static ITrace Logger = new NMSLogger(NMSLogger.ToLogLevel(TestConfig.Instance.LogLevel), TestConfig.Instance.AmqpFrameTrace);
        
        static BaseTestCase()
        {
            Tracer.Trace = Logger;
        }

        private static readonly TestSetupAttributeComparer TestSetupOrderComparer = new TestSetupAttributeComparer();
        private class TestSetupAttributeComparer : IComparer<TestSetupAttribute>
        {
            public int Compare(TestSetupAttribute x, TestSetupAttribute y)
            {
                return y.ComparableOrder - x.ComparableOrder;
            }
        }
        
        [SetUp]
        public virtual void Setup()
        {
            Logger.Info(string.Format("Setup TestCase {0} for test {1}.", this.GetType().Name, TestContext.CurrentContext.Test.MethodName));

            // Apply TestSetup Attribute in correct order
            TestContext.TestAdapter testAdapter = TestContext.CurrentContext.Test;
            MethodInfo methodInfo = GetType().GetMethod(testAdapter.MethodName);
            object[] attributes = methodInfo.GetCustomAttributes(true);

            // This set will order the TestSetup Attributes in the appropriate execution order for NMS Instance Initialization.
            // ie, should a test setup a connection and a session dependent that connection it ensure the connection setup attribute
            // execute its setup first.
            ISet<TestSetupAttribute> testSetupAttributes = new SortedSet<TestSetupAttribute>(TestSetupOrderComparer);
            foreach(System.Attribute attribute in attributes)
            {
                if(attribute is TestSetupAttribute)
                {
                    testSetupAttributes.Add(attribute as TestSetupAttribute);
                }
            }
            foreach(TestSetupAttribute tsa in testSetupAttributes)
            {
                tsa.Setup(this);
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            Logger.Info(string.Format("Tear Down TestCase {0} for test {1}.", this.GetType().Name, TestContext.CurrentContext.Test.MethodName));
            // restore properties for next test
            properties = Clone(DefaultProperties);
            CleanupInstances();
        }

        protected string GetMethodName()
        {
            StackTrace stackTrace = new StackTrace();
            return stackTrace.GetFrame(1).GetMethod().Name;
        }

        internal virtual void PrintTestFailureAndAssert(string methodDescription, string info, Exception ex)
        {
            if (ex is AssertionException || ex is IgnoreException)
            {
                throw ex;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("@@Test Failed in {0}", methodDescription);
            if (info != null)
            {
                sb.AppendFormat(", where info = {0}", info);
            }
            if (ex != null)
            {
                sb.AppendFormat(", {0}\n", GetTestException(ex));
            }
            
            Logger.Error(sb.ToString());
            Assert.Fail(sb.ToString());
        }

        protected virtual string GetTestException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Encountered an exception:\n\tMessage = {0}", ex.Message);
            sb.AppendFormat("\tType = {0}\n", ex.GetType());
            sb.AppendFormat("\tStack = {0}\n", ex.StackTrace);
            if(ex is NMSException)
            {
                if((ex as NMSException).ErrorCode != null)
                {
                    sb.AppendFormat("\tErrorCode = {0}\n", (ex as NMSException).ErrorCode);
                }
            }
            return sb.ToString();
            
        }

        internal void PrintTestException(Exception ex)
        {
            
            Logger.Error(GetTestException(ex));
        }
        

    }

    #endregion

}