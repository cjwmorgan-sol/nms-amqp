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

    #region Abstract Test Setup Attribute Classes

    #region Base Test Setup Attribute Class 

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class |
            AttributeTargets.Interface | AttributeTargets.Assembly,
            AllowMultiple = true)]
    internal abstract class TestSetupAttribute : System.Attribute, ITestAction
    {
        private enum SetupType { Unknown = -1, TestAction = 0, TestSetup = 1 }
        private static readonly Exception FailureException = new Exception("Test Setup Failure.");

        public bool IsTestAction
        {
            get
            {
                return type == SetupType.TestAction;
            }

            set
            {
                if (value == false)
                {
                    type = SetupType.TestSetup;
                }
                else
                {
                    type = SetupType.TestAction;
                }
            }
        }
        public string NmsParentId;
        protected int parentIndex = -1;
        protected string[] nmsInstanceIds;
        private SetupType type = SetupType.Unknown;

        protected abstract string InstanceName { get; }
        protected abstract string ParentName { get; }

        protected string TestName { get; private set; }

        protected TestSetupAttribute(string nmsParentId, string[] nmsIds)
        {
            if (nmsIds == null)
            {
                throw new ArgumentNullException("NMS " + InstanceName + " Ids can not be null.");
            }
            else if (nmsIds.Length == 0)
            {
                throw new ArgumentException("At Least one " + InstanceName + " must be specified.");
            }
            if (nmsParentId == null)
            {
                parentIndex = 0;
            }
            NmsParentId = nmsParentId;
            this.nmsInstanceIds = nmsIds;
            IsTestAction = false;
        }

        public ActionTargets Targets
        {
            get { return ActionTargets.Test; }
        }

        public virtual void AfterTest(ITest test)
        {

        }

        public virtual void BeforeTest(ITest test)
        {
            TestName = test.MethodName;
            if (type == SetupType.Unknown)
            {
                type = SetupType.TestAction;
            }
        }

        public virtual void Setup(BaseTestCase nmsTest)
        {
            TestName = TestContext.CurrentContext.Test.MethodName;
            if (type == SetupType.Unknown)
            {
                type = SetupType.TestSetup;
            }
        }

        protected void InitializeTest<T, P>(BaseTestCase nmsTest, bool fromAction = false)
        {
            if (nmsTest != null && (!IsTestAction || fromAction))
            {
                BaseTestCase.Logger.Info("IsTestAction : " + IsTestAction + " fromAction " + fromAction + "");
                if (BaseTestCase.Logger.IsDebugEnabled)
                    BaseTestCase.Logger.Debug("Initial test state: " + nmsTest.ToString());
                P parent = GetParentNMSInstance<P>(nmsTest);
                if (BaseTestCase.Logger.IsDebugEnabled)
                    BaseTestCase.Logger.Debug("Added parent test state: " + nmsTest.ToString());
                for (int i = 0; i < nmsInstanceIds.Length; i++)
                {
                    string id = nmsInstanceIds[i];
                    T instance = CreateNMSInstance<T, P>(nmsTest, parent);
                    if (BaseTestCase.Logger.IsDebugEnabled)
                        BaseTestCase.Logger.Debug("Adding Instance " + id + " test state: " + nmsTest.ToString());
                    AddInstance(nmsTest, instance, id);
                    if (BaseTestCase.Logger.IsDebugEnabled)
                        BaseTestCase.Logger.Debug("Added Instance " + id + " test state: " + nmsTest.ToString());
                }
            }
        }


        protected void InitializeNUnitTest<T, P>(ITest test)
        {
            BaseTestCase nmsTest = GetTest(test);
            if (IsTestAction)
                InitializeTest<T, P>(nmsTest, true);
        }

        protected BaseTestCase GetTest(ITest test)
        {
            object fixture = test.Fixture;
            if (fixture is BaseTestCase)
            {
                return fixture as BaseTestCase;
            }
            return null;
        }

        protected abstract P GetParentNMSInstance<P>(BaseTestCase test);

        protected abstract T CreateNMSInstance<T, P>(BaseTestCase test, P parent);
        protected abstract void AddInstance<T>(BaseTestCase test, T instance, string id);


        protected virtual void TestSetupFailure(BaseTestCase test, string message, Exception cause)
        {
            test.PrintTestFailureAndAssert(
                        TestName,
                        message,
                        cause
                        );
        }

        protected virtual void TestSetupFailure(BaseTestCase test, string message)
        {
            TestSetupFailure(test, message, FailureException);
        }

        protected virtual void TestSetupFailureParentNotFound(BaseTestCase test)
        {
            string id = (NmsParentId == null) ? parentIndex.ToString() : NmsParentId;

            string message = string.Format(
                "Failed to find Parent {0} {1} to create {2}.",
                ParentName, id, InstanceName
                );
            TestSetupFailure(test, message);
        }

        public int ComparableOrder
        {
            get
            {
                return int.MaxValue - ExecuteOrder;
            }
        }

        protected abstract int ExecuteOrder { get; }

    }

    #endregion //End base Base Test Setup

    #region Parent Session Setup Attribute Class

    abstract class SessionParentSetupAttribute : TestSetupAttribute
    {
        protected override string ParentName { get { return typeof(ISession).Name; } }

        protected override int ExecuteOrder { get { return 3; } }

        protected SessionParentSetupAttribute(string sessionId, string[] nmsIds) : base(sessionId, nmsIds) { }

        protected override P GetParentNMSInstance<P>(BaseTestCase test)
        {
            ISession session = null;
            if (!test.NMSInstanceExists<ISession>(parentIndex))
            {
                if (NmsParentId == null)
                {
                    TestSetupFailureParentNotFound(test);
                }
                else
                {
                    session = test.GetSession(NmsParentId);
                }
            }
            else
            {
                session = test.GetSession(parentIndex);
            }
            return (P)session;
        }

    }

    #endregion // end Session Parent Attribute Class

    #region Destination Dependent Attribute Class

    abstract class SessionParentDestinationDependentSetupAttribute : SessionParentSetupAttribute
    {
        public string DestinationId { get; set; }

        protected int destinationIndex = -1;

        protected override int ExecuteOrder { get { return 4; } }

        protected SessionParentDestinationDependentSetupAttribute(string sessionId, string destinationId, string[] nmsIds) : base(sessionId, nmsIds)
        {
            if (destinationId == null || destinationId.Length == 0)
            {
                destinationIndex = 0;
            }
            else
            {
                DestinationId = destinationId;
            }
        }

        protected IDestination GetDestination(BaseTestCase test)
        {
            IDestination destination = null;
            if (!test.NMSInstanceExists<IDestination>(destinationIndex))
            {
                if (DestinationId == null)
                {
                    TestSetupFailureDestinationNotFound(test);
                }
                else
                {
                    destination = test.GetDestination(DestinationId);
                }
            }
            else
            {
                destination = test.GetDestination(destinationIndex);
            }
            return destination;
        }

        protected virtual void TestSetupFailureDestinationNotFound(BaseTestCase test)
        {
            string destinationId = DestinationId ?? destinationIndex.ToString();
            string message = string.Format("Failed to find IDestination {0}, when creating {1}.",
                destinationId,
                InstanceName
                );
            TestSetupFailure(test, message);
        }

    }

    #endregion

    #endregion // end Attribute classes

}
