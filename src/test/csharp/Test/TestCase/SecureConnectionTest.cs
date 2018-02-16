using System;
using System.Collections.Specialized;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;
using NMS.AMQP.Test.Attribute;
using System.Security.Cryptography.X509Certificates;

namespace NMS.AMQP.Test.TestCase
{
    // initial ssl connection tests.
    [TestFixture]
    class SecureConnectionTest : BaseTestCase
    {
        protected const string TestSuiteClientCertificateFileName = "client.crt";
        protected const string ApplicationCallbackExceptionMessage = "Bad Application";

        protected IConnection Connection;
        protected readonly static TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(60000);
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
            Connection?.Close();
            Connection = null;
        }


        #region Useful Function
        private static string GetSecureProdiverURIString()
        {
            if(TestConfig.Instance.IsSecureBroker)
            {
                return TestConfig.Instance.BrokerUri.ToString();
            }
            else
            {
                const string SecurePort = "5671";
                const string SecureScheme = "amqps://";
                string brokerIp = TestConfig.Instance.BrokerIpAddress;
                string providerURI = string.Format("{0}{1}:{2}", SecureScheme, brokerIp, SecurePort);
                return providerURI;
            }
        }


        private IConnection CreateSecureConnection(IConnectionFactory connectionFactory)
        {
            IConnection connection = null;
            if (TestConfig.Instance.BrokerUsername != null)
            {
                connection = connectionFactory.CreateConnection(TestConfig.Instance.BrokerUsername, TestConfig.Instance.BrokerPassword);
            }
            else
            {
                connection = connectionFactory.CreateConnection();
            }

            connection.RequestTimeout = ConnectTimeout;

            return connection;
        }

        private void TestSecureConnect(out Exception failure)
        {
            TestSecureConnect(Connection, out failure);
        }

        private static void TestSecureConnect(IConnection connection, out Exception failure)
        {
            failure = null;
            try
            {
                connection.Start();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        }

        #endregion

        /*
         * Tests how to configure ssl options for a connection factory, via URI and properties. 
         */
        [Ignore("Unfinished Test")]
        [Test]
        public void TestConnectionFactorySSLConfiguration()
        {
            StringDictionary props = new StringDictionary();

            try
            {

                //props[NMSPropertyConstants.NMS_CONNECTION_CLIENT_ID] = "foobarr";
                props.Add("transport.SSLProtocol", "");
                props.Add("transport.AcceptInvalidBrokerCert", bool.FalseString);
                props.Add("transport.ClientCertFileName", TestSuiteClientCertificateFileName);
                props.Add("transport.ServerName", "NMS test Req");
                this.InitConnectedFactoryProperties(props);
                IConnectionFactory connectionFactory = CreateConnectionFactory();
                string brokerConnectionString = GetSecureProdiverURIString();
                connectionFactory.BrokerUri = new Uri(brokerConnectionString);

                ConnectionFactory providerFactory = connectionFactory as ConnectionFactory;
                
                //providerFactory.CertificateValidationCallback = (a,b,c,d)=>true;
                //providerFactory.LocalCertificateSelect = (a,b,c,d,e) => null;
                /*providerFactory.FactoryProperties[NMS.AMQP.Property.ConnectionFactoryPropertyConstants.SSL_EXCLUDED_PROTOCOLS] = "";
                providerFactory.FactoryProperties[NMS.AMQP.Property.ConnectionFactoryPropertyConstants.SSL_CLIENT_CERTIFICATE_FILE] = "client.crt";
                providerFactory.FactoryProperties[NMS.AMQP.Property.ConnectionFactoryPropertyConstants.SSL_VALIDATE_CERTIFICATE] = bool.TrueString;
                */
                Assert.IsTrue(providerFactory.IsSSL, "Failed to Configure Connection Factory for SSL.");
                
            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
        }

        /*
         * Tests valid values for the transport.SSLProtocol property.
         * The test then attempts to connect to the broker with the assigned property creating should it not be able to connect.
         * The protocols Ssl3, Ssl2 and tls1.0 are not secured so all test values should result in the tsl11, or later, protocol to 
         * be selected should a broker support the secure protocols.
         * */
        [Test]
        public void TestValidSSLProtocols(
            [Values( "tls11", "tls12", "Ssl3,tls11", "tls,tls11", "Default,tls11", "tls,ssl3,tls11", "  tls12,tls", "tls,tls11,tls12")]
            string protocolsString
            )
        {
            StringDictionary queryOptions = new StringDictionary()
            {
                { NMSPropertyConstants.NMS_SECURE_TANSPORT_SSL_PROTOCOLS, protocolsString }
            };

            string normalizedName;
            System.Security.Authentication.SslProtocols protocol = System.Security.Authentication.SslProtocols.None;
            if (Enum.TryParse(protocolsString, true, out protocol))
            {
                normalizedName = protocol.ToString();
            }
            else
            {
                normalizedName = protocolsString;
            }


            try
            {
                // create provider URI
                string providerUriQueryParameters = Apache.NMS.Util.URISupport.CreateQueryString(queryOptions);
                string providerUriBase = GetSecureProdiverURIString();
                string providerUri = string.Format("{0}?{1}", providerUriBase, providerUriQueryParameters);

                IConnectionFactory connectionFactory = CreateConnectionFactory();
                connectionFactory.BrokerUri = new Uri(providerUri);
                ConnectionFactory providerConnectionFactory = connectionFactory as ConnectionFactory;
                // disables certificate validation
                providerConnectionFactory.CertificateValidationCallback = (a, b, c, d) => true;
                string transportSSLProtocol = providerConnectionFactory.TransportProperties[NMSPropertyConstants.NMS_SECURE_TANSPORT_SSL_PROTOCOLS];

                Assert.AreEqual(normalizedName, transportSSLProtocol);

                Connection = CreateSecureConnection(connectionFactory);
                
                try
                {
                    // attempt to connect to broker
                    Connection.Start();
                }
                catch (NMSSecurityException secEx)
                {
                    Logger.Warn(string.Format("Security failure. Check {0} file for test configuration. Or check broker configuration. Security Message : {1}", Configuration.CONFIG_FILENAME, secEx.Message));
                }
            }
            catch(Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
            finally
            {
                Connection?.Close();
            }
            
        }


        [Test]
        public void TestProviderRemoteCertificateValidationCallback()
        {
            StringDictionary queryOptions = new StringDictionary()
            {
                { NMSPropertyConstants.NMS_SECURE_TRANSPORT_ACCEPT_INVALID_BROKER_CERT, bool.TrueString }
            };
            try
            {
                // Create Provider URI
                string providerUriQueryParameters = Apache.NMS.Util.URISupport.CreateQueryString(queryOptions);
                string providerUriBase = GetSecureProdiverURIString();
                string providerUri = string.Format("{0}?{1}", providerUriBase, providerUriQueryParameters);

                // Create Provider connection factory
                IConnectionFactory connectionFactory = CreateConnectionFactory();
                connectionFactory.BrokerUri = new Uri(providerUri);
                ConnectionFactory providerConnectionFactory = connectionFactory as ConnectionFactory;

                bool signaled = false;

                // (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                // test Validation Callback can override NMS_SECURE_TRANSPORT_ACCEPT_INVALID_BROKER_CERT property
                providerConnectionFactory.CertificateValidationCallback = (sender, cert, chain, errors) =>
                {
                    signaled = true;
                    // indicate that the remote certificate is invalid.
                    return false;
                };

                Exception connectionFailure = null;

                // Create connection using NMS Interface
                using (Connection = CreateSecureConnection(connectionFactory))
                {
                    TestSecureConnect(Connection, out connectionFailure);

                    Assert.IsTrue(signaled, "Application callback was not executed.");

                    Assert.NotNull(connectionFailure, "Application callback CertificateValidationCallback should cause Connection start to fail. Expected exception.");

                    Logger.Info(string.Format("Caught exception on invalid start of connection. Connection failure cause : {0} ", connectionFailure));
                }

                Connection = null;
                connectionFailure = null;
                signaled = false;

                //update connection factory transport settings

                providerConnectionFactory.TransportProperties[NMSPropertyConstants.NMS_SECURE_TRANSPORT_ACCEPT_INVALID_BROKER_CERT] = bool.FalseString;

                // Test NMS_SECURE_TRANSPORT_ACCEPT_INVALID_BROKER_CERT is ignored when the application callback throws exception.
                providerConnectionFactory.CertificateValidationCallback = (sender, cert, chain, errors) =>
                {
                    signaled = true;
                    // throw dummy exception to indicate an application callback failure.
                    throw new Exception(ApplicationCallbackExceptionMessage);
                };

                // Create connection using NMS Interface
                using (Connection = CreateSecureConnection(connectionFactory))
                {
                    TestSecureConnect(Connection, out connectionFailure);

                    Assert.IsTrue(signaled, "Application Callback thats throws exception was not executed.");

                    Assert.NotNull(connectionFailure, "Connection did not produce an exception from the application callback.");

                    Assert.AreEqual(ApplicationCallbackExceptionMessage, connectionFailure.InnerException?.Message, "Exception produced was not application callback exception.");
                }
            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
        }

        /*
         * Tests that an application can load an X509Certificate and use that certificate in the callback.
         * This test can fail different base on the TestSuite configuration. Should the testSuite be configured 
         * to use a client certificate this test assumes that the remote broker can accept the client certificate 
         * and fail appropriately. Should the test suite not be configured to use a client certificate the test 
         * will not fail on a connection failure.
         */
        [Test]
        public void TestProviderLocalCertificateSelectCallback()
        {
            bool isConfiguredCert = !String.IsNullOrWhiteSpace(TestConfig.Instance.ClientCertFileName);
            string clientCertFileName = isConfiguredCert ? TestConfig.Instance.ClientCertFileName : TestSuiteClientCertificateFileName;
            // configure to ignore remote certificate validation.
            StringDictionary queryOptions = new StringDictionary()
            {
                { NMSPropertyConstants.NMS_SECURE_TRANSPORT_ACCEPT_INVALID_BROKER_CERT, bool.TrueString }
            };
            try
            {
                // load certificate
                X509Certificate cert = null;
                try
                {
                     cert = X509Certificate2.CreateFromCertFile(clientCertFileName);
                }
                catch (Exception ex)
                {
                    // failure to load should fail test.
                    Assert.Fail("Could not load client certificate for from file {0}, cause of failure : {2}", clientCertFileName, ex.Message);
                }

                Assert.NotNull(cert, "Failed to load client certificate {0}.", clientCertFileName);

                // Create Provider URI
                string providerUriQueryParameters = Apache.NMS.Util.URISupport.CreateQueryString(queryOptions);
                string providerUriBase = GetSecureProdiverURIString();
                string providerUri = string.Format("{0}?{1}", providerUriBase, providerUriQueryParameters);

                // Create Provider connection factory
                IConnectionFactory connectionFactory = CreateConnectionFactory();
                connectionFactory.BrokerUri = new Uri(providerUri);
                ConnectionFactory providerConnectionFactory = connectionFactory as ConnectionFactory;

                bool signaled = false;
                Exception connectionFailure = null;

                // Test selecting certificate from loaded by application

                // X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
                providerConnectionFactory.LocalCertificateSelect = (sender, targetHost, localCertificates, remoteCertificate, issuers) =>
                {
                    signaled = true;
                    // return loaded certificate
                    return cert;
                };

                using (Connection = CreateSecureConnection(connectionFactory))
                {

                    TestSecureConnect(Connection, out connectionFailure);

                    string failureCause = null;
                    // log any failure for reference
                    if (connectionFailure != null)
                    {
                        failureCause = string.Format("Connection failed to connection. Cause : {0}", connectionFailure.Message);
                        Logger.Info("Caught exception for secure connection " + connectionFailure);
                    }

                    Assert.IsTrue(signaled, "LocalCertificateSelectCallback failed to execute. {0}", failureCause ?? "");
                    
                    // Should the runner of the test suite indent to use a configured client certificate
                    // the test should fail if the connection fails to connection.
                    // This means the client certificate selected must be accepted by the remote broker.
                    if (isConfiguredCert)
                    {
                        Assert.IsNull(connectionFailure, "Caught Exception for configured client certificate." +
                            " Please check TestSuite Configuration in {0} and" +
                            " ensure host {1} can process client certificate {2}." +
                            " Failure Message : {3}",
                            Configuration.CONFIG_FILENAME, 
                            TestConfig.Instance.BrokerIpAddress, 
                            clientCertFileName,
                            connectionFailure?.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
        }

        /*
         * Tests the behaviour of the application LocalCertificateSelectCallback callback throw an exception.
         */
        [Test]
        public void TestProviderLocalCertificateSelectCallbackThrowsException()
        {
            // configure to ignore remote certificate validation.
            StringDictionary queryOptions = new StringDictionary()
            {
                { NMSPropertyConstants.NMS_SECURE_TRANSPORT_ACCEPT_INVALID_BROKER_CERT, bool.TrueString }
            };
            try
            {
                // Create Provider URI
                string providerUriQueryParameters = Apache.NMS.Util.URISupport.CreateQueryString(queryOptions);
                string providerUriBase = GetSecureProdiverURIString();
                string providerUri = string.Format("{0}?{1}", providerUriBase, providerUriQueryParameters);

                // Create Provider connection factory
                IConnectionFactory connectionFactory = CreateConnectionFactory();
                connectionFactory.BrokerUri = new Uri(providerUri);
                ConnectionFactory providerConnectionFactory = connectionFactory as ConnectionFactory;

                bool signaled = false;
                Exception connectionFailure = null;

                providerConnectionFactory.LocalCertificateSelect = (sender, targetHost, localCertificates, remoteCertificate, issuers) =>
                {
                    signaled = true;
                    // throw exception
                    throw new Exception(ApplicationCallbackExceptionMessage);
                };

                using (Connection = CreateSecureConnection(connectionFactory))
                {
                    TestSecureConnect(Connection, out connectionFailure);

                    string failureCause = null;
                    // log any failure for reference
                    if (connectionFailure != null)
                    {
                        failureCause = string.Format("Connection failed to connection. Cause : {0}", connectionFailure.Message);
                        Logger.Info("Caught exception for secure connection " + connectionFailure);
                    }

                    Assert.IsTrue(signaled, "LocalCertificateSelectCallback failed to execute. {0}", failureCause ?? "");

                    Assert.NotNull(connectionFailure, "Connection did not produce exception from application callback.");

                    Assert.AreEqual(ApplicationCallbackExceptionMessage, connectionFailure.InnerException?.Message, "Exception produced was not application callback exception.");
                }
            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
        }

        /*
         * Tests that the property for client certificate file name loads the certificate into the
         * certificate collection for selection.
         */
        [Test]
        public void TestClientCertificateFileName()
        {
            string clientCertificateFileName = TestSuiteClientCertificateFileName;
            // Configure to ignore remote certificate validation and add TestSuiteClientCertificateFileName for selction.
            StringDictionary queryOptions = new StringDictionary()
            {
                { NMSPropertyConstants.NMS_SECURE_TRANSPORT_ACCEPT_INVALID_BROKER_CERT, bool.TrueString },
                { NMSPropertyConstants.NMS_SECURE_TRANSPORT_CLIENT_CERT_FILE_NAME, clientCertificateFileName }
            };

            try
            {
                // load certificate
                X509Certificate cert = null;
                try
                {
                    cert = X509Certificate2.CreateFromCertFile(clientCertificateFileName);
                }
                catch (Exception ex)
                {
                    // failure to load should fail test.
                    Assert.Fail(
                        "Could not load client certificate for from file {0}, cause of failure : {2}", 
                        clientCertificateFileName, 
                        ex.Message
                        );
                }
                Assert.NotNull(cert, "Failed to load client certificate. {0}", clientCertificateFileName);

                byte[] clientCertBytes = cert.GetRawCertData();

                // Create Provider URI
                string providerUriQueryParameters = Apache.NMS.Util.URISupport.CreateQueryString(queryOptions);
                string providerUriBase = GetSecureProdiverURIString();
                string providerUri = string.Format("{0}?{1}", providerUriBase, providerUriQueryParameters);

                // Create Provider connection factory
                IConnectionFactory connectionFactory = CreateConnectionFactory();
                connectionFactory.BrokerUri = new Uri(providerUri);
                ConnectionFactory providerConnectionFactory = connectionFactory as ConnectionFactory;

                Exception connectionFailure = null;
                bool foundCert = false;

                // use the LocalCertificateSelect callback to test the localCertificates parameter for the client certificate.

                providerConnectionFactory.LocalCertificateSelect = (sender, targetHost, localCertificates, remoteCertificate, issuers) =>
                {
                    X509Certificate selected = null;
                    foreach (X509Certificate localCert in localCertificates)
                    {
                        byte[] localCertBytes = localCert.GetRawCertData();
                        if (localCertBytes.Length == clientCertBytes.Length)
                        {
                            int bytesMatched = 0;
                            for (int i=0; i<localCertBytes.Length; i++)
                            {

                                if (localCertBytes[i] != clientCertBytes[i])
                                {
                                    break;
                                }
                                bytesMatched++;
                            }
                            if(bytesMatched == clientCertBytes.Length)
                            {
                                foundCert = true;
                                selected = localCert;
                                break;
                            }
                        }
                    }
                    return selected;
                };

                using(Connection = CreateSecureConnection(connectionFactory))
                {
                    // Connect to invoke callback to test.
                    TestSecureConnect(Connection, out connectionFailure);

                    // log connection failure for reference or debugging.
                    if (connectionFailure != null)
                    {
                        Logger.Info("Caught exception for secure connection " + connectionFailure);
                    }

                    Assert.IsTrue(foundCert, "Could not find certificate {0} when added using property \"{1}\"", clientCertificateFileName, NMSPropertyConstants.NMS_SECURE_TRANSPORT_CLIENT_CERT_FILE_NAME);
                }

            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
        }
    }
}