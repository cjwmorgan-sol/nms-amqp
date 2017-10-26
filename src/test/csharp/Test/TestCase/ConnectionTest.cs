using System;
using System.Collections.Specialized;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;
using NMS.AMQP.Test.Attribute;

namespace NMS.AMQP.Test.TestCase
{
    [TestFixture]
    class ConnectionTest : BaseTestCase
    {
        protected IConnection Connection;
        public override void Setup()
        {
            base.Setup();
            
        }

        public override void TearDown()
        {
            base.TearDown();
            
        }

        [Test]
        [ConnectionSetup]
        public void TestStart()
        {
            using (Connection = GetConnection())
            {
                Connection.Start();
            }   
        }

        [Test]
        public void TestSetClientIdFromConnectionFactory()
        {
            
            StringDictionary props = new StringDictionary();
            props[NMSPropertyConstants.NMS_CONNECTION_CLIENT_ID] = "foobarr";
            this.InitConnectedFactoryProperties(props);
            IConnectionFactory connectionFactory = CreateConnectionFactory();
            
            IConnection connection = connectionFactory.CreateConnection();
            
            try
            {
                Assert.AreEqual("foobarr", connection.ClientId, "ClientId was not set by Connection Factory.");
                connection.ClientId = "barfoo";
                Assert.Fail("Expect Invalid ClientId Exception");
            }
            catch (InvalidClientIDException e)
            {
                Assert.NotNull(e);
                
                // success
            }
            finally
            {
                connection.Close();
            }
            
        }

        [Test]
        [ConnectionSetup]
        public void TestSetClientIdFromConnection()
        {
            using (Connection = GetConnection())
            {
                try
                {
                    Connection.ClientId = "barfoo";
                    Assert.AreEqual("barfoo", Connection.ClientId, "ClientId was not set.");
                    Connection.Start();
                }
                catch (NMSException e)
                {
                    PrintTestFailureAndAssert(GetMethodName(), "Unexpected NMSException", e);
                }

            }
        }

        [Test]
        [ConnectionSetup]
        public void TestSetClientIdAfterStart()
        {
            using (Connection = GetConnection())
            {
                try
                {
                    Connection.ClientId = "barfoo";
                    Connection.Start();
                    Connection.ClientId = "foobar";
                    Assert.Fail("Expected Invalid Operation Exception.");
                }
                catch (NMSException e)
                {
                    Assert.IsTrue((e is InvalidClientIDException), "Expected InvalidClientIDException Got : {0}", e.GetType());
                    // success
                }
            }
        }
    }
}