using Azure.Identity;
using Microsoft.Extensions.Azure;

namespace SdkDI
{
    public static class Extensions
    {
        private const string BlobUri = "https://{0}.blob.core.windows.net";

        // https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection
        public static void AddSdkClients(this IServiceCollection services, IConfiguration configuration, bool isDevelopmentEnvironment = false)
        {
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder
                    .AddBlobServiceClient(new Uri(String.Format(BlobUri, "aname")))
                    .WithName("aname");

                //clientBuilder
                //    .AddBlobServiceClient(configuration...) // connectionstring-based
                //    .WithName("aname");

                string keyVaultUrl = configuration.GetValue<string>("akeyvaulturlsetting");
                clientBuilder.AddSecretClient(new Uri(keyVaultUrl));

                // Basic options https://blog.jongallant.com/2021/08/azure-identity-201/
                // Optimize via https://scottsauber.com/2022/05/10/improving-azure-key-vault-performance-in-asp-net-core-by-up-to-10x/
                // Ordered resolution via ChainedTokenCredential: https://yourazurecoach.com/2020/08/13/managed-identity-simplified-with-the-new-azure-net-sdks/
                Azure.Core.TokenCredential credential = null;
                if (isDevelopmentEnvironment)
                {
                    credential = new VisualStudioCredential();
                }
                else
                {
                    credential = new ChainedTokenCredential(
                            new ManagedIdentityCredential(),
                            new VisualStudioCredential(),
                            new AzurePowerShellCredential()
                        );
                }

                clientBuilder.UseCredential(credential);
            });
        }
    }
}
