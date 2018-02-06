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
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1")]
        [QueueSetup("s1","q1", Name = "nms.unique.queue")]
        [ProducerSetup("s1", "q1","sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        public void TestProducerSend()
        {
            const int NUM_MSGS = 100;

            try
            {
                using(IConnection connection = GetConnection("c1"))
                using(IMessageProducer producer = GetProducer("sender"))
                {
                    connection.ExceptionListener += DefaultExceptionListener;
                    ITextMessage textMessage = producer.CreateTextMessage();
                    for(int i=0; i<NUM_MSGS; i++)
                    {
                        textMessage.Text = "msg:" + i;
                        producer.Send(textMessage);

                    }
                    waiter.WaitOne(2000); // wait 2s for message to be received by broker.
                    Assert.IsNull(asyncEx, "Received asynchronous exception. Message : {0}", asyncEx?.Message);
                    
                }
            }
            catch(Exception ex)
            {
                this.PrintTestFailureAndAssert(GetTestMethodName(), "Unexpected exception.", ex);
            }
        }

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1")]
        [QueueSetup("s1", "q1", Name = "nmsQueue1")]
        [QueueSetup("s1", "q2", Name = "nmsQueue2")]
        [TopicSetup("s1", "t1", Name = "nmsTopic1")]
        [TopicSetup("s1", "t2", Name = "nmsTopic2")]
        [ConsumerSetup("s1", "q1", "cq1")]
        [ConsumerSetup("s1", "q2", "cq2")]
        [ConsumerSetup("s1", "t1", "ct1")]
        [ConsumerSetup("s1", "t2", "ct2")]
        public void TestAnonymousProducerSend()
        {
            const int NUM_MSGS = 100;
            IList<IDestination> destinations = this.GetDestinations(new string[] { "q1", "q2", "t1", "t2" });
            IList<IMessageConsumer> consumers = this.GetConsumers(new string[] { "cq1", "cq2", "ct1", "ct2" });
            int DestinationPoolSize = destinations.Count;
            int ConsumerPoolSize = consumers.Count;
            
            IMessageProducer producer = null;
            
            using (ISession session = GetSession("s1"))
            using (IConnection connection = GetConnection("c1"))
            {
                try
                {
                    connection.ExceptionListener += DefaultExceptionListener;
                    foreach(IMessageConsumer c in consumers)
                    {
                        c.Listener += CreateListener(NUM_MSGS);
                    }

                    connection.Start();
                    
                    producer = session.CreateProducer();
                    producer.DeliveryMode = MsgDeliveryMode.Persistent;
                    ITextMessage textMessage = producer.CreateTextMessage();

                    for (int i = 0; i < NUM_MSGS; )
                    {
                        foreach (IDestination dest in destinations)
                        {
                            Logger.Info("Sending message " + i + " to destination " + dest.ToString());
                            textMessage.Text = "Num:" + dest.ToString() + ":" + i;
                            i++;

                            producer.Send(dest, textMessage);
                        }
                    }

                    Assert.IsTrue(waiter.WaitOne(TIMEOUT), "Failed to received all messages. Received {0} of {1} in {2}ms.", msgCount, NUM_MSGS, TIMEOUT);

                }
                catch (Exception ex)
                {
                    this.PrintTestFailureAndAssert(GetTestMethodName(), "Unexpected exception.", ex);
                }
            }
            
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
        
        #region Destination Tests

        private void TestDestinationMessageDelivery(
            IConnection connection, 
            ISession session, 
            IMessageProducer producer, 
            IDestination destination, 
            int msgPoolSize, 
            bool isDurable = false)
        {
            const string PROP_KEY = "send_msg_id";

            int TotalMsgSent = 0;
            int TotalMsgRecv = 0;

            try
            {
                
                IMessageConsumer consumer = session.CreateConsumer(destination);
                ITextMessage sendMessage = session.CreateTextMessage();

                consumer.Listener += CreateListener(msgPoolSize);
                connection.ExceptionListener += DefaultExceptionListener;

                connection.Start();

                for (int i = 0; i < msgPoolSize; i++)
                {
                    sendMessage.Text = "Msg:" + i;
                    sendMessage.Properties.SetInt(PROP_KEY, TotalMsgSent);
                    producer.Send(sendMessage);
                    TotalMsgSent++;
                }
                
                bool signal = waiter.WaitOne(TIMEOUT);
                TotalMsgRecv = msgCount;
                Assert.IsTrue(signal, "Timed out waiting to receive messages. Received {0} of {1} in {2}ms.", msgCount, TotalMsgSent, TIMEOUT);
                Assert.AreEqual(TotalMsgSent, msgCount, "Failed to receive all messages. Received {0} of {1} in {2}ms.", msgCount, TotalMsgSent, TIMEOUT);

                // close consumer
                consumer.Close();
                // reset waiter
                waiter.Reset();

                for (int i = 0; i < msgPoolSize; i++)
                {
                    sendMessage.Text = "Msg:" + i;
                    sendMessage.Properties.SetInt(PROP_KEY, TotalMsgSent);
                    producer.Send(sendMessage);
                    TotalMsgSent++;
                    if(isDurable || destination.IsQueue)
                    {
                        TotalMsgRecv++;
                    }
                }

                int expectedId = (isDurable || destination.IsQueue) ? msgPoolSize : TotalMsgSent;

                connection.Stop();

                // expectedMsgCount is 2 msgPoolSize groups for non-durable topics, one for initial send of pool size and one for final send of pool size.
                // expedtedMsgCount is 3 msgPoolSize groups for queues and durable topics, same two groups for non-durable topic plus the group sent while there is no active consumer.
                int expectedMsgCount = (isDurable || destination.IsQueue) ? 3 * msgPoolSize : 2 * msgPoolSize;

                MessageListener callback = CreateListener(expectedMsgCount);
                string errString = null;
                consumer = session.CreateConsumer(destination);
                consumer.Listener += (m) =>
                {
                    int id = m.Properties.GetInt(PROP_KEY);
                    if (id != expectedId)
                    {
                        errString = string.Format("Received Message out of order. Received msg : {0} Expected : {1}", id, expectedId);
                        waiter.Set();
                        return;
                    }
                    else
                    {
                        expectedId++;
                    }
                    callback(m);
                };
                connection.Start();



                for (int i = 0; i < msgPoolSize; i++)
                {
                    sendMessage.Text = "Msg:" + i;
                    sendMessage.Properties.SetInt(PROP_KEY, TotalMsgSent);
                    producer.Send(sendMessage);
                    TotalMsgSent++;
                    TotalMsgRecv++;
                }

                signal = waiter.WaitOne(TIMEOUT);
                Assert.IsNull(asyncEx, "Received asynchrounous exception. Message: {0}", asyncEx?.Message);
                Assert.IsNull(errString, "Failure occured on Message Callback. Message : {0}", errString ?? "");
                Assert.IsTrue(signal, "Timed out waiting for message receive. Received {0} of {1} in {2}ms.", msgCount, TotalMsgRecv, TIMEOUT);
                Assert.AreEqual(TotalMsgRecv, msgCount, "Failed to receive all messages. Received {0} of {1} in {2}ms.", msgCount, TotalMsgRecv, TIMEOUT);
                consumer.Close();
                
            }
            catch (Exception ex)
            {
                this.PrintTestFailureAndAssert(this.GetTestMethodName(), "Unexpected Exception", ex);
            }
        }

        #region Queue Destination Tests

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1")]
        [QueueSetup("s1", "q1", Name = "nms.queue")]
        [ProducerSetup("s1", "q1", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        public void TestQueueMessageDelivery()
        {
            const int NUM_MSGS = 100;

            IDestination destination = GetDestination("q1");

            using (IConnection connection = GetConnection("c1"))
            using (ISession session = GetSession("s1"))
            using (IMessageProducer producer = GetProducer("sender"))
            {
                TestDestinationMessageDelivery(connection, session, producer, destination, NUM_MSGS);
            }

        }

        #endregion // end queue tests

        #region Topic Tests

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1")]
        [TopicSetup("s1", "t1", Name = "nms.test")]
        [ProducerSetup("s1", "t1", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        public void TestTopicMessageDelivery()
        {
            const int NUM_MSGS = 100;

            IDestination destination = GetDestination("t1");

            using (IConnection connection = GetConnection("c1"))
            using (ISession session = GetSession("s1"))
            using (IMessageProducer producer = GetProducer("sender"))
            {
                TestDestinationMessageDelivery(connection, session, producer, destination, NUM_MSGS);
            }

        }

        #endregion // end topic tests

        #region Temporary Destination Tests

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1", "s1")]
        [TemporaryQueueSetup("s1", "temp1")]
        [ProducerSetup("s1", "temp1", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        public void TestTemporaryQueueMessageDelivery()
        {
            const int NUM_MSGS = 100;

            IDestination destination = GetDestination("temp1");

            using (IConnection connection = GetConnection("c1"))
            using (ISession session = GetSession("s1"))
            using (IMessageProducer producer = GetProducer("sender"))
            {
                TestDestinationMessageDelivery(connection, session, producer, destination, NUM_MSGS);
            }

        }

        [Test]
        [ConnectionSetup(null, "c1")]
        [SessionSetup("c1","s1")]
        [TemporaryTopicSetup("s1", "temp1")]
        [ProducerSetup("s1", "temp1", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        public void TestTemporaryTopicMessageDelivery()
        {
            const int NUM_MSGS = 100;
            
            IDestination destination = GetDestination("temp1");

            using (IConnection connection = GetConnection("c1"))
            using (ISession session = GetSession("s1"))
            using (IMessageProducer producer = GetProducer("sender"))
            {
                TestDestinationMessageDelivery(connection, session, producer, destination, NUM_MSGS);
            }
            
        }


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
        [TopicSetup("s1", "t1", Name = "nms.t.temp.reply.to.topic")]
        [TemporaryTopicSetup("s1", "temp1")]
        [ProducerSetup("s1", "t1", "sender", DeliveryMode = MsgDeliveryMode.NonPersistent)]
        [ConsumerSetup("s2", "t1", "receiver")]
        [ProducerSetup("s2", "temp1", "replyer", DeliveryMode = MsgDeliveryMode.Persistent, TimeToLive = 2500)]
        [ConsumerSetup("s1", "temp1", "listener")]
        public void TestTemporaryTopicReplyTo()
        {
            const int NUM_MSGS = 100;
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

        #endregion // end Temporary Destination tests

        #endregion // end Destination tests
    }
}