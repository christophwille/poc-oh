using AwesomeAssertions;
using NUnit.Framework;

namespace MTPCoverlet.Tests
{
    public class SampleClassTests
    {
        // For why the csproj looks like it does, see:
        // https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-migration-from-v1-to-v2#compatibility-with-vstest-based-dotnet-test
        // https://github.com/coverlet-coverage/coverlet/issues/1827

        // Options https://www.nuget.org/packages/coverlet.MTP/#readme-body-tab
        // eg dotnet test --coverlet
        // coverage data in: MTPCoverlet.Tests\bin\Debug\net10.0\TestResults

        [Test]
        public void MyTest()
        {
            var sut = new SampleClass();
            var result = sut.SampleMethod(true);
            result.Should().Be("Branch1");
        }
    }
}
