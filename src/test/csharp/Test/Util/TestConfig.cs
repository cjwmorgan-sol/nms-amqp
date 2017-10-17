using System;

namespace NMS.AMQP.Test.Util
{
    public class TestConfig
    {
        public const string DEFAULT_BROKER_IP_ADDRESS = "192.168.2.69";
        public const string DEFAULT_BROKER_PORT = "5672";
        public const string DEFAULT_LOG_LEVEL = "Warn";

        public string BrokerIpAddress = DEFAULT_BROKER_IP_ADDRESS;
        public string BrokerPort = DEFAULT_BROKER_PORT;
        public string BrokerUsername = null;
        public string BrokerPassword = null;
        public string ClientId = null;


        protected Uri uri = null; 
        public Uri BrokerUri
        {
            get
            {
                if(uri == null)
                {
                    uri = new Uri("amqp://" + BrokerIpAddress + ":" + BrokerPort);
                }
                return uri;
            }
        }

        public string LogLevel = DEFAULT_LOG_LEVEL;
        public bool AmqpFrameTrace = false;

        private TestConfig()
        {
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
}
