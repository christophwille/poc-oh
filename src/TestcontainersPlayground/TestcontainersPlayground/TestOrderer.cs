using System.Reflection;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace TestcontainersPlayground
{
    // https://stackoverflow.com/a/78936785
    public class TestOrderer : ITestCaseOrderer
    {
        public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : notnull, ITestCase
        {
            return testCases.OrderBy(t => GetTestMethodOrder(t)).CastOrToReadOnlyCollection();
        }

        private static int GetTestMethodOrder<TTestCase>(TTestCase testCase) where TTestCase : notnull, ITestCase
        {
            var testMethod = testCase.TestMethod as XunitTestMethod ?? throw new InvalidOperationException("Error trying to figure out test method order.");
            return testMethod.Method.GetCustomAttribute<OrderAttribute>()?.Order ?? throw new InvalidOperationException("Error trying to figure out test method order.");
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OrderAttribute(int order) : Attribute
    {
        public int Order { get; private set; } = order;
    }
}
