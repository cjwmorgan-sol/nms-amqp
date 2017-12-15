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

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1")]
        [TemporaryTopicSetup("s1", "temp1")]
        [ProducerSetup("s1", "temp1", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        public void TestCannotSendOnDeletedTemporaryTopic()
        {
            
            try
            {
                using (IConnection connection = GetConnection("c1"))
                using (IDestination destination = GetDestination("temp1"))
                using (IMessageProducer producer = GetProducer("sender"))
                {
                    ITemporaryTopic tempTopic = destination as ITemporaryTopic;
                    Assert.NotNull(tempTopic, "Failed to Create Temporary Topic.");
                    IMessage msg = producer.CreateMessage();
                    tempTopic.Delete();
                    try
                    {
                        producer.Send(msg);
                        Assert.Fail("Expected Exception for sending message on deleted temporary topic.");
                    }
                    catch(NMSException nex)
                    {
                        Assert.IsTrue(nex is InvalidDestinationException, "Received Unexpected exception {0}", nex);
                    }
                    
                    
                }

            }
            catch(Exception ex)
            {
                this.PrintTestFailureAndAssert(GetTestMethodName(), "Unexpected exception.", ex);
            }
        }

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1", AckMode = AcknowledgementMode.IndividualAcknowledge)]
        [SessionSetup("c1", "s2")]
        [TopicSetup("s1", "t1", Name = "nms.topic")]
        [TemporaryTopicSetup("s1", "temp1")]
        [ProducerSetup("s1", "t1", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        [ConsumerSetup("s2", "t1", "receiver")]
        [ProducerSetup("s2", "temp1", "replyer", DeliveryMode = MsgDeliveryMode.Persistent, TimeToLive = 2500)]
        [ConsumerSetup("s1", "temp1", "listener")]
        public void TestTemporaryTopicReplyTo()
        {
            const int NUM_MSGS = 1000;
            const string MSG_BODY = "num : ";
            IDestination replyTo = GetDestination("temp1");
            long repliedCount = 0;
            long lastRepliedId = -1;
            string errString = null;

            using (IConnection connection = GetConnection("c1"))
            using (IMessageConsumer receiver = GetConsumer("receiver"))
            using (IMessageConsumer listener = GetConsumer("listener"))
            using (IMessageProducer sender = GetProducer("sender"))
            using (IMessageProducer replyer = GetProducer("replyer"))
            {
                try
                {
                    connection.ExceptionListener += DefaultExceptionListener;
                    ITextMessage rmsg = null;
                    ITextMessage sendMsg = sender.CreateTextMessage();
                    sendMsg.NMSReplyTo = replyTo;

                    listener.Listener += (message) => 
                    {
                        if (errString == null)
                        {
                            repliedCount++;
                            long msgId = ExtractMsgId(message.NMSMessageId);
                            if (msgId != lastRepliedId + 1)
                            {
                                // Test failed release blocked thread for shutdown.
                                errString = String.Format("Received msg {0} out of order expected {1}", msgId, lastRepliedId + 1);
                                waiter.Set();
                            }
                            else
                            {
                                lastRepliedId = msgId;
                                if (msgId == NUM_MSGS - 1)
                                {
                                    // test done signal complete.
                                    waiter.Set();
                                }
                                message.Acknowledge();
                            }
                        }
                    };

                    receiver.Listener += (message) =>
                    {
                        if (errString == null)
                        {


                            msgCount++;
                            rmsg = message as ITextMessage;
                            if (rmsg == null)
                            {
                                // test failure
                                errString = string.Format(
                                    "Received message, id = {2}, body of type {0}, expected {1}.", 
                                    message.GetType().Name, 
                                    typeof(ITextMessage).Name, 
                                    ExtractMsgId(message.NMSMessageId)
                                    );
                                waiter.Set();
                                return;
                            }
                            IDestination replyDestination = message.NMSReplyTo;
                            if (!replyDestination.Equals(replyTo))
                            {
                                // test failure
                                errString = string.Format(
                                    "Received message, id = {0}, with incorrect reply Destination. Expected : {1}, Actual : {2}.",
                                    ExtractMsgId(message.NMSMessageId),
                                    replyTo,
                                    replyDestination
                                    );
                                waiter.Set();
                                return;
                            }
                            else
                            {
                                ITextMessage reply = replyer.CreateTextMessage();
                                reply.Text = "Received:" + rmsg.Text;
                                try
                                {
                                    replyer.Send(reply);
                                }
                                catch (NMSException nEx)
                                {
                                    Logger.Error("Failed to send message from replyer Cause : " + nEx);
                                    throw nEx;
                                }
                            }
                        }
                    };

                    connection.Start();

                    for(int i=0; i<NUM_MSGS; i++)
                    {
                        sendMsg.Text = MSG_BODY + i;
                        sender.Send(sendMsg);
                    }

                    if(!waiter.WaitOne(250*NUM_MSGS))
                    {
                        Assert.Fail("Timed out waiting on message delivery to complete. Received {1} of {0}, Replied {2} of {0}, Last Replied Msg Id {3}.", NUM_MSGS, msgCount, repliedCount, lastRepliedId);
                    }
                    else if(errString != null)
                    {
                        Assert.Fail("Asynchronous failure occurred. Cause : {0}", errString);
                    }
                    else
                    {
                        Assert.IsNull(asyncEx, "Received Exception Asynchronously. Cause : {0}", asyncEx);
                        Assert.AreEqual(NUM_MSGS, msgCount, "Failed to receive all messages.");
                        Assert.AreEqual(NUM_MSGS, repliedCount, "Failed to reply to all messages");
                        Assert.AreEqual(NUM_MSGS - 1, lastRepliedId, "Failed to receive the final message");
                    }

                }
                catch (Exception ex)
                {
                    this.PrintTestFailureAndAssert(GetTestMethodName(), "Unexpected exception.", ex);
                }
                
            }
        }
        
        [Test]
        [ConnectionSetup(null, "default")]
        [SessionSetup("default", "s1")]
        public void TestCreateTemporaryDestination()
        {
            const int NUM_MSGS = 100;
            try
            {


                using (IConnection connection = GetConnection("default"))
                using (ISession session = GetSession("s1"))
                {
                    IStreamMessage msg = session.CreateStreamMessage();
                    
                    IDestination temp = session.CreateTemporaryQueue();
                    IMessageProducer producer = session.CreateProducer(temp);
                    
                    for (int i = 0; i < NUM_MSGS; i++)
                    {
                        msg.WriteObject("foobar");
                        msg.WriteObject(i);

                        msg.Properties.SetInt("count", i);

                        producer.Send(msg);

                        msg.ClearBody();
                    }

                    temp = session.CreateTemporaryTopic();
                    producer = session.CreateProducer(temp);

                    for (int i = 0; i < NUM_MSGS; i++)
                    {
                        msg.WriteObject("foobar");
                        msg.WriteObject(i);

                        msg.Properties.SetInt("count", i);

                        producer.Send(msg);

                        msg.ClearBody();
                    }

                }
            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(GetTestMethodName(), "Unexpected exception.", ex);
            }
        }

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1", "s2", "tFactory")]
        [TemporaryTopicSetup("tFactory", "temp")]
        [ProducerSetup("s2", "temp", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        [ConsumerSetup("s1", "temp", "receiver")]
        public void TestSendToTemporaryOnClosedSession()
        {
            const int NUM_MSGS = 100;
            string errString = null;
            const int TIMEOUT = NUM_MSGS * 100;
            try
            {
                using (IConnection connection = GetConnection("c1"))
                using (ISession tFactory = GetSession("tFactory"))
                using (IMessageProducer producer = GetProducer("sender"))
                using (IMessageConsumer consumer = GetConsumer("receiver"))
                {
                    IDestination destination = GetDestination("temp");
                    IMapMessage mapMessage = producer.CreateMapMessage();
                    MessageListener ackCallback = CreateListener(NUM_MSGS);
                    MessageListener callback = (message) =>
                    {
                        if (errString == null)
                        {

                            if (!destination.Equals(message.NMSReplyTo))
                            {
                                errString = string.Format("Received message, id = {0}, has incorrect ReplyTo property.", ExtractMsgId(message.NMSMessageId));
                                waiter.Set();
                            }

                            ackCallback(message);
                        }
                    };
                    consumer.Listener += callback;
                    connection.ExceptionListener += DefaultExceptionListener;

                    mapMessage.NMSReplyTo = destination;

                    connection.Start();

                    // close session
                    tFactory.Close();

                    for(int i = 0; i < NUM_MSGS; i++)
                    {
                        mapMessage.Body.SetString("Link", "temp");
                        mapMessage.Body.SetInt("count", i);

                        producer.Send(mapMessage);

                        mapMessage.ClearBody();
                    }

                    if (!waiter.WaitOne(TIMEOUT))
                    {
                        if(errString == null)
                        {
                            Assert.Fail("Timed out waiting messages. Received, {0} of {1} messages in {2}ms.", msgCount, NUM_MSGS, TIMEOUT);
                        }
                        else
                        {
                            Assert.Fail(errString);
                        }
                    }

                    Assert.AreEqual(NUM_MSGS, msgCount, "Did not receive expected number of messages.");
                }
            }
            catch(Exception ex)
            {
                this.PrintTestFailureAndAssert(GetTestMethodName(), "Unexpected exception.", ex);
            }
        }

    }
}