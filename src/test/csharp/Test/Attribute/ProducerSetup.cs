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

    #region Producer Setup Attribute Class

    internal class ProducerSetupAttribute : SessionParentDestinationDependentSetupAttribute
    {

        public MsgDeliveryMode DeliveryMode { get; set; } = NMSConstants.defaultDeliveryMode;

        public MsgPriority MsgPriority { get; set; } = NMSConstants.defaultPriority;

        public int RequestTimeout { get; set; } = -1;

        protected override string InstanceName { get { return typeof(IMessageProducer).Name; } }

        public ProducerSetupAttribute(string parentId, string destinationId, string[] producerIds) : base(parentId, destinationId, producerIds) { }

        public ProducerSetupAttribute(string parentId, string destinationId, string producerId) : this(parentId, destinationId, new string[] { producerId }) { }

        public ProducerSetupAttribute(string parentId = null, string destinationId = null) : this(parentId, destinationId, new string[] { null }) { }

        public override void BeforeTest(ITest test)
        {
            base.BeforeTest(test);
            InitializeNUnitTest<IMessageProducer, ISession>(test);
        }

        public override void Setup(BaseTestCase nmsTest)
        {
            base.Setup(nmsTest);
            InitializeTest<IMessageProducer, ISession>(nmsTest);
        }

        protected void InitializeProducerProperties(IMessageProducer producer)
        {
            if (MsgPriority != NMSConstants.defaultPriority)
            {
                producer.Priority = MsgPriority;
            }
            if (DeliveryMode != NMSConstants.defaultDeliveryMode)
            {
                producer.DeliveryMode = DeliveryMode;
            }
            if (RequestTimeout != -1)
            {
                producer.RequestTimeout = TimeSpan.FromMilliseconds(RequestTimeout);
            }
        }

        protected override T CreateNMSInstance<T, P>(BaseTestCase test, P parent)
        {
            IMessageProducer producer = test.CreateProducer((ISession)parent, this.GetDestination(test));
            InitializeProducerProperties(producer);
            return (T)producer;
        }

        protected override void AddInstance<T>(BaseTestCase test, T instance, string id)
        {
            test.AddProducer((IMessageProducer)instance, id);
        }

    }

    #endregion // End Producer Setup

}