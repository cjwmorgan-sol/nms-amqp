using System;
using System.Net.Security;

namespace NMS.AMQP.Transport.Secure
{
    internal interface IProviderSecureTransportContext : IProviderTransportContext
    {
        
        new IProviderSecureTransportContext Copy();

        string ServerName { get; set; }

        string ClientCertFileName { get; set; }

        string ClientCertSubject { get; set; }

        string ClientCertPassword { get; set; }

        string KeyStoreName { get; set; }
        
        string KeyStoreLocation { get; set; }

        bool AcceptInvalidBrokerCert { get; set; }

        string SSLProtocol { get; set; }

        string SSLExcludeProtocols { get; set; }

        RemoteCertificateValidationCallback ServerCertificateValidateCallback { get; set; }

        LocalCertificateSelectionCallback ClientCertificateSelectCallback { get; set; }

        bool CheckCertificateRevocation { get; set; }
        
    }
}