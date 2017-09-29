using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amqp;
using Apache.NMS;

namespace NMS.AMQP.Util
{
    /// <summary>
    /// Used to convert between System.Uri and Amqp.Address.
    /// </summary>
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

        public static string GetAddress(IDestination dest)
        {
            if (dest != null)
            {
                if (dest.IsQueue)
                {
                    return (dest as IQueue).QueueName;
                }
                else
                {
                    return (dest as ITopic).TopicName;
                }
            }
            else
            {
                throw new InvalidDestinationException("Destination can not be null.");
            }
        }
    }
}
