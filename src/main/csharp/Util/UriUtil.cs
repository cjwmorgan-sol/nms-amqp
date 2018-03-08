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

        public static string GetDestinationName(string address, Connection conn)
        {
            if(address!=null && address.Length > 0)
            {
                string destinationName = address;
                if( conn.TopicPrefix!=null && conn.TopicPrefix.Length>0 
                    && address.StartsWith(conn.TopicPrefix))
                {
                    destinationName = address.Substring(conn.TopicPrefix.Length);
                    return destinationName;
                }

                if (conn.QueuePrefix != null && conn.QueuePrefix.Length > 0
                    && address.StartsWith(conn.QueuePrefix))
                {
                    destinationName = address.Substring(conn.QueuePrefix.Length);
                }
                return destinationName;
            }
            else
            {
                return null;
            }
        }

        public static string GetAddress(IDestination dest, Connection conn)
        {
            
            if (dest != null)
            {
                string qPrefix = null;
                string tPrefix = null;
                if (!dest.IsTemporary)
                {
                    qPrefix = conn.QueuePrefix;
                    tPrefix = conn.TopicPrefix;
                }

                string destinationName = null;
                string prefix = null;
                if (dest.IsQueue)
                {
                    destinationName = (dest as IQueue).QueueName;
                    prefix = qPrefix ?? string.Empty;
                }
                else
                {
                    destinationName = (dest as ITopic).TopicName;
                    prefix = tPrefix ?? string.Empty;
                }

                if (!destinationName.StartsWith(prefix))
                {
                    destinationName = prefix + destinationName;
                }
                return destinationName;
            }
            else
            {
                return null;
            }
        }
    }
}
