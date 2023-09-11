using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;

namespace SdkDI
{
    public class DemoService
    {
        private readonly SecretClient _secretClient;
        private readonly BlobServiceClient _blobServiceClient;

        public DemoService(SecretClient secretClient, BlobServiceClient blobServiceClient)
        {
            _secretClient = secretClient;
            _blobServiceClient = blobServiceClient;
        }
    }
}
