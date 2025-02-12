using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace TestcontainersPlayground
{
    // From: https://github.com/tpeczek/dotnet-server-timing/blob/main/test/Test.Azure.Functions.Worker.ServerTiming/Infrastructure/AzureFunctionsTestcontainersFixture.cs
    // Changed: Directory logic for WithDockerfileDirectory, name of Dockerfile for WithDockerfile
    public class AzureFunctionsTestcontainersFixture : IAsyncLifetime
    {
        private readonly IFutureDockerImage _azureFunctionsDockerImage;

        public IContainer AzureFunctionsContainerInstance { get; private set; }

        public AzureFunctionsTestcontainersFixture()
        {
            var slnDirectory = CommonDirectoryPath.GetSolutionDirectory();
            var azfuncDirectory = new CommonDirectoryPath(slnDirectory.DirectoryPath + "./AzFunc.ToTest/");

            _azureFunctionsDockerImage = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(azfuncDirectory, String.Empty)
                .WithDockerfile("IntegrationTesting.Dockerfile")
                .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
                .Build();
        }

        public async ValueTask InitializeAsync()
        {
            await _azureFunctionsDockerImage.CreateAsync();

            AzureFunctionsContainerInstance = new ContainerBuilder()
                .WithImage(_azureFunctionsDockerImage)
                .WithPortBinding(80, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(80)))
                .Build();
            await AzureFunctionsContainerInstance.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await AzureFunctionsContainerInstance.DisposeAsync();

            await _azureFunctionsDockerImage.DisposeAsync();
        }
    }
}
