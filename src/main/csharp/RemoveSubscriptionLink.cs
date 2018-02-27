using Apache.NMS;
using NMS.AMQP.Util;
using Amqp;
using Amqp.Framing;

namespace NMS.AMQP
{
    /// <summary>
    /// RemoveSubscriptionLink handles the amqp consumer link attach frames for the durable 
    /// consumer unsubscribe operation.
    /// </summary>
    internal class RemoveSubscriptionLink : MessageLink
    {
        internal RemoveSubscriptionLink(Session session, string name) : base(session, null)
        {
            IdGenerator idgen = new CustomIdGenerator(true, session.Connection.ClientId, name);
            Info = new SubscriptionInfo(idgen.GenerateId())
            {
                SubscriptionName = name,
                ClientId = session.Connection.ClientId
            };
        }

        protected new SubscriptionInfo Info
        {
            get
            {
                return base.Info as SubscriptionInfo;
            }
            set
            {
                base.Info = value;
            }
        }

        #region Public Methods

        public void Unsubscribe()
        {
            // attach will throw an exception should the remote subscription not exist or is invalid.
            this.Attach();
            // send amqp detach with closed set to true.
            this.Close();
        }

        #endregion

        #region MessageLink Methods

        protected override ILink CreateLink()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnInternalClosed(IAmqpObject sender, Error error)
        {
            Tracer.InfoFormat("Closed subscription {0}", this.Info.SubscriptionName);
            if (error != null && !this.IsOpening && !this.IsClosing)
            {
                Tracer.WarnFormat("Subscription {0} on connection {1} has been destroyed. Cause {1}", this.Info.SubscriptionName, this.Info.ClientId , error.ToString());
            }
            this.OnResponse();
        }

        protected override void StartResource()
        {
            // Remove Subscription Link does not start or stop it uses unsubscribe instead.
            throw new System.NotImplementedException();
        }

        protected override void StopResource()
        {
            // Remove Subscription Link does not start or stop it uses unsubscribe instead.
            throw new System.NotImplementedException();
        }

        #endregion

    }

    internal class SubscriptionInfo : LinkInfo
    {
        internal SubscriptionInfo(Id id) : base (id)
        {
        }
        public string SubscriptionName { get; set; }
        public string ClientId { get; set; }
    }
}