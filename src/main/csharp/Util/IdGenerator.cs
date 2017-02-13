using System;
using System.Net;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS.Util;
using Apache.NMS;

namespace NMS.AMQP.Util
{
    class IdGenerator
    {
        private static readonly string DEFAULT_PREFIX = "ID:"; 
        private static string hostname=null;

        private readonly string prefix;
        private readonly AtomicSequence sequence = new AtomicSequence(1);

        static IdGenerator()
        {
            DnsPermission permissions = null;
            try
            {
                permissions = new DnsPermission(PermissionState.Unrestricted);
            }
            catch (Exception e)
            {
                Tracer.InfoFormat("{0}", e.StackTrace);
            }
            if (permissions != null)
            {
                hostname = Dns.GetHostName();
            }
            
        }

        public IdGenerator (string prefix)
        {
            this.prefix = prefix + ((hostname == null) ? "" : hostname + ":") ;
        }

        public IdGenerator () : this(DEFAULT_PREFIX)
        {
        }

        public string generateID()
        {
            return string.Format("{0}{1}:{2}", this.prefix, Guid.NewGuid().ToString(), sequence.getAndIncrement());
        }
    }
}
