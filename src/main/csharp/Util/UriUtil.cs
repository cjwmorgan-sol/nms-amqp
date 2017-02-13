using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amqp;
using Apache.NMS;

namespace NMS.AMQP.Util
{
    class UriUtil
    {
        public static Address ToAddress(Uri uri, string username = null, string password = null)
        {
            Address addr = new Address(uri.Host, uri.Port, username, password, "/", uri.Scheme);
            return addr;
        }

        public static Uri ToUri(Address addr)
        {
            return null;
        }
        
    }
}
