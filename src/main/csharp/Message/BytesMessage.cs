using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Apache.NMS;
using Apache.NMS.Util;

namespace NMS.AMQP.Message
{
    using Cloak;
    /// <summary>
    /// NMS.AMQP.Message.BytesMessage inherits from NMS.AMQP.Message.Message that implements the Apache.NMS.IBytesMessage interface.
    /// NMS.AMQP.Message.BytesMessage uses the NMS.AMQP.Message.Cloak.IBytesMessageCloak interface to detach from the underlying AMQP 1.0 engine.
    /// </summary>
    class BytesMessage : Message, IBytesMessage
    {
        private BinaryWriter dataOut = null;
        private BinaryReader dataIn = null;
        private MemoryStream outputBuffer = null;
        private int length = 0;
        private readonly new IBytesMessageCloak cloak;

        #region Constructor

        internal BytesMessage(IBytesMessageCloak message) : base(message)
        {
            cloak = message;
        }

        #endregion

        #region Private Methods

        private void InitializeReadingMode()
        {
            FailIfWriteOnlyMsgBody();
            if(dataIn == null)
            {
                dataIn = cloak.DataIn;
            }
        }

        private void InitializeWritingMode()
        {
            FailIfReadOnlyMsgBody();
            if(dataOut == null)
            {
                this.dataOut = cloak.DataOut;
            }
        }

        private void StoreContent()
        {
            if(dataOut != null)
            {
                dataOut.Close();
                base.Content = outputBuffer.ToArray();

                dataOut = null;
                outputBuffer = null;
            }
        }

        #endregion

        #region IBytesMessage Properties

        public new byte[] Content
        {
            get
            {
                byte[] buffer = null;
                InitializeReadingMode();
                if(this.length != 0)
                {
                    buffer = new byte[this.length];
                    dataIn.Read(buffer, 0, buffer.Length);
                }
                return buffer;
            }
            set
            {
                InitializeWritingMode();
                if(value != null)
                {
                    this.dataOut.Write(value, 0, value.Length);
                }    
            }
        }

        public long BodyLength
        {
            get
            {
                InitializeReadingMode();
                return length;
            }
        }

        #endregion

        #region IBytesMessage Methods

        public void Reset()
        {
            this.cloak.Reset();
            dataIn = null;
            dataOut = null;
            outputBuffer = null;
            IsReadOnly = true;
        }

        public bool ReadBoolean()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadBoolean();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public byte ReadByte()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadByte();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public int ReadBytes(byte[] value)
        {
            InitializeReadingMode();
            try
            {
                return dataIn.Read(value, 0, value.Length);
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public int ReadBytes(byte[] value, int length)
        {
            InitializeReadingMode();
            try
            {
                return dataIn.Read(value, 0, length);
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public char ReadChar()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadChar();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public double ReadDouble()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadDouble();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public short ReadInt16()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadInt16();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public int ReadInt32()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadInt32();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public long ReadInt64()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadInt64();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public float ReadSingle()
        {
            InitializeReadingMode();
            try
            {
                return dataIn.ReadSingle();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }

        public string ReadString()
        {
            InitializeReadingMode();
            try
            {
                // Note if dataIn is an EndianBinaryReader the string length is read as 16bit short
                return dataIn.ReadString();
            }
            catch (EndOfStreamException e)
            {
                throw NMSExceptionSupport.CreateMessageEOFException(e);
            }
            catch (IOException e)
            {
                throw NMSExceptionSupport.CreateMessageFormatException(e);
            }
        }
        
        public void WriteBoolean(bool value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteByte(byte value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch(Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }

        }

        public void WriteBytes(byte[] value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value, 0, value.Length);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteBytes(byte[] value, int offset, int length)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value, offset, length);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteChar(char value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteDouble(double value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteInt16(short value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteInt32(int value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteInt64(long value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteObject(object value)
        {
            InitializeWritingMode();
            
            Type objType = value.GetType();
            if(value is byte[])
            {
                dataOut.Write((byte[])value);
            }
            else if (objType.IsPrimitive)
            {
                if(value is Byte)
                {
                    dataOut.Write((byte)value);
                }
                else if (value is Char)
                {
                    dataOut.Write((char)value);
                }
                else if (value is Boolean)
                {
                    dataOut.Write((bool)value);
                }
                else if (value is Int16)
                {
                    dataOut.Write((short)value);
                }
                else if (value is Int32)
                {
                    dataOut.Write((int)value);
                }
                else if (value is Int64)
                {
                    dataOut.Write((long)value);
                }
                else if (value is Single)
                {
                    dataOut.Write((float)value);
                }
                else if (value is Double)
                {
                    dataOut.Write((double)value);
                }
                else if (value is String)
                {
                    dataOut.Write((string) value);
                }
                else
                {
                    throw new MessageFormatException("Cannot write primitive type:" + objType);
                }
            }
            else
            {
                throw new MessageFormatException("Cannot write non-primitive type:" + objType);
            }
            
        }

        public void WriteSingle(float value)
        {
            InitializeWritingMode();
            try
            {
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        public void WriteString(string value)
        {
            InitializeWritingMode();
            try
            {
                // note if dataOut is an EndianBinaryWriter, strings are written with a 16bit short length.
                dataOut.Write(value);
            }
            catch (Exception e)
            {
                throw NMSExceptionSupport.Create(e);
            }
        }

        #endregion
    }
}
