using System;
using System.Collections.Specialized;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;
using NMS.AMQP.Test.Attribute;

namespace NMS.AMQP.Test.TestCase
{
    // initial ssl connection tests.
    [TestFixture]
    class SecureConnectionTest : BaseTestCase
    {
        protected IConnection Connection;
        public override void Setup()
        {
            if(!this.IsSecureBroker)
            {
                Assert.Ignore("Broker {0} not configured for secure connection.", TestConfig.Instance.BrokerUri.ToString());
            }
            base.Setup();

        }

        public override void TearDown()
        {
            base.TearDown();

        }

        [Test]
        public void TestConnectionFactorySSLConfiguration()
        {
            const string SecurePort = "5671";
            const string SecureScheme = "amqps://";

            StringDictionary props = new StringDictionary();

            try
            {

                //props[NMSPropertyConstants.NMS_CONNECTION_CLIENT_ID] = "foobarr";
                //props.Add("transport.SSLExcludeProtocols", "tls12,tls,tls11,ssl3");
                props.Add("transport.SSLProtocol", "");
                props.Add("transport.AcceptInvalidBrokerCert", bool.FalseString);
                props.Add("transport.ClientCertFileName", "client.crt");
                props.Add("transport.ServerName", "NMS test Req");
                this.InitConnectedFactoryProperties(props);
                IConnectionFactory connectionFactory = CreateConnectionFactory();
                string brokerConnectionString = SecureScheme + TestConfig.Instance.BrokerIpAddress + ":" + SecurePort;
                connectionFactory.BrokerUri = new Uri(brokerConnectionString);

                ConnectionFactory providerFactory = connectionFactory as ConnectionFactory;

                providerFactory.TransportProperties["transport.SSLExcludeProtocols"] = "ssl3";

                //providerFactory.CertificateValidationCallback = (a,b,c,d)=>true;
                //providerFactory.LocalCertificateSelect = (a,b,c,d,e) => null;
                /*providerFactory.FactoryProperties[NMS.AMQP.Property.ConnectionFactoryPropertyConstants.SSL_EXCLUDED_PROTOCOLS] = "";
                providerFactory.FactoryProperties[NMS.AMQP.Property.ConnectionFactoryPropertyConstants.SSL_CLIENT_CERTIFICATE_FILE] = "client.crt";
                providerFactory.FactoryProperties[NMS.AMQP.Property.ConnectionFactoryPropertyConstants.SSL_VALIDATE_CERTIFICATE] = bool.TrueString;
                */
                Assert.IsTrue(providerFactory.IsSSL, "Failed to Configure Connection Factory for SSL.");

                IConnection connection = connectionFactory.CreateConnection();
                connection.Start();
                connection.Close();
            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
        }
    }
}