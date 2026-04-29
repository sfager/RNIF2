namespace RNIF2.Core.Configuration;

public sealed class RnifOptions
{
    public string ListenerPrefix { get; set; } = "http://localhost:8080/rnif/";
    public int MaxConcurrentRequests { get; set; } = 20;
    public string LocalTradingPartnerId { get; set; } = "";
    public string LocalBusinessName { get; set; } = "";
    public string MessagePattern { get; set; } = "Async";
    public SecurityOptions Security { get; set; } = new();
}

public sealed class SecurityOptions
{
    public bool EnableSignatureVerification { get; set; }
    public bool EnableEncryption { get; set; }
    public string SigningCertificateThumbprint { get; set; } = "";
    public string CertificateStoreName { get; set; } = "My";
    public string CertificateStoreLocation { get; set; } = "LocalMachine";
}
