using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Apache.NMS;
using NMS.AMQP.Test.Util;
using NMS.AMQP.Test.Attribute;

namespace NMS.AMQP.Test.TestCase
{
    [TestFixture]
    class ProducerTest : BaseTestCase
    {

        const int TIMEOUT = 15000; // 15 secs

        public override void Setup()
        {
            base.Setup();
            waiter = new System.Threading.ManualResetEvent(false);
        }
        
        
        [Test]
        //[Repeat(25)]
        [ConnectionSetup(null,"c1")]
        [SessionSetup("c1","s1")]
        [TopicSetup("s1","t1",Name = "nms.topic")]
        [ConsumerSetup("s1","t1","drain")]
        public void TestMultipleProducerCreateAndSend()
        {
            const int NUM_MSGS = 2000;
            const int NUM_PRODUCERS = 5;
            IMessageProducer producer = null;
            IList<IMessageProducer> producers = null;
            try
            {
                using (IConnection connection = this.GetConnection("c1"))
                using (ISession session = this.GetSession("s1"))
                using (IDestination destination = this.GetDestination("t1"))
                using (IMessageConsumer drain = this.GetConsumer("drain"))
                {
                    drain.Listener += CreateListener(NUM_MSGS);
                    connection.ExceptionListener += DefaultExceptionListener;
                    connection.Start();
                    producers = new List<IMessageProducer>();
                    for (int i = 0; i < NUM_PRODUCERS; i++)
                    {
                        try
                        {
                            producer = session.CreateProducer(destination);
                        }
                        catch (Exception ex)
                        {
                            this.PrintTestFailureAndAssert(this.GetMethodName(), "Failed to Created Producer " + i, ex);
                        }
                        producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
                        producer.DisableMessageID = true;
                        producer.TimeToLive = TimeSpan.FromMilliseconds(1500);
                        producers.Add(producer);
                    }

                    Assert.AreEqual(NUM_PRODUCERS, producers.Count, "Did not create all producers.");
                    Assert.IsNull(asyncEx, "Exception Listener Called While creating producers. With exception {0}.", asyncEx);


                    ITextMessage msg = session.CreateTextMessage();
                    int producerIndex = -1;
                    for (int i = 0; i < NUM_MSGS; i++)
                    {
                        msg.Text = "Index:" + i;
                        msg.Properties["MsgIndex"] = i;
                        producerIndex = i % NUM_PRODUCERS;
                        producers[producerIndex].Send(msg);
                    }

                    Assert.IsNull(asyncEx, "Exception Listener Called While sending messages. With exception {0}.", asyncEx);

                    Assert.IsTrue(waiter.WaitOne(TIMEOUT), "Failed to received all messages in {0}ms.", TIMEOUT);
                    Assert.AreEqual(NUM_MSGS, msgCount, "Failed to receive messages sent.");

                    Assert.IsNull(asyncEx, "Exception Listener Called While receiveing messages. With exception {0}.", asyncEx);
                }
            }
            catch (Exception e)
            {
                this.PrintTestFailureAndAssert(this.GetMethodName(), "Unexpected Exception.", e);
            }
            finally
            {
                if(producers != null)
                {
                    foreach(IMessageProducer p in producers)
                    {
                        p?.Close();
                        p?.Dispose();
                    }
                    producers.Clear();
                }
            }
        }
        //*/
    }
}