namespace MTPEntrypoint
{
    public class Tests
    {
        // Press F10 to hit the startup class of MTP
        /*
        namespace MTPEntrypoint
        {
            [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            internal sealed class MicrosoftTestingPlatformEntryPoint
            {
                public static async global::System.Threading.Tasks.Task<int> Main(string[] args)
                {
                    global::Microsoft.Testing.Platform.Builder.ITestApplicationBuilder builder = await global::Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);
                    SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args);
                    using (global::Microsoft.Testing.Platform.Builder.ITestApplication app = await builder.BuildAsync())
                    {
                        return await app.RunAsync();
                    }
                }
            }
        }*/


        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}
