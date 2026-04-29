using System.Security.Cryptography.X509Certificates;

namespace RNIF2.Security;

public sealed class CertificateStore
{
    private readonly string _storeName;
    private readonly StoreLocation _storeLocation;

    public CertificateStore(string storeName = "My", string storeLocation = "LocalMachine")
    {
        _storeName = storeName;
        _storeLocation = Enum.TryParse<StoreLocation>(storeLocation, out var loc)
            ? loc
            : StoreLocation.LocalMachine;
    }

    public X509Certificate2? FindByThumbprint(string thumbprint, bool requirePrivateKey = false)
    {
        using var store = new X509Store(_storeName, _storeLocation);
        store.Open(OpenFlags.ReadOnly);
        var results = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            thumbprint.Replace(":", "").Replace(" ", ""),
            validOnly: false);
        return results.Count > 0 && (!requirePrivateKey || results[0].HasPrivateKey)
            ? results[0]
            : null;
    }

    public IReadOnlyList<X509Certificate2> FindBySubject(string subjectDn)
    {
        using var store = new X509Store(_storeName, _storeLocation);
        store.Open(OpenFlags.ReadOnly);
        var results = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subjectDn, false);
        return [.. results];
    }
}
