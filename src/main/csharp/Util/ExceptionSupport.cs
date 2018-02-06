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
            errTypeMap.Add(NMSErrorCode.CONNECTION_TIME_OUT, typeof(NMSConnectionException));
            errTypeMap.Add(ErrorCode.ConnectionRedirect, typeof(NMSConnectionException));
            errTypeMap.Add(ErrorCode.ConnectionForced, typeof(NMSConnectionException));
            errTypeMap.Add(ErrorCode.IllegalState, typeof(IllegalStateException));

            errTypeMap.Add(NMSErrorCode.INTERNAL_ERROR, typeof(NMSException));
            errTypeMap.Add(NMSErrorCode.UNKNOWN_ERROR, typeof(NMSException));
            errTypeMap.Add(NMSErrorCode.SESSION_TIME_OUT, typeof(NMSException));
            errTypeMap.Add(NMSErrorCode.LINK_TIME_OUT, typeof(NMSException));
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

        public static NMSException GetTimeoutException(IAmqpObject obj, string format, params object[] args)
        {
            return GetTimeoutException(obj, string.Format(format, args));
        }

        public static NMSException GetTimeoutException(IAmqpObject obj, string message)
        {
            Error e = null;
            if (obj is Amqp.Connection)
            {
                e = NMSError.CONNECTION_TIMEOUT;
            }
            else if (obj is Amqp.Session)
            {
                e = NMSError.SESSION_TIMEOUT;
            }
            else if (obj is Amqp.Link)
            {
                e = NMSError.LINK_TIMEOUT;
            }
            
            return GetException(e, message);

        }

        public static NMSException GetException(IAmqpObject obj, string format, params object[] args)
        {
            return GetException(obj, string.Format(format, args));
        }

        public static NMSException GetException(Error amqpErr, string format, params object[] args)
        {
            return GetException(amqpErr, string.Format(format, args));
        }

        public static NMSException GetException(IAmqpObject obj, string message="")
        {
            return GetException(obj.Error, message);
        }

        public static NMSException GetException(Error amqpErr, string message="")
        {
            string errCode = null;
            string errMessage = null;
            string additionalErrInfo = null;
            if (amqpErr == null)
            {
                amqpErr = NMSError.INTERNAL;
            }
            
            errCode = amqpErr.Condition.ToString();
            errMessage = amqpErr.Description;

            errMessage = errMessage != null ? ", Description = " + errMessage : "";

            additionalErrInfo = Types.ConversionSupport.ToString(amqpErr.Info);
            
            additionalErrInfo = amqpErr.Info!=null && amqpErr.Info.Count > 0 ? ", ErrorInfo = " + additionalErrInfo : "";  

            NMSException ex = null;
            Type exType = null;
            if(errTypeMap.TryGetValue(errCode, out exType))
            {
                ConstructorInfo ci = exType.GetConstructor(new[] { typeof(string), typeof(string) });
                object inst = ci.Invoke(new object[] { message + errMessage + additionalErrInfo , errCode });
                ex = inst as NMSException;
            }
            else
            {
                ex = new NMSException(message + errMessage, errCode);
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
            NMSException nmsEx = null;
            string exMessage = message;
            if (exMessage == null || exMessage.Length == 0)
            {
                exMessage = e.Message;
            }
            if (e is NMSException)
            {
                nmsEx = new NMSAggregateException(exMessage, e as NMSException);
            }
            if (e is AggregateException)
            {
                Exception cause = (e as AggregateException).InnerException;
                if (cause != null)
                {
                    nmsEx = Wrap(cause, message);
                }
                else
                {
                    nmsEx = new NMSAggregateException(exMessage, NMSErrorCode.INTERNAL_ERROR, e);
                }
            }
            else if (e is AmqpException)
            {
                Error err = (e as AmqpException).Error;
                nmsEx = GetException(err, message);
                Tracer.DebugFormat("Encoutered AmqpException {0} and created NMS Exception {1}.", e, nmsEx);
            }
            else
            {
                nmsEx = new NMSAggregateException(exMessage, NMSErrorCode.INTERNAL_ERROR, e);
            }
            
            return nmsEx;
        }

    }

    #region Exceptions


    public class InvalidPropertyException : NMSException
    {
        protected static string ExFormat = "Invalid Property {0}. Cause: {1}";

        public InvalidPropertyException(string property, string message) : base(string.Format(ExFormat, property, message))
        {
            exceptionErrorCode = NMSErrorCode.PROPERTY_ERROR;
        }
    }


    internal class NMSAggregateException : NMSException
    {
        private string InstanceTrace;

        public NMSAggregateException() : base()
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(1, true);
            InstanceTrace = trace.ToString();
        }

        public NMSAggregateException(string message) : base(message)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(1, true);
            InstanceTrace = trace.ToString();
        }

        public NMSAggregateException(string message, string errorCode) : base(message, errorCode)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(1, true);
            InstanceTrace = trace.ToString();
        }

        public NMSAggregateException(string message, NMSException innerException) : base(message, innerException)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(1, true);
            InstanceTrace = trace.ToString();
            exceptionErrorCode = innerException.ErrorCode ?? NMSErrorCode.INTERNAL_ERROR;
        }

        public NMSAggregateException(string message, Exception innerException) : base (message, innerException)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(1, true);
            InstanceTrace = trace.ToString();
        }

        public NMSAggregateException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(1, true);
            InstanceTrace = trace.ToString();
        }

        public override string StackTrace
        {
            get
            {
                string stack = base.StackTrace;
                if ( stack==null || stack.Length == 0)
                {
                    stack = InstanceTrace;
                }
                if ( InnerException != null && (InnerException.StackTrace != null && InnerException.StackTrace.Length>0))
                { 
                    stack += "\nCause " + InnerException.Message + " : \n" +
                                 InnerException.StackTrace;
                    
                }
                return stack;
            }
        }
    }

    #endregion

    #region Error Codes

    internal static class NMSError 
    {
        public static Error SESSION_TIMEOUT = new Error() { Condition = NMSErrorCode.SESSION_TIME_OUT, Description = "Session Begin Request has timed out." };
        public static Error CONNECTION_TIMEOUT = new Error() { Condition = NMSErrorCode.SESSION_TIME_OUT, Description = "Connection Open Request has timed out." };
        public static Error LINK_TIMEOUT = new Error() { Condition = NMSErrorCode.SESSION_TIME_OUT, Description = "Link Target Request has timed out." };
        public static Error PROPERTY = new Error() { Condition = NMSErrorCode.PROPERTY_ERROR, Description = "Property Error." };
        public static Error UNKNOWN = new Error() { Condition = NMSErrorCode.UNKNOWN_ERROR, Description = "Unknown Error." };
        public static Error INTERNAL = new Error() { Condition = NMSErrorCode.INTERNAL_ERROR, Description = "Internal Error." };

    }
    internal static class NMSErrorCode
    {
        public static string CONNECTION_TIME_OUT = "nms:connection:timout";
        public static string SESSION_TIME_OUT = "nms:session:timout";
        public static string LINK_TIME_OUT = "nms:link:timeout";
        public static string PROPERTY_ERROR = "nms:property:error";
        public static string UNKNOWN_ERROR = "nms:unknown";
        public static string INTERNAL_ERROR = "nms:internal";
    }

    #endregion
}
