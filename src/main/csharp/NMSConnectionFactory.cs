using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;


namespace NMS.AMQP
{
    public class NMSConnectionFactory : Apache.NMS.NMSConnectionFactory
    {
        static NMSConnectionFactory()
        {
            ProviderFactoryInfo amqp_1_0_ProviderInfo = new ProviderFactoryInfo("NMS.AMQP", "NMS.AMQP.ConnectionFactory");
            schemaProviderFactoryMap["amqp"] = amqp_1_0_ProviderInfo; // overwrite apache AMQP provider
            schemaProviderFactoryMap["amqp1.0"] = amqp_1_0_ProviderInfo; // add specific amqp1.0 scheme
        }

        public NMSConnectionFactory(string providerURI, params object[] constructorParams) : base(providerURI, constructorParams){}
        public NMSConnectionFactory(Uri uriProvider, params object[] constructorParams) : base(uriProvider, constructorParams) {}
    }
}
