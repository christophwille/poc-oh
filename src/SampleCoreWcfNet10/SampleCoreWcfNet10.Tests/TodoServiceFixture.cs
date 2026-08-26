using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace SampleCoreWcfNet10.Tests;

/// <summary>
/// Hosts the real service on Kestrel (TLS + client certificates cannot be exercised through TestServer)
/// with a per-run PKI, configured purely through the same configuration keys production uses.
/// </summary>
public sealed class TodoServiceFixture : WebApplicationFactory<Program>
{
    private const string PfxPassword = "test";
    private readonly string _dir = Directory.CreateTempSubdirectory("todo-mtls-").FullName;

    public TestCertificates Certs { get; } = new();

    public TodoServiceFixture() => UseKestrel();

    public Uri ServiceUri => new(Services.GetRequiredService<IServer>().Features
        .Get<IServerAddressesFeature>()!.Addresses.Single() + "/TodoService.svc");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var caPath = Path.Combine(_dir, "ca.cer");
        var serverPfx = Path.Combine(_dir, "server.pfx");
        File.WriteAllBytes(caPath, Certs.Ca.Export(X509ContentType.Cert));
        File.WriteAllBytes(serverPfx, Certs.Server.Export(X509ContentType.Pkcs12, PfxPassword));

        builder.ConfigureWebHost(web => web
            .UseSetting("Kestrel:Endpoints:Https:Url", "https://127.0.0.1:0")
            .UseSetting("Kestrel:Certificates:Default:Path", serverPfx)
            .UseSetting("Kestrel:Certificates:Default:Password", PfxPassword)
            .UseSetting("Mtls:CaCertificatePath", caPath)
            .UseSetting("Mtls:Partners:0:Name", "Contoso")
            .UseSetting("Mtls:Partners:0:Sha256Thumbprint", TestCertificates.Sha256Fingerprint(Certs.Partner)));

        return base.CreateHost(builder);
    }

    /// <summary>A WCF client exactly like an external partner would build one: transport security + client certificate.</summary>
    public ChannelFactory<ITodoServiceClient> CreateChannelFactory(X509Certificate2? clientCertificate)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
        binding.Security.Transport.ClientCredentialType =
            clientCertificate is null ? HttpClientCredentialType.None : HttpClientCredentialType.Certificate;

        var factory = new ChannelFactory<ITodoServiceClient>(binding, new EndpointAddress(ServiceUri));
        factory.Credentials.ClientCertificate.Certificate = clientCertificate;
        // Pin the test server certificate instead of trusting the machine store.
        factory.Credentials.ServiceCertificate.SslCertificateAuthentication = new X509ServiceCertificateAuthentication
        {
            CertificateValidationMode = X509CertificateValidationMode.Custom,
            CustomCertificateValidator = new PinnedCertificateValidator(Certs.Server),
        };
        return factory;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        Certs.Dispose();
        Directory.Delete(_dir, recursive: true);
    }

    private sealed class PinnedCertificateValidator(X509Certificate2 expected) : X509CertificateValidator
    {
        public override void Validate(X509Certificate2 certificate)
        {
            if (!certificate.RawData.AsSpan().SequenceEqual(expected.RawData))
                throw new SecurityTokenValidationException("Unexpected server certificate.");
        }
    }
}
