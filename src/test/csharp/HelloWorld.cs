using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMS.AMQP;
using Apache.NMS;

namespace HelloWorld
{
    class Program
    {
        private static string host = "localhost";
        private static long connTimeout = 15000;
        private static string clientId = null;
        private static string username = null;
        private static string password = null;
        private static Logger.LogLevel loglevel = Logger.LogLevel.INFO;
        private static bool amqpTrace = false;

        #region Arguments

        private static void printUsage()
        {
            Console.WriteLine("HelloWorld (-ip hostIp | -ip=hostIp) [-ct conTimeout | -ct=connTimeout]");
            Environment.Exit(0);
        }

        private static bool parseFlag(string arg, string symbol)
        {
            string token = "-" + symbol;
            string eqToken = token + "=";
            if (arg.Equals(token, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }
            if (arg.StartsWith(eqToken, StringComparison.CurrentCultureIgnoreCase))
            {
                
                return true;
            }
            return false;
        }

        private static bool parseToken(string arg, string symbol, out string value )
        {
            value = null;
            string token = "-" + symbol;
            string eqToken = token + "=";
            if(arg.Equals(token, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }
            if(arg.StartsWith(eqToken, StringComparison.CurrentCultureIgnoreCase))
            {
                value = arg.Substring(eqToken.Length);
                return true;
            }
            return false;
        }

        private static void parseParams(string[] args)
        {
            if(args.Length < 1)
            {
                printUsage();
            }
            bool hasIp = false;
            for (int i = 0; i < args.Length; i++)
            {
                string token = args[i];
                string value = null;
                if (parseToken(token, "ip", out value))
                {
                    if(value == null)
                    {
                        i++;
                        host = args[i];
                    }
                    else
                    {
                        host = value;
                    }
                    hasIp = true;
                }
                else if(parseToken(token, "ct", out value))
                {
                    if (value == null)
                    {
                        i++;
                        connTimeout = Convert.ToInt64(args[i]);
                    }
                    else
                    {
                        connTimeout = Convert.ToInt64(value);
                    }
                }
                else if (parseToken(token, "cid", out value))
                {
                    if (value == null)
                    {
                        i++;
                        clientId = args[i];
                    }
                    else
                    {
                        clientId = value;
                    }
                }
                else if (parseToken(token, "cu", out value))
                {
                    if (value == null)
                    {
                        i++;
                        username = args[i];
                    }
                    else
                    {
                        username = value;
                    }
                }
                else if (parseToken(token, "cpwd", out value))
                {
                    if (value == null)
                    {
                        i++;
                        password = args[i];
                    }
                    else
                    {
                        password = value;
                    }
                }
                else if (parseToken(token, "log", out value))
                {
                    string logString = "";
                    if (value == null)
                    {
                        i++;
                        logString = args[i];
                    }
                    else
                    {
                        logString = value;
                    }
                    loglevel = Logger.ToLogLevel(logString);
                }
                else if (parseFlag(token, "d"))
                {
                    amqpTrace = true;
                }
                else
                {
                    printUsage();
                }
            }
            if (!hasIp)
            {
                printUsage();
            }
        }
#endregion

        static void Main(string[] args)
        {
            parseParams(args);

            //NMSConnectionFactory.CreateConnectionFactory()
            // "amqp:tcp://localhost:5672"
            ITrace logger = new Logger(loglevel, amqpTrace);
            Tracer.Trace = logger;
            
            //string ip = "amqp://192.168.2.69:5672";
            string ip = "amqp://" + host + ":5672";
            Uri providerUri = new Uri(ip);
            Console.WriteLine("scheme: {0}", providerUri.Scheme);

            StringDictionary properties = new StringDictionary();
            //properties["clientId"] = "guest";
            if(username !=null)
                properties["NMS.username"] = username;
            if(password !=null)
                properties["nms.password"] = password;
            if (clientId != null)
                properties["NMS.CLIENTID"] = clientId;
            //properties["nms.clientid"] = "myclientid1";
            properties["NMS.sendtimeout"] = connTimeout+"";

            NMS.AMQP.NMSConnectionFactory providerFactory = new NMS.AMQP.NMSConnectionFactory(providerUri, properties);
            //Apache.NMS.NMSConnectionFactory providerFactory = new Apache.NMS.NMSConnectionFactory(providerUri, properties);
            IConnectionFactory factory = providerFactory.ConnectionFactory;
            //(factory as NMS.AMQP.ConnectionFactory).ConnectionProperties = properties;
            
            Console.WriteLine("Creating Connection...");
            IConnection conn = factory.CreateConnection();
            conn.ExceptionListener += (logger as Logger).LogException;
            //conn.ClientId = "myclientid1";
            Console.WriteLine("Created Connection.");
            Console.WriteLine("Version: {0}", conn.MetaData);

            ISession ses = conn.CreateSession();
            Console.WriteLine("First Starting Connection...");
            //IDestination dest = ses.CreateTemporaryQueue();
            IDestination dest = ses.GetQueue("jms.queue.RADU_CU");

            IMessageProducer prod = ses.CreateProducer(dest);
            prod.DeliveryMode = MsgDeliveryMode.NonPersistent;
            prod.TimeToLive = TimeSpan.FromSeconds(2.5);
            ITextMessage msg = prod.CreateTextMessage("Hello World!");

            conn.Start();


            prod.Send(msg);

            for (int i=0;i<2000;i++)
            {
                
                msg.Text = "Hello World! n:" + i;
                prod.Send(msg);
            }


            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            //int c = 0;
            //while (c < 50)
            //{
            //    Console.WriteLine("Stopping Connection...");

            //    conn.Stop();

            //    System.Threading.Thread.Sleep(50);

            //    Console.WriteLine("Starting Connection...");
            //    conn.Start();
            //    c++;
            //}
            //conn.Stop();
            //for (int i = 0; i<50; i++)
            //{
            //    conn.Start();
            //}
            //c = 1;
            //for (int i = 0; i < 50; i++)
            //{
            //    conn.Stop();
            //}
            //c = 2;
            //for (int i = 0; i < 50; i++)
            //{
            //    conn.Start();
            //}

            Console.WriteLine("Connection Started: {0} Resquest Timeout: {1}", conn.IsStarted, conn.RequestTimeout);
            int count = 0;
            while ( count++ < connTimeout/500)
            {
                System.Threading.Thread.Sleep(500);
            }
            
            //if (conn.IsStarted)
            //{
                //Console.WriteLine("Closing Connection...");
                //conn.Close();
                //Console.WriteLine("Connection Closed.");
            //}
            conn.Dispose();

        }
    }

    #region Logging

    class Logger : ITrace
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
            if(logString == null || logString.Length == 0)
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

        private LogLevel lv;

        public static void TraceListener(string format, params object[] args)
        {
            Console.WriteLine(("Internal Trace: "+format), args);
        }

        public void LogException(Exception ex)
        {
            this.Warn("Exception: "+ex.Message);
        }

        public Logger() : this(LogLevel.WARN)
        {
        }

        private Amqp.TraceLevel from(LogLevel lvl)
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

        public Logger(LogLevel lvl, bool traceInternal = false)
        {
            lv = lvl;
            Amqp.TraceLevel frameTrace = (this.IsInfoEnabled) ? Amqp.TraceLevel.Frame : 0;
            Amqp.Trace.TraceLevel = !traceInternal ? 0 : frameTrace | from(lv);
            Amqp.Trace.TraceListener = Logger.TraceListener;
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
            if(IsDebugEnabled)
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
    #endregion 
}
