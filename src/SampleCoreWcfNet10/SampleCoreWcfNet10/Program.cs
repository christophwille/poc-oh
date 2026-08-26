using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using SampleCoreWcfNet10;
using SampleCoreWcfNet10.Contracts;
using SampleCoreWcfNet10.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<MtlsOptions>()
    .BindConfiguration(MtlsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Transport: the TLS handshake must present a client certificate. Chain + allow-list are checked
// by the certificate authentication handler below (Kestrel itself only knows the OS trust store).
builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureHttpsDefaults(https =>
{
    https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
    https.AllowAnyClientCertificate();
}));

builder.Services
    .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate();

builder.Services
    .AddOptions<CertificateAuthenticationOptions>(CertificateAuthenticationDefaults.AuthenticationScheme)
    .Configure<IOptions<MtlsOptions>>((options, mtls) =>
    {
        options.AllowedCertificateTypes = CertificateTypes.Chained;
        options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
        options.CustomTrustStore = [X509CertificateLoader.LoadCertificateFromFile(mtls.Value.CaCertificatePath)];
        // Private CA without CRL/OCSP; the explicit allow-list is the revocation mechanism.
        options.RevocationMode = X509RevocationMode.NoCheck;
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                var fingerprint = context.ClientCertificate.GetCertHashString(HashAlgorithmName.SHA256);
                var partner = mtls.Value.Partners.FirstOrDefault(p =>
                    string.Equals(p.Sha256Thumbprint, fingerprint, StringComparison.OrdinalIgnoreCase));

                if (partner is null)
                {
                    context.Fail($"Certificate {fingerprint} is not an allow-listed partner.");
                    return Task.CompletedTask;
                }

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, partner.Name), new Claim(ClaimTypes.Thumbprint, fingerprint)],
                    context.Scheme.Name));
                context.Success();
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<InMemoryTodoStore>();
builder.Services.AddTransient<TodoService>(); // CoreWCF resolves service instances from DI (per call)
builder.Services.AddServiceModelServices().AddServiceModelMetadata();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseServiceModel(serviceModel =>
{
    var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
    // ASP.NET Core (Kestrel + certificate handler) owns authentication; CoreWCF consumes HttpContext.User.
    binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.InheritedFromHost;

    serviceModel
        .AddService<TodoService>(options =>
            options.DebugBehavior.IncludeExceptionDetailInFaults = app.Environment.IsDevelopment())
        .AddServiceEndpoint<TodoService, ITodoService>(binding, "/TodoService.svc");
});

// WSDL for partners at https://host/TodoService.svc?wsdl (still behind mTLS).
app.Services.GetRequiredService<ServiceMetadataBehavior>().HttpsGetEnabled = true;

app.Run();

public partial class Program;
