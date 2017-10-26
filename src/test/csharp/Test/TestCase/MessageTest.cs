using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using Apache.NMS.Util;
using NMS.AMQP.Test.Util;
using NMS.AMQP.Test.Attribute;

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
            base.Setup();
        }

        public override void TearDown()
        {
            base.TearDown();
            Connection = null;
            Session = null;
            Destination = null;
            Producer = null;
        }
        
        [Test]
        [ProducerSetup("default", "test")]
        [TopicSetup("default", "test", Name = "nms.test")]
        [SessionSetup("dotnetConn", "default")]
        [ConnectionSetup("default", "dotnetConn", EncodingType = "dotnet")]
        public void TestObjectMessageDotnetEncoding()
        {
            using (Session = GetSession("default"))
            using (Producer = GetProducer())
            {
                object[] values = new object[]
                {
                    new Number(){ Value = 12354638},
                    new Number(){ Value = new Decimal(123456.1235468)},
                };
                try
                {
                    IMessage msg = null;
                    foreach (object value in values)
                    {
                        msg = Session.CreateObjectMessage(value);
                        if (Logger.IsDebugEnabled)
                            Logger.Debug(msg.ToString());
                        Producer.Send(msg);
                    }
                }
                catch (Exception e)
                {
                    PrintTestFailureAndAssert(GetMethodName(), "Unexpected Exception", e);
                }
            }
        }

        [Test]
        [ConnectionSetup("default", "default")]
        [SessionSetup("default", "default")]
        [TopicSetup("default", Name = "nms.test")]
        [ProducerSetup("default", DeliveryMode = MsgDeliveryMode.Persistent)]
        public void TestObjectMessage()
        {
            using (Connection = GetConnection("default"))
            using (Session = GetSession("default"))
            using (Destination = GetDestination())
            using (Producer = GetProducer())
            {
                IMessage msg = null;
                try
                {
                    
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
                    foreach (object value in values)
                    {
                        msg = Session.CreateObjectMessage(value);
                        if (Logger.IsDebugEnabled)
                            Logger.Debug(msg.ToString());
                        Producer.Send(msg);
                    }

                }
                catch (Exception e)
                {
                    PrintTestFailureAndAssert(GetMethodName(), "Unexpected Exception", e);
                }

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