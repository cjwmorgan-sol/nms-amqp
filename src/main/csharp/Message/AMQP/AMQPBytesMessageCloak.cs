using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using Amqp.Framing;
using Amqp.Types;

namespace NMS.AMQP.Message.AMQP
{
    
    using Cloak;
    using Util;
    using Factory;
    class AMQPBytesMessageCloak : AMQPMessageCloak, IBytesMessageCloak
    {

        private static readonly Data EMPTY_DATA;

        static AMQPBytesMessageCloak()
        {
            EMPTY_DATA = new Data();
            EMPTY_DATA.Binary = new byte[0];
        }

        private EndianBinaryReader byteIn=null;
        private EndianBinaryWriter byteOut=null;

        internal AMQPBytesMessageCloak(Connection c) : base(c)
        {
            Content = null;
        }

        internal AMQPBytesMessageCloak(MessageConsumer c, Amqp.Message msg) : base (c, msg) { }

        internal override byte JMSMessageType { get { return MessageSupport.JMS_TYPE_BYTE; } }

        public override byte[] Content
        {
            get
            {
                //BinaryReader reader = DataIn;
                //byte[] buffer = new byte[BodyLength];
                //reader.Read(buffer, 0, BodyLength);
                //return buffer;
                
                return this.GetBinaryFromBody().Binary;
            }
            set
            {
                //BinaryWriter writer = DataOut;
                //if(value != null)
                //{
                //    writer.Write(value, 0, value.Length);
                //}
                Data result = EMPTY_DATA;
                if (value != null && value.Length>0)
                {
                    result = new Data();
                    result.Binary = value;
                }
                this.message.BodySection = result;
                base.Content = result.Binary;
            }
        }

        public int BodyLength
        {
            get
            {
                
                return Content != null ? Content.Length : -1;
            }
        }

        public BinaryReader DataIn
        {
            get
            {
                if(byteOut != null)
                {
                    throw new IllegalStateException("Cannot read message while writing.");
                }
                if (byteIn == null)
                {
                    byte[] data = Content;
                    if (Content == null)
                    {
                        data = EMPTY_DATA.Binary;
                    }
                    Stream dataStream = new MemoryStream(data, false);
                    
                    byteIn = new EndianBinaryReader(dataStream);
                }
                return byteIn;
            }
        }

        public BinaryWriter DataOut
        {
            get
            {
                if (byteIn != null)
                {
                    throw new IllegalStateException("Cannot write message while reading.");
                }
                if (byteOut == null)
                {
                    MemoryStream outputBuffer = new MemoryStream();
                    this.byteOut = new EndianBinaryWriter(outputBuffer);
                    message.BodySection = EMPTY_DATA;

                }
                return byteOut;
            }
        }

        public void Reset()
        {
            if (byteOut != null)
            {

                MemoryStream byteStream = new MemoryStream((int)byteOut.BaseStream.Length);
                byteOut.BaseStream.Position = 0;
                byteOut.BaseStream.CopyTo(byteStream);
                
                byte[] value = byteStream.ToArray();
                Content = value;
                
                byteStream.Close();
                byteOut.Close();
                byteOut = null;
            }
            if (byteIn != null)
            {
                byteIn.Close();
                byteIn = null;
            }
        }

        IBytesMessageCloak IBytesMessageCloak.Copy()
        {
            IBytesMessageCloak bcloak = new AMQPBytesMessageCloak(connection);
            this.CopyInto(bcloak);
            return bcloak;
        }

        public override void ClearBody()
        {
            this.Reset();
            Content = null;
        }

        protected override void CopyInto(IMessageCloak msg)
        {
            base.CopyInto(msg);
            this.Reset();
            IBytesMessageCloak bmsg = msg as IBytesMessageCloak;
            bmsg.Content = this.Content;
            
            
        }

        private Data GetBinaryFromBody()
        {
            RestrictedDescribed body = message.BodySection;
            Data result = EMPTY_DATA;
            if(body == null)
            {
                return result;
            }
            else if (body is Data)
            {
                byte[] binary = (body as Data).Binary;
                if(binary != null && binary.Length != 0)
                {
                    return body as Data;
                }
            }
            else if (body is AmqpValue)
            {
                object value = (body as AmqpValue).Value;
                if(value == null) { return result; }
                if(value is byte[])
                {
                    byte[] dataValue = value as byte[];
                    if (dataValue.Length > 0)
                    {
                        result = new Data();
                        result.Binary = dataValue;
                    }
                }
                else
                {
                    throw new IllegalStateException("Unexpected Amqp value content-type: " + value.GetType().FullName);
                }
            }
            else
            {
                throw new IllegalStateException("Unexpected body content-type: " + body.GetType().FullName);
            }

            return result;
        }
        public override string ToString()
        {
            string result = base.ToString();
            if (this.Content != null)
            {
                result += string.Format("\nMessage Body: {0}\n", this.Content.ToString());
            }
            return result;
        }
    }
}
