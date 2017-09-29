using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP.Message
{
    using Cloak;
    /// <summary>
    /// NMS.AMQP.Message.TextMessage inherits from NMS.AMQP.Message.Message that implements the Apache.NMS.ITextMessage interface.
    /// NMS.AMQP.Message.TextMessage uses the NMS.AMQP.Message.Cloak.ITextMessageCloak interface to detach from the underlying AMQP 1.0 engine.
    /// </summary>
    class TextMessage : Message, ITextMessage
    {
        protected readonly new ITextMessageCloak cloak;

        #region Constructor

        internal TextMessage(ITextMessageCloak cloak) : base(cloak)
        {
            this.cloak = cloak;
        }

        #endregion

        #region ITextMessage Properties

        public string Text
        {
            get
            {
                return cloak.Text;
            }

            set
            {
                FailIfReadOnlyMsgBody();
                cloak.Text = value;
            }
        }

        #endregion
    }
}
