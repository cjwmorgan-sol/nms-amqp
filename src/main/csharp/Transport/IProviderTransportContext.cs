using System;

namespace NMS.AMQP.Transport
{
    /// <summary>
    /// 
    /// </summary>
    internal interface IProviderTransportContext
    {
        Apache.NMS.IConnectionFactory Owner { get; }

        #region Transport Options

        int ReceiveBufferSize { get; set; }

        int ReceiveTimeout { get; set; }

        int SendBufferSize { get; set; }

        int SendTimeout { get; set; }
        
        bool UseLogging { get; set; }

        #endregion

        ProviderCreateConnection CreateConnectionBuilder();

        IProviderTransportContext Copy();
    }
}