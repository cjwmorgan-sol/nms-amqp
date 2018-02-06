using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

using Apache.NMS;

using NMS.AMQP.Util;
using NMS.AMQP.Transport.AMQP;
using System.Collections.Specialized;

namespace NMS.AMQP.Transport.Secure.AMQP
{

    /// <summary>
    /// Secure Transport management is mainly handled by the AmqpNetLite library, Except for certicate selection and valibdation.
    /// SecureTransportContext should configure the Amqp.ConnectionFactory for the ssl transport properties.
    /// </summary>
    internal class SecureTransportContext : TransportContext, IProviderSecureTransportContext
    {


        #region Ignore Case Comparer
        private class IgnoreCaseComparer : IEqualityComparer
        {
            public new bool Equals(object x, object y)
            {
                if (x == null || y == null)
                {
                    return x == null && y == null;
                }
                else if (!(x is string) || !(y is string))
                {
                    return false;
                }
                else
                {
                    string a = x as string;
                    string b = y as string;
                    return a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
                }

            }

            public int GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }

        #endregion

        private readonly static IgnoreCaseComparer ComparerInstance = new IgnoreCaseComparer();
        private readonly static List<string> SupportedProtocols;
        private readonly static Dictionary<string, int> SupportedProtocolValues;

        #region static Initializer

        static SecureTransportContext()
        {
            const string Default = "Default";
            const string None = "None";
            SupportedProtocols = new List<string>();
            SupportedProtocolValues = new Dictionary<string, int>();
            foreach (string name in Enum.GetNames(typeof(System.Security.Authentication.SslProtocols)))
            {
                if (name.Equals(Default, StringComparison.CurrentCultureIgnoreCase) ||
                   name.Equals(None, StringComparison.CurrentCultureIgnoreCase))
                {
                    // ignore
                }
                else if(!SystemSSLProtocolHelper.IsProtocolEnabled(name))
                {
                    // ignore
                    if (Tracer.IsDebugEnabled)
                        Tracer.DebugFormat("Protocol {0} is not enabled.", name);
                }
                else
                {
                    SupportedProtocols.Add(name);
                }

            }
            foreach (int value in Enum.GetValues(typeof(System.Security.Authentication.SslProtocols)))
            {
                SslProtocols p = (System.Security.Authentication.SslProtocols)value;
                if (p.Equals(SslProtocols.Default) ||
                   p.Equals(SslProtocols.None))
                {
                    // ignore
                }
                else if (!SystemSSLProtocolHelper.IsProtocolEnabled(p.ToString()))
                {
                    // ignore
                    if (Tracer.IsDebugEnabled)
                        Tracer.DebugFormat("Protocol {0} is not enabled.", p.ToString());
                }
                else
                {
                    string name = ((SslProtocols)value).ToString().ToLower();
                    SupportedProtocolValues.Add(name, value);
                }
            }
            if (Tracer.IsDebugEnabled)
                Tracer.DebugFormat("Supported SSL protocols list {0}", Util.PropertyUtil.ToString(SupportedProtocols));
        }

        #endregion

        #region SSL Protocol helper functions

        private static IList<string> ReadProtocolFlags(SslProtocols protocols)
        {
            List<string> result = new List<string>();
            if (protocols == SslProtocols.None)
            {
                return result;
            }

            foreach (string protocol in SupportedProtocols)
            {
                int value = SupportedProtocolValues[protocol];
                int mask = value & ((int)protocols);
                if (mask != value)
                {
                    result.Add(protocol);
                }
            }
            return result;
        }

        private static SslProtocols CreateProtocolFlags(IList<string> supportedProtocols, IList<string> excludedProtocols)
        {
            SslProtocols sslProtocols = SslProtocols.None;
            foreach (string protocol in supportedProtocols)
            {
                if (excludedProtocols.Count == 0 ||!ListContainsIgnoreCase(excludedProtocols, protocol))
                {
                    int value = 0;
                    if(SupportedProtocolValues.TryGetValue(protocol.ToLower(), out value))
                    {
                        sslProtocols |= (SslProtocols)value;
                    }
                    
                }
            }

            return sslProtocols;
        }

        private static bool ListContainsIgnoreCase(IList<string> list, string value)
        {
            return ListContains(list, value, ComparerInstance);
        }

        private static bool ListContains(IList<string> list, string value, IEqualityComparer comparer)
        {
            bool contains = false;
            foreach (string item in list)
            {
                if (comparer.Equals(value, item))
                {
                    contains = true;
                    break;
                }
            }
            return contains;
        }

        #endregion

        #region Constructors

        internal SecureTransportContext(NMS.AMQP.ConnectionFactory factory) : base(factory)
        {
            this.connectionBuilder.SSL.LocalCertificateSelectionCallback = this.ContextLocalCertificateSelect;
            this.connectionBuilder.SSL.RemoteCertificateValidationCallback = this.ContextServerCertificateValidation;
            connectionBuilder.SASL.Profile = Amqp.Sasl.SaslProfile.External;
        }

        // Copy Contructor
        protected SecureTransportContext() : base() { }

        #endregion

        #region Secure Transport Context Properties

        public string KeyStoreName { get; set; }

        public string KeyStorePassword { get; set; }

        public string ClientCertFileName { get; set; }

        public bool AcceptInvalidBrokerCert { get; set; } = false;
        
        public string ClientCertSubject { get; set; }
        public string ClientCertPassword { get; set; }
        public string KeyStoreLocation { get; set; }
        public string SSLExcludeProtocols { get; set; } = null;

        public string SSLProtocol { get; set; }

        public bool CheckCertificateRevocation
        {
            get
            {
                return this.connectionBuilder?.SSL.CheckCertificateRevocation ?? false;
            }
            set
            {
                if(this.connectionBuilder != null)
                    this.connectionBuilder.SSL.CheckCertificateRevocation = value;
            }
        }


        public string ServerName { get; set; }

        public RemoteCertificateValidationCallback ServerCertificateValidateCallback { get; set; }
        public LocalCertificateSelectionCallback ClientCertificateSelectCallback { get; set; }

        #endregion

        #region Private Methods
        // These are the default values given by amqpnetlite.
        private static readonly SslProtocols DefaultProtocols = (new Amqp.ConnectionFactory()).SSL.Protocols;


        private SslProtocols GetSslProtocols()
        {
            
            // create list of protocols to exclude droping unrecognized protocols.
            List<string> protocolsToExclude = new List<string>();
            if (this.SSLExcludeProtocols != null)
            {


                string[] protocols = this.SSLExcludeProtocols.Split(',');
                
                foreach (string protocol in protocols)
                {
                    if (ListContainsIgnoreCase(SupportedProtocols, protocol))
                    {
                        protocolsToExclude.Add(protocol);
                    }
                    else
                    {
                        Tracer.DebugFormat("Unrecognized or Disabled SSL Protocol {0} is ignored.", protocol);
                    }

                }
                return CreateProtocolFlags(SupportedProtocols, protocolsToExclude);
            }
            else if (this.SSLProtocol != null)
            {
                Tracer.InfoFormat("transport.SSLProtocol is deprecated should use transport.SSLExcludeProtocols instead.");
                SslProtocols value = DefaultProtocols;
                if(Enum.TryParse(this.SSLProtocol, true, out value))
                {
                    return value;
                }
                else
                {
                    return DefaultProtocols;
                }
            }
            else
            {
                return DefaultProtocols;
            }
            
        }

        private X509Certificate2Collection LoadClientCertificates()
        {
            X509Certificate2Collection certificates = new X509Certificate2Collection();

            if(!String.IsNullOrWhiteSpace(this.ClientCertFileName))
            {
                Tracer.DebugFormat("Attempting to load Client Certificate file: {0}", this.ClientCertFileName);
                X509Certificate2 certificate = new X509Certificate2(this.ClientCertFileName, this.ClientCertPassword);
                Tracer.DebugFormat("Loaded Client Certificate: {0}", certificate.Subject);

                certificates.Add(certificate);
            }
            else
            {
                string storeName = String.IsNullOrWhiteSpace(this.KeyStoreName) ? StoreName.My.ToString() : this.KeyStoreName;
                StoreLocation storeLocation = StoreLocation.CurrentUser;
                if(!String.IsNullOrWhiteSpace(this.KeyStoreLocation))
                {
                    bool found = false;
                    foreach(string location in Enum.GetNames(typeof(StoreLocation)))
                    {
                        if(ComparerInstance.Equals(this.KeyStoreLocation, location))
                        {
                            storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), location, true);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        throw new NMSException(string.Format("Invalid Store location {0}", this.KeyStoreLocation), NMSErrorCode.PROPERTY_ERROR);
                    }
                }

                Tracer.DebugFormat("Loading store {0}, from location {1}.", storeName, storeLocation.ToString());
                try
                {


                    X509Store store = new X509Store(storeName, storeLocation);

                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2[] storeCertificates = new X509Certificate2[store.Certificates.Count];
                    store.Certificates.CopyTo(storeCertificates, 0);
                    certificates.AddRange(storeCertificates);
                }
                catch(Exception ex)
                {
                    Tracer.WarnFormat("Error loading KeyStore, name : {0}; location : {1}. Cause {2}", storeName, storeLocation, ex);
                    throw ExceptionSupport.Wrap(ex, "Error loading KeyStore.", storeName, storeLocation.ToString());
                }
            }

            return certificates;
        }

        private Task<Amqp.Connection> CreateSecureConnection(Amqp.Address addr, Amqp.Framing.Open open, Amqp.OnOpened onOpened)
        {
            
            // Load local certificates
            this.connectionBuilder.SSL.ClientCertificates.AddRange(LoadClientCertificates());
            Tracer.DebugFormat("Loading Certificates from {0} possibilit{1}.", this.connectionBuilder.SSL.ClientCertificates.Count, (this.connectionBuilder.SSL.ClientCertificates.Count==1) ?"y" : "ies");
            
            // assign SSL protocols
            this.connectionBuilder.SSL.Protocols = GetSslProtocols();
            Tracer.DebugFormat("Set accepted SSL protocols to {0}.", this.connectionBuilder.SSL.Protocols.ToString());

            if(this.connectionBuilder.SSL.Protocols == SslProtocols.None)
            {
                string Property = (this.SSLExcludeProtocols == null) ? SecureTransportPropertyInterceptor.SSL_PROTOCOLS_PROPERTY : SecureTransportPropertyInterceptor.SSL_EXCLUDED_PROTOCOLS_PROPERTY;
                string value = this.SSLExcludeProtocols ?? this.SSLProtocol;
                throw new NMSSecurityException(string.Format("Invalid SSL Protocol {0} selected from system supported protocols {1} and property {2} with value {3}", this.connectionBuilder.SSL.Protocols, PropertyUtil.ToString( SupportedProtocols), Property, value));
            }

            ProviderCreateConnection delagate = base.CreateConnectionBuilder();
            return delagate.Invoke(addr, open, onOpened);
        }

        #endregion

        #region IProviderSecureTransportContext Methods

        public override ProviderCreateConnection CreateConnectionBuilder()
        {
            return new ProviderCreateConnection(this.CreateSecureConnection);
        }

        #endregion

        #region Certificate Callbacks

        protected X509Certificate ContextLocalCertificateSelect(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            if (Tracer.IsDebugEnabled)
            {
                string subjects = "{";
                string issuers = "{";
                string acceptedIssuers = "{";

                foreach (X509Certificate cert in localCertificates)
                {
                    subjects += cert.Subject + ", ";
                    issuers += cert.Issuer + ", ";
                }

                subjects += "}";
                issuers += "}";

                for (int i = 0; i < acceptableIssuers.Length; i++)
                {
                    acceptedIssuers += acceptableIssuers[i] + ", ";
                }

                Tracer.DebugFormat("Local Certificate Selection.\n" +
                    "Sender {0}, Target Host {1}, Remote Cert Subject {2}, Remote Cert Issuer {3}" +
                    "\nlocal Cert Subjects {4}, " +
                    "\nlocal Cert Issuers {5}",
                    sender.ToString(),
                    targetHost,
                    remoteCertificate?.Subject,
                    remoteCertificate?.Issuer,
                    subjects,
                    issuers);
            }
            X509Certificate localCertificate = null;
            if (ClientCertificateSelectCallback != null)
            {
                try
                {
                    if (Tracer.IsDebugEnabled) Tracer.DebugFormat("Calling application callback for Local certificate selection.");
                    localCertificate = ClientCertificateSelectCallback(sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
                }
                catch (Exception ex)
                {
                    Tracer.WarnFormat("Caught Exception from application callback for local certificate selction. Exception : {0}", ex);
                }
            }
            else if (localCertificates.Count >= 1)
            {
                // when there is only one certificate select that certificate.
                localCertificate = localCertificates[0];
                if (!String.IsNullOrWhiteSpace(this.ClientCertSubject))
                {
                    // should the application identify a specific certificate to use search for that certificate.
                    localCertificate = null;
                    foreach (X509Certificate cert in localCertificates)
                    {
                        if (String.Compare(cert.Subject, this.ClientCertSubject, true) == 0)
                        {
                            localCertificate = cert;
                            break;
                        }
                    }
                    
                }
            }

            if (localCertificate == null)
            {
                Tracer.WarnFormat("Could not select Local Certificate for target host {0}", targetHost);
            }
            else if (Tracer.IsDebugEnabled)
            {
                Tracer.DebugFormat("Selected Local Certificate {0}", localCertificate.ToString());
            }

            return localCertificate;
        }

        protected bool ContextServerCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            
            if (Tracer.IsDebugEnabled)
            {
                string name = null;
                if (certificate is X509Certificate2)
                {
                    X509Certificate2 cert = certificate as X509Certificate2;
                    name = cert.SubjectName.Name;


                }
                Tracer.DebugFormat("Cert DN {0}; Cert Subject {1}; Cert Issuer {2}; SSLPolicyErrors [{3}]", name, certificate?.Subject ?? "null", certificate?.Issuer ?? "null", sslPolicyErrors.ToString());
                try
                {
                    X509VerificationFlags verFlags = chain.ChainPolicy.VerificationFlags;
                    X509RevocationMode revMode = chain.ChainPolicy.RevocationMode;
                    X509RevocationFlag revFlags = chain.ChainPolicy.RevocationFlag;
                    StringBuilder sb = new StringBuilder();
                    sb.Append("ChainStatus={");
                    int size = sb.Length;
                    foreach (X509ChainStatus status in chain.ChainStatus)
                    {
                        X509ChainStatusFlags csflags = status.Status;
                        sb.AppendFormat("Info={0}; flags=0x{1:X}; flagNames=[{2}]", status.StatusInformation, csflags, csflags.ToString());
                        sb.Append(", ");
                    }
                    if (size != sb.Length)
                    {
                        sb.Remove(sb.Length - 2, 2);
                    }
                    sb.Append("}");

                    Tracer.DebugFormat("X.509 Cert Chain, Verification Flags {0:X} {1}, Revocation Mode {2}, Revocation Flags {3}, Status {4} ",
                        verFlags, verFlags.ToString(), revMode.ToString(), revFlags.ToString(), sb.ToString());
                }
                catch (Exception ex)
                {
                    Tracer.ErrorFormat("Error displaying Remote Cert fields. Cause: {0}", ex);
                }
            }

            bool? valid = null;
            if (ServerCertificateValidateCallback != null)
            {
                try
                {
                    if (Tracer.IsDebugEnabled) Tracer.DebugFormat("Calling application callback for Remote Certificate Validation.");
                    valid = ServerCertificateValidateCallback(sender, certificate, chain, sslPolicyErrors);
                }
                catch (Exception ex)
                {
                    Tracer.WarnFormat("Caught Exception from application callback for Remote Certificate Validation. Exception : {0}", ex);
                }
            }
            else if (sslPolicyErrors == SslPolicyErrors.None)
            {
                valid = true;
            }
            else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch && !String.IsNullOrWhiteSpace(this.ServerName))
            {
                valid = certificate.Subject.IndexOf(string.Format("CN={0}", this.ServerName), StringComparison.InvariantCultureIgnoreCase) > -1;
            }
            else
            {
                Tracer.WarnFormat("SSL certificate {0} validation error : {1}", certificate.Subject, sslPolicyErrors.ToString());
                valid = this.AcceptInvalidBrokerCert;
            }

            return valid ?? this.AcceptInvalidBrokerCert;
        }

        #endregion
        
        #region Copy Methods


        protected override void CopyBuilder(Amqp.ConnectionFactory copy)
        {
            base.CopyBuilder(copy);
            //StringDictionary secureProperties = PropertyUtil.GetProperties(connectionBuilder.SSL);

            //PropertyUtil.SetProperties(copy.SSL, secureProperties);

            copy.SSL.Protocols = connectionBuilder.SSL.Protocols;
            copy.SSL.CheckCertificateRevocation = connectionBuilder.SSL.CheckCertificateRevocation;

            if (connectionBuilder.SSL.ClientCertificates != null)
            {
                copy.SSL.ClientCertificates = new X509CertificateCollection(connectionBuilder.SSL.ClientCertificates);
            }

        }

        protected override void CopyInto(TransportContext copy)
        {
            SecureTransportContext stcCopy = copy as SecureTransportContext;

            // Copy Secure properties.

            // copy keysotre properties
            stcCopy.KeyStoreName = this.KeyStoreName;
            stcCopy.KeyStorePassword = this.KeyStorePassword;
            stcCopy.KeyStoreLocation = this.KeyStoreLocation;

            // copy certificate properties
            stcCopy.AcceptInvalidBrokerCert = this.AcceptInvalidBrokerCert;
            stcCopy.ServerName = this.ServerName;
            stcCopy.ClientCertFileName = this.ClientCertFileName;
            stcCopy.ClientCertPassword = this.ClientCertPassword;
            stcCopy.ClientCertSubject = this.ClientCertSubject;

            // copy application callback
            stcCopy.ServerCertificateValidateCallback = this.ServerCertificateValidateCallback;
            stcCopy.ClientCertificateSelectCallback = this.ClientCertificateSelectCallback;

            // SSL Protocols 
            stcCopy.SSLExcludeProtocols = this.SSLExcludeProtocols;
            stcCopy.SSLProtocol = this.SSLProtocol;

            base.CopyInto(copy);

            stcCopy.connectionBuilder.SSL.RemoteCertificateValidationCallback = this.ContextServerCertificateValidation;
            stcCopy.connectionBuilder.SSL.LocalCertificateSelectionCallback = this.ContextLocalCertificateSelect;
        }

        public override IProviderTransportContext Copy()
        {
            TransportContext copy = new SecureTransportContext();
            this.CopyInto(copy);
            return copy;
        }
        
        IProviderSecureTransportContext IProviderSecureTransportContext.Copy()
        {
            return this.Copy() as SecureTransportContext;
        }

        #endregion
    }

    /// <summary>
    /// SystemSSLProtocolHelper determines whether an SSLProtocol is enabled on a system
    /// </summary>
    #region SystemSSLProtocolHelper
// TODO change placeholder #if define to check OS platform
#if NET452 || NET46

    static class SystemSSLProtocolHelper
    {
        private const string SSLProtocolClientRegistryKeyTreeFormat = "SYSTEM\\CurrentControlSet\\Control\\SecurityProviders\\SCHANNEL\\Protocols\\{0}\\Client";
        private static readonly Microsoft.Win32.RegistryKey Local = Microsoft.Win32.Registry.LocalMachine;

        private const string DefaultDisabledName = "DisabledByDefault";
        private const string EnabledName = "Enabled";

        private static readonly StringDictionary protocolNameMap = new StringDictionary()
        {
            { SslProtocols.Ssl2.ToString(), "SSL 2.0" },
            { SslProtocols.Ssl3.ToString(), "SSL 3.0" },
            { SslProtocols.Tls.ToString(), "TLS 1.0" },
            { SslProtocols.Tls11.ToString(), "TLS 1.1" },
            { SslProtocols.Tls12.ToString(), "TLS 1.2" },
        };


        private static string SSLProtocolNameToRegistryName(string enumName)
        {
            if (protocolNameMap.ContainsKey(enumName))
            {
                return protocolNameMap[enumName];
            }
            else
            {
                return enumName;
            }
        }

        public static bool IsProtocolEnabled(string protocolString)
        {
            bool result = true;
            string ProtocolRegistryName = SSLProtocolNameToRegistryName(protocolString);
            string registryKeyString = string.Format(SSLProtocolClientRegistryKeyTreeFormat, ProtocolRegistryName);
            try
            {
                Microsoft.Win32.RegistryKey key = Local.OpenSubKey(registryKeyString);
                if (key != null)
                {
                    List<string> valueNames = new List<string>(key.GetValueNames());
                    if (valueNames.Contains(DefaultDisabledName) && !valueNames.Contains(EnabledName))
                    {
                        int disabled = Convert.ToInt32(key.GetValue(DefaultDisabledName));
                        result = disabled == 0;
                    }
                    else if (valueNames.Contains(DefaultDisabledName) && valueNames.Contains(EnabledName))
                    {
                        int ddisabled = Convert.ToInt32(key.GetValue(DefaultDisabledName));
                        int enabled = Convert.ToInt32(key.GetValue(EnabledName));
                        result = ddisabled == 0 && enabled != 0;
                    }
                    else if (!valueNames.Contains(DefaultDisabledName))
                    {
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if(Tracer.IsDebugEnabled)
                    Tracer.DebugFormat("Ignoring exception when searching registry for key {0}. Exception {1}", registryKeyString, ex);
            }
            return result;
        }
    }

#else

    static class SystemSSLProtocolHelper
    {
        public static bool IsProtocolEnabled(string protocolString) 
        {
            return true;
        }

    }

#endif
    #endregion
}