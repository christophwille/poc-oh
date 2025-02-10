namespace TestcontainersPlayground
{
    public class CustomTestcontainersFixture<TFixture> : IClassFixture<TFixture> where TFixture : class
    {
        protected readonly TFixture fixture;
        protected readonly ITestOutputHelper output;

        public CustomTestcontainersFixture(TFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.output = output;
        }
    }
}
