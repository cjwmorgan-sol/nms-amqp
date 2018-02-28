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

                if (dest.IsQueue)
                {
                    return (qPrefix ?? "") + (dest as IQueue).QueueName;
                }
                else
                {
                    return (tPrefix ?? "") + (dest as ITopic).TopicName;
                }
            }
            else
            {
                //throw new InvalidDestinationException("Destination can not be null.");
                return null;
            }
        }
    }
}
