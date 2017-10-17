using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP.Test.Util;

namespace NMS.AMQP.Test.TestCase
{
    [TestFixture]
    class MessageTest : BaseTestCase
    {
        protected IDestination Destination;
        protected IMessageProducer Producer;
        protected ISession Session;
        protected IConnection Connection;
        public override void Setup()
        {
            //Logger.Info(string.Format("Setup TestCase {0}.", this.GetType().Name));
            base.Setup();
            Connection = this.CreateConnection();
            Session = Connection.CreateSession();
            Destination = Session.GetTopic("nms.test");
            Producer = null;
        }

        protected override void InitConnectedFactoryProperties()
        {
            //Logger.Info(string.Format("Init {0} test properties.", this.GetType().Name));
            base.InitConnectedFactoryProperties();
            //ConnectionFactoryProperties["NMS.Message.Serialization"] = "dotnet";
        }

        public override void TearDown()
        {
            base.TearDown();

            if (Producer != null)
            {
                Producer.Close();
            }

            if (Session != null)
            {
                Session.Close();
            }

            if (Connection != null)
            {
                Connection.Close();
            }
            
        }
        
        [Test]
        public void TestObjectMessage()
        {
            IMessage msg = null;
            try
            {
                Producer = Session.CreateProducer(Destination);
                IPrimitiveMap map = new PrimitiveMap();
                map["myKey"] = "foo";
                map["myOtherKey"] = 58;
                map["myMapKey"] = new Dictionary<string, object>()
                {
                    {
                        "test", new List<object>
                        {
                            null,
                            1.12f,
                            "barfoo"
                        }
                    },
                    { "key1", 0xfffff454564 }
                };
                object[] values = new object[]
                {
                    null,
                    123456789,
                    1.12f,
                    55.84846,
                    3000000000L,
                    long.MaxValue,
                    int.MinValue,
                    map,
                    "FooBar",
                    new List<object>
                    {
                        null,
                        1.12f,
                        "barfoo"
                    },
                    new byte[]{ 0x66,0x65,0x77 },
                    'c',
                    
                };
                foreach(object value in values)
                {
                    msg = Session.CreateObjectMessage(value);
                    if (Logger.IsDebugEnabled)
                        Logger.Debug(msg.ToString());
                    Producer.Send(msg);
                }
                
            }
            catch(Exception e)
            {
                PrintTestFailureAndAssert(GetMethodName(), "Unexpected Exception", e);
            }
            

        }

        [Serializable]
        protected class Number 
        {
            private Decimal value;

            internal Number() : this(Decimal.Zero) { }
            internal Number(Decimal val) { value = val; }
            
            public Decimal Value
            {
                get { return value; }
                set { this.value = value; }
            }
        }

    }
}