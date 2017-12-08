using System;
using Apache.NMS;
using Amqp;

namespace NMS.AMQP.Test.Util
{

    class NMSLogger : ITrace
    {
        public enum LogLevel
        {
            OFF = -1,
            FATAL,
            ERROR,
            WARN,
            INFO,
            DEBUG
        }

        public static LogLevel ToLogLevel(string logString)
        {
            if (logString == null || logString.Length == 0)
            {
                return LogLevel.OFF;
            }
            if ("FATAL".StartsWith(logString, StringComparison.CurrentCultureIgnoreCase))
            {
                return LogLevel.FATAL;
            }
            else if ("ERROR".StartsWith(logString, StringComparison.CurrentCultureIgnoreCase))
            {
                return LogLevel.ERROR;
            }
            else if ("WARN".StartsWith(logString, StringComparison.CurrentCultureIgnoreCase))
            {
                return LogLevel.WARN;
            }
            else if ("INFO".StartsWith(logString, StringComparison.CurrentCultureIgnoreCase))
            {
                return LogLevel.INFO;
            }
            else if ("DEBUG".StartsWith(logString, StringComparison.CurrentCultureIgnoreCase))
            {
                return LogLevel.DEBUG;
            }
            else
            {
                return LogLevel.OFF;
            }
        }

        public static LogLevel ToLogLevel(TraceLevel tracelvl)
        {
            LogLevel llvl = LogLevel.WARN;
            switch (tracelvl)
            {
                case TraceLevel.Error:
                    llvl = LogLevel.ERROR;
                    break;
                case TraceLevel.Warning:
                    llvl = LogLevel.WARN;
                    break;
                case TraceLevel.Frame:
                case TraceLevel.Information:
                    llvl = LogLevel.INFO;
                    break;
                case TraceLevel.Output:
                case TraceLevel.Verbose:
                    llvl = LogLevel.DEBUG;
                    break;
                default:
                    break;
            }
            return llvl;
        }

        private LogLevel lv;

        public static void TraceListener(TraceLevel lvl, string format, params object[] args)
        {
            string str = string.Format("Internal Trace (level={0}) : {1}", lvl.ToString(), format);
            Console.WriteLine(str, args);
        }

        public static void TraceListener(string format, params object[] args)
        {
            string str = string.Format("Internal Trace : {0}", format);
            Console.WriteLine(str, args);
        }

        public void LogException(Exception ex)
        {
            this.Warn("Exception: " + ex.Message);
        }

        public NMSLogger() : this(LogLevel.WARN)
        {
        }

        private Amqp.TraceLevel From(LogLevel lvl)
        {
            switch (lvl)
            {
                case LogLevel.DEBUG:
                    return Amqp.TraceLevel.Verbose;
                case LogLevel.INFO:
                    return Amqp.TraceLevel.Information;
                case LogLevel.WARN:
                    return Amqp.TraceLevel.Warning;
                case LogLevel.ERROR:
                case LogLevel.FATAL:
                    return Amqp.TraceLevel.Error;
                case LogLevel.OFF:
                default:
                    return 0;

            }
        }

        public NMSLogger(LogLevel lvl, bool traceInternal = false)
        {
            lv = lvl;
            Amqp.TraceLevel frameTrace = (this.IsInfoEnabled) ? Amqp.TraceLevel.Frame : 0;
            Amqp.Trace.TraceLevel = !traceInternal ? 0 : frameTrace | From(lv);
            Amqp.Trace.TraceListener = NMSLogger.TraceListener;
        }

        public bool IsDebugEnabled
        {
            get
            {
                return lv >= LogLevel.DEBUG;
            }
        }

        public bool IsErrorEnabled
        {
            get
            {

                return lv >= LogLevel.ERROR;
            }
        }

        public bool IsFatalEnabled
        {
            get
            {
                return lv >= LogLevel.FATAL;
            }
        }

        public bool IsInfoEnabled
        {
            get
            {
                return lv >= LogLevel.INFO;
            }
        }

        public bool IsWarnEnabled
        {
            get
            {
                return lv >= LogLevel.WARN;
            }
        }

        public void Debug(string message)
        {
            if (IsDebugEnabled)
                Console.WriteLine("Debug: {0}", message);
        }

        public void Error(string message)
        {
            if (IsErrorEnabled)
                Console.WriteLine("Error: {0}", message);
        }

        public void Fatal(string message)
        {
            if (IsFatalEnabled)
                Console.WriteLine("Fatal: {0}", message);
        }

        public void Info(string message)
        {
            if (IsInfoEnabled)
                Console.WriteLine("Info: {0}", message);
        }

        public void Warn(string message)
        {
            if (IsWarnEnabled)
                Console.WriteLine("Warn: {0}", message);
        }
    }
}