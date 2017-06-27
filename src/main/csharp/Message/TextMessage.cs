using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace NMS.AMQP.Message
{
    using Cloak;
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
