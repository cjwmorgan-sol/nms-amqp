using System;
using System.Collections.Specialized;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;
using NMS.AMQP.Test.Attribute;

namespace NMS.AMQP.Test.TestCase
{
    [TestFixture]
    class SessionTest : BaseTestCase
    {
        [Test]
        [ConnectionSetup("default","default"/*, ClientId = "ID:foobartest"*/)]
        [SessionSetup("default")]
        public void TestSessionStart()
        {
            Logger.Error(this.ToString());
            IConnection conn = this.GetConnection("default");
            Logger.Error(conn.ToString());
            ISession session = conn.CreateSession();
            conn.Start();
            
            conn.Close();
        }

        [Test]
        [ConnectionSetup(null, "default")]
        [SessionSetup("default", "default")]
        public void TestSessionThrowIfConnectionClosed()
        {
            using (IConnection conn = GetConnection())
            using (ISession session = GetSession("default"))
            {
                conn.Start();
                IMessage msg = session.CreateMessage();
                try
                {
                    conn.Close();
                    msg = session.CreateMessage();
                    Assert.Fail("Should Throw NMSException for Closed Session.");
                }
                catch(NMSException ne)
                {
                    Assert.True(ne is IllegalStateException, "Didn't receive Correct NMSException for Operation on closed Session.");
                    Assert.AreEqual("Invalid Operation on Closed session.", ne.Message);
                }
                catch (Exception e)
                {
                    PrintTestFailureAndAssert(GetMethodName(), "Expected Excepted Exception.", e);
                }
                finally
                {
                    session?.Dispose();
                    conn?.Dispose();
                }
                

            }   
            
        }

        [Test]
        [ConnectionSetup(null, "default")]
        [SessionSetup("default", "default")]
        public void TestSessionThrowIfSessionClosed()
        {
            using (IConnection conn = GetConnection())
            using (ISession session = GetSession("default"))
            {
                conn.Start();
                IMessage msg = session.CreateMessage();

                try
                {
                    session.Close();
                    msg = session.CreateMessage();
                    Assert.Fail("Should Throw NMSException for Closed Session.");
                }
                catch (NMSException ne)
                {
                    Assert.True(ne is IllegalStateException, "Didn't receive Correct NMSException for Operation on closed Session.");
                    Assert.AreEqual("Invalid Operation on Closed session.", ne.Message);
                }
                catch (Exception e)
                {
                    PrintTestFailureAndAssert(GetMethodName(), "Expected Excepted Exception.", e);
                }
                finally
                {
                    session?.Dispose();
                    conn?.Dispose();
                }


            }

        }
        

    }
}
