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
using NMS.AMQP.Test.TestCase;

namespace NMS.AMQP.Test.Attribute
{

    #region Consumer Setup Attribute Class

    internal class ConsumerSetupAttribute : SessionParentDestinationDependentSetupAttribute
    {

        public MessageListener OnMessage { get; set; } = null;

        protected override string InstanceName { get { return typeof(IMessageConsumer).Name; } }

        public ConsumerSetupAttribute(string sessionId, string destinationId, string[] consumerIds) : base(sessionId, destinationId, consumerIds) { }

        public ConsumerSetupAttribute(string sessionId, string destinationId, string consumerId) : this(sessionId, destinationId, new string[] { consumerId }) { }

        public ConsumerSetupAttribute(string sessionId = null, string destinationId = null) : this(sessionId, destinationId, new string[] { null }) { }

        public override void BeforeTest(ITest test)
        {
            base.BeforeTest(test);
            InitializeNUnitTest<IMessageConsumer, ISession>(test);
        }

        public override void Setup(BaseTestCase nmsTest)
        {
            base.Setup(nmsTest);
            InitializeTest<IMessageConsumer, ISession>(nmsTest);
        }

        protected void InitializeConsumerProperties(IMessageConsumer consumer)
        {
            consumer.Listener += OnMessage;
        }

        protected override T CreateNMSInstance<T, P>(BaseTestCase test, P parent)
        {
            IMessageConsumer consumer = test.CreateConsumer((ISession)parent, this.GetDestination(test));
            InitializeConsumerProperties(consumer);
            return (T)consumer;
        }

        protected override void AddInstance<T>(BaseTestCase test, T instance, string id)
        {
            test.AddConsumer((IMessageConsumer)instance, id);
        }

    }

    #endregion

}