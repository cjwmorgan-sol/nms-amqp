using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Apache.NMS;
using Amqp.Framing;
using Amqp;

namespace NMS.AMQP.Util
{
    class ExceptionSupport
    {

        private static readonly Dictionary<string, NMSException> errMap;
        private static readonly Dictionary<string, Type> errTypeMap;

        static ExceptionSupport()
        {
            errMap = new Dictionary<string, NMSException>();
            errMap.Add(ErrorCode.ConnectionRedirect, new NMSConnectionException("Connection Disconnected Unexpectedly.", ErrorCode.ConnectionRedirect));
            errMap.Add(ErrorCode.ConnectionForced, new NMSConnectionException("Connection has been Disconnected.", ErrorCode.ConnectionForced));
            errMap.Add(ErrorCode.IllegalState, new IllegalStateException("Amqp Object is in an Illegal State.", ErrorCode.IllegalState));

            // mapping of amqp .Net Lite error code to NMS exception type

            errTypeMap = new Dictionary<string, Type>();
            errTypeMap.Add(ErrorCode.ConnectionRedirect, typeof(NMSConnectionException));
            errTypeMap.Add(ErrorCode.ConnectionForced, typeof(NMSConnectionException));
            errTypeMap.Add(ErrorCode.IllegalState, typeof(IllegalStateException));


            errTypeMap.Add(ErrorCode.DecodeError, typeof(NMSException));
            errTypeMap.Add(ErrorCode.DetachForced, typeof(NMSException));
            errTypeMap.Add(ErrorCode.ErrantLink, typeof(NMSException));
            errTypeMap.Add(ErrorCode.FrameSizeTooSmall, typeof(NMSConnectionException));
            errTypeMap.Add(ErrorCode.FramingError, typeof(NMSConnectionException));
            errTypeMap.Add(ErrorCode.HandleInUse, typeof(NMSException));
            errTypeMap.Add(ErrorCode.InternalError, typeof(NMSException));
            errTypeMap.Add(ErrorCode.InvalidField, typeof(NMSException));
            errTypeMap.Add(ErrorCode.LinkRedirect, typeof(NMSException));
            errTypeMap.Add(ErrorCode.MessageReleased, typeof(IllegalStateException));
            errTypeMap.Add(ErrorCode.MessageSizeExceeded, typeof(NMSException));
            errTypeMap.Add(ErrorCode.NotAllowed, typeof(NMSException));
            errTypeMap.Add(ErrorCode.NotFound, typeof(InvalidDestinationException));
            errTypeMap.Add(ErrorCode.NotImplemented, typeof(NMSException));
            errTypeMap.Add(ErrorCode.PreconditionFailed, typeof(NMSException));
            errTypeMap.Add(ErrorCode.ResourceDeleted, typeof(NMSException));
            errTypeMap.Add(ErrorCode.ResourceLimitExceeded, typeof(NMSException));
            errTypeMap.Add(ErrorCode.ResourceLocked, typeof(NMSException));
            errTypeMap.Add(ErrorCode.Stolen, typeof(NMSException));
            errTypeMap.Add(ErrorCode.TransactionRollback, typeof(TransactionRolledBackException));
            errTypeMap.Add(ErrorCode.TransactionTimeout, typeof(TransactionInProgressException));
            errTypeMap.Add(ErrorCode.TransactionUnknownId, typeof(TransactionRolledBackException));
            errTypeMap.Add(ErrorCode.TransferLimitExceeded, typeof(NMSException));
            errTypeMap.Add(ErrorCode.UnattachedHandle, typeof(NMSException));
            errTypeMap.Add(ErrorCode.UnauthorizedAccess, typeof(NMSSecurityException));
            errTypeMap.Add(ErrorCode.WindowViolation, typeof(NMSException));
            
        }

        private static FieldInfo[] GetConstants(Type type)
        {
            ArrayList list = new ArrayList();
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Static | 
                BindingFlags.Public | 
                BindingFlags.FlattenHierarchy
                );
            foreach (FieldInfo field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly)
                    list.Add(field);
            }
            return (FieldInfo[])list.ToArray(typeof(FieldInfo));
        }

        private static string[] GetStringConstants(Type type)
        {
            FieldInfo[] fields = GetConstants(type);
            ArrayList list = new ArrayList(fields.Length);
            foreach(FieldInfo fi in fields)
            {
                if (fi.FieldType.Equals(typeof(string)))
                {
                    list.Add(fi.GetValue(null));
                }
            }
            return (string[])list.ToArray(typeof(string));
        }

        public static NMSException GetException(AmqpObject obj, string format, params object[] args)
        {
            return GetException(obj, string.Format(format, args));
        }

        public static NMSException GetException(Error amqpErr, string format, params object[] args)
        {
            return GetException(amqpErr, string.Format(format, args));
        }

        public static NMSException GetException(AmqpObject obj, string message="")
        {
            return GetException(obj.Error, message);
        }

        public static NMSException GetException(Error amqpErr, string message="")
        {
            string errCode = amqpErr.Condition.ToString();
            string errMessage = amqpErr.Description;
            NMSException ex = null;
            Type exType = null;
            if(errTypeMap.TryGetValue(errCode, out exType))
            {
                ConstructorInfo ci = exType.GetConstructor(new[] { typeof(string), typeof(string) });
                object inst = ci.Invoke(new object[] { message + " " + errMessage, errCode });
                ex = inst as NMSException;
            }
            return ex;
            
        }

        public static NMSException Wrap(Exception e, string format, params object[] args)
        {
            return Wrap(e, string.Format(format, args));
        }

        public static NMSException Wrap(Exception e, string message = "")
        {
            if(e == null)
            {
                return null;
            }
            return new NMSException(message, ErrorCode.InternalError, e);
        }

    }
}
