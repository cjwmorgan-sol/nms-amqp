using System;
using System.Collections.Specialized;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;

namespace NMS.AMQP.Test.TestCase
{
    [TestFixture]
    class ConnectionTest : BaseTestCase
    {
        protected IConnection Connection;
        public override void Setup()
        {
            base.Setup();
            Connection = this.CreateConnection();
        }

        public override void TearDown()
        {
            base.TearDown();
            if(Connection != null)
            {
                Connection.Close();
            }
        }

        [Test]
        public void TestStart()
        {
            Connection.Start();
        }

        [Test]
        public void TestSetClientIdFromConnectionFactory()
        {
            ConnectionFactoryProperties["NMS.clientid"] = "foobarr";
            IConnectionFactory connectionFactory = CreateConnectionFactory();
            IConnection connection = connectionFactory.CreateConnection();
            try
            {
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
        public void TestSetClientIdFromConnection()
        {
            
            try
            {
                Connection.ClientId = "barfoo";
                Connection.Start();
            }
            catch (NMSException e)
            {
                PrintTestFailureAndAssert(GetMethodName(), "Unexpected NMSException", e);
            }
            
        }

        [Test]
        public void TestSetClientIdAfterStart()
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