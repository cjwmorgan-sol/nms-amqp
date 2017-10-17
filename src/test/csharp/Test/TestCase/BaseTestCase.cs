using System;
using System.Text;
using System.Collections.Specialized;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;
using System.Diagnostics;

namespace NMS.AMQP.Test.TestCase
{

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class |
                AttributeTargets.Interface | AttributeTargets.Assembly,
                AllowMultiple = true)]
    public class TestSetupAttribute : System.Attribute, ITestAction
    {
        public ActionTargets Targets => throw new NotImplementedException();

        public void AfterTest(ITest test)
        {
            throw new NotImplementedException();
        }

        public void BeforeTest(ITest test)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class BaseTestCase
    {
        
        public static ITrace Logger = new NMSLogger(NMSLogger.ToLogLevel(TestConfig.Instance.LogLevel), TestConfig.Instance.AmqpFrameTrace);
        public static StringDictionary Clone(StringDictionary original)
        {
            StringDictionary clone = new StringDictionary();
            foreach (string key in original.Keys)
            {
                clone.Add(key.Clone() as string, original[key].Clone() as string);
            }
            return clone;
        }

        static BaseTestCase()
        {
            Tracer.Trace = Logger;
        }

        private IConnectionFactory connectionFactory;
        private StringDictionary properties = null;
        private StringDictionary DefaultProperties = null;

        protected IConnectionFactory CreateConnectionFactory()
        {
            InitConnectedFactoryProperties();
            NMS.AMQP.NMSConnectionFactory providerFactory = new NMS.AMQP.NMSConnectionFactory(BrokerURI, properties);
            return providerFactory.ConnectionFactory;
        }
        
        protected virtual void InitConnectedFactoryProperties()
        {
            if (properties == null)
            {
                properties = new StringDictionary();
                if (TestConfig.Instance.BrokerUsername != null)
                {
                    ConnectionFactoryProperties["NMS.Username"] = TestConfig.Instance.BrokerUsername;
                }
                if (TestConfig.Instance.BrokerPassword != null)
                {
                    ConnectionFactoryProperties["NMS.Password"] = TestConfig.Instance.BrokerPassword;
                }
                if (TestConfig.Instance.ClientId != null)
                {
                    ConnectionFactoryProperties["NMS.ClientId"] = TestConfig.Instance.ClientId;
                }
                DefaultProperties = Clone(properties);
            }
        }

        protected IConnection CreateConnection()
        {
            return ConnectionFactory.CreateConnection();
        }

        protected StringDictionary ConnectionFactoryProperties
        {
            get { return properties; }
        }

        protected IConnectionFactory ConnectionFactory
        {
            get
            {
                if (connectionFactory == null)
                {
                    connectionFactory = CreateConnectionFactory();
                }
                return connectionFactory;
            }
        }

        public Uri BrokerURI
        {
            get { return TestConfig.Instance.BrokerUri; }
            protected set { if (connectionFactory != null) { ConnectionFactory.BrokerUri = value; } }
        }

        [SetUp]
        public virtual void Setup()
        {
            
        }

        [TearDown]
        public virtual void TearDown()
        {
            // restore properties for next test
            properties = Clone(DefaultProperties);
        }

        protected string GetMethodName()
        {
            StackTrace stackTrace = new StackTrace();
            return stackTrace.GetFrame(1).GetMethod().Name;
        }

        protected virtual void PrintTestFailureAndAssert(string methodDescription, string info, Exception ex)
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
            // TODO Add Error log
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

        protected void PrintTestException(Exception ex)
        {
            // TODO Add Test logging
            Logger.Error(GetTestException(ex));
        }
    }
}