using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SampleCoreWcfNet10.Tests;

/// <summary>A throw-away PKI: private CA, server cert and client certs, all generated in memory per test run.</summary>
public sealed class TestCertificates : IDisposable
{
    public X509Certificate2 Ca { get; }
    public X509Certificate2 Server { get; }
    /// <summary>Issued by <see cref="Ca"/> and on the allow-list.</summary>
    public X509Certificate2 Partner { get; }
    /// <summary>Issued by <see cref="Ca"/> but NOT on the allow-list.</summary>
    public X509Certificate2 UnknownPartner { get; }
    /// <summary>Issued by a different CA.</summary>
    public X509Certificate2 Rogue { get; }

    public TestCertificates()
    {
        Ca = CreateCa("Test Todo CA");
        Server = Issue(Ca, "localhost", server: true);
        Partner = Issue(Ca, "Contoso", server: false);
        UnknownPartner = Issue(Ca, "Fabrikam", server: false);
        using var otherCa = CreateCa("Some Other CA");
        Rogue = Issue(otherCa, "Contoso", server: false);
    }

    public static string Sha256Fingerprint(X509Certificate2 cert) => cert.GetCertHashString(HashAlgorithmName.SHA256);

    private static X509Certificate2 CreateCa(string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, false, 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));
        return Persist(request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1)));
    }

    private static X509Certificate2 Issue(X509Certificate2 ca, string commonName, bool server)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid(server ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2")], critical: false));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(ca, includeKeyIdentifier: true, includeIssuerAndSerial: false));
        if (server)
        {
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(System.Net.IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());
        }

        var serial = RandomNumberGenerator.GetBytes(16);
        serial[0] &= 0x7F; // positive serial number
        using var issued = request.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddMonths(6), serial);
        return Persist(issued.CopyWithPrivateKey(key));
    }

    // Round-trip through PKCS#12 so the private key is not ephemeral (SChannel refuses ephemeral keys for TLS).
    private static X509Certificate2 Persist(X509Certificate2 cert)
    {
        using (cert)
            return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.Exportable);
    }

    public void Dispose()
    {
        Ca.Dispose(); Server.Dispose(); Partner.Dispose(); UnknownPartner.Dispose(); Rogue.Dispose();
    }
}
