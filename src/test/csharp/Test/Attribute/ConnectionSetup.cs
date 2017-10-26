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
    
    #region Connection Setup Attribute Class

    internal class ConnectionSetupAttribute : TestSetupAttribute
    {
        public string EncodingType { get; set; } = null;
        public string ClientId { get; set; } = null;

        protected override string InstanceName { get { return typeof(IConnection).Name; } }
        protected override string ParentName { get { return typeof(IConnectionFactory).Name; } }

        protected override int ExecuteOrder { get { return 1; } }

        public ConnectionSetupAttribute(string nmsConnectionFactoryId, params string[] nmsConnectionIds) : base(nmsConnectionFactoryId, nmsConnectionIds)
        { }
        public ConnectionSetupAttribute(string nmsConnectionFactoryId, string nmsConnectionId) : this(nmsConnectionFactoryId, new string[] { nmsConnectionId }) { }
        public ConnectionSetupAttribute(string nmsConnectionFactoryId = null) : this(nmsConnectionFactoryId, new string[] { null }) { }


        public override void BeforeTest(ITest test)
        {
            base.BeforeTest(test);
            InitializeNUnitTest<IConnection, IConnectionFactory>(test);
        }

        public override void Setup(BaseTestCase nmsTest)
        {
            base.Setup(nmsTest);
            InitializeTest<IConnection, IConnectionFactory>(nmsTest);
        }

        protected StringDictionary GetConnectionProperties(BaseTestCase nmsTest)
        {
            StringDictionary properties = new StringDictionary();
            if (ClientId != null && nmsInstanceIds.Length < 2)
            {
                properties[NMSPropertyConstants.NMS_CONNECTION_CLIENT_ID] = ClientId;
            }
            if (EncodingType != null)
            {
                properties[NMSPropertyConstants.NMS_CONNECTION_ENCODING] = EncodingType;
            }
            return properties;
        }

        protected IConnectionFactory GetConnectionFactory(BaseTestCase nmsTest)
        {
            IConnectionFactory cf = null;

            if (!nmsTest.NMSInstanceExists<IConnectionFactory>(parentIndex))
            {
                cf = nmsTest.CreateConnectionFactory();
                nmsTest.AddConnectionFactory(cf, NmsParentId);
            }
            else
            {
                if (NmsParentId == null)
                {
                    cf = nmsTest.GetConnectionFactory();
                }
                else
                {
                    cf = nmsTest.GetConnectionFactory(NmsParentId);
                }
            }
            BaseTestCase.Logger.Info("Found Connection Factory " + cf + "");
            return cf;
        }

        protected override T GetParentNMSInstance<T>(BaseTestCase nmsTest)
        {
            nmsTest.InitConnectedFactoryProperties(GetConnectionProperties(nmsTest));
            return (T)GetConnectionFactory(nmsTest);
        }

        protected override T CreateNMSInstance<T, P>(BaseTestCase test, P parent)
        {
            return (T)test.CreateConnection((IConnectionFactory)parent);
        }

        protected override void AddInstance<T>(BaseTestCase test, T instance, string id)
        {
            test.AddConnection((IConnection)instance, id);
        }

    }

    #endregion // end connection setup
    
}