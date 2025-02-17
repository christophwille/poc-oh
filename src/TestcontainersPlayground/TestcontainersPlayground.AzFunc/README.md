# Testing Azure Functions

The problem: [Implement test worker/test framework](https://github.com/Azure/azure-functions-dotnet-worker/issues/281)


## Approach 1 (Function in Testcontainers itself)

https://www.tpeczek.com/2023/10/azure-functions-integration-testing.html

with code to be found here

https://github.com/tpeczek/dotnet-server-timing/tree/main/test/Test.Azure.Functions.Worker.ServerTiming

or

https://totheroot.io/article/hands-on-integration-test-azure-functions


## Approach 2

* All of those are not https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=hostbuilder%2Cwindows#version-2x
* All of those are not Microsoft.Testing.Framework with no IsTestProject / .exe output

https://github.com/ormikopo1988/azure-functions-isolated-code-testing-sample/ interesting bits eg TestHelpers.cs

Articles similar to that 

* https://www.wagemakers.net/posts/testing-service-bus-trigger-azure-functions/ [repo](https://github.com/mawax/azure-functions-integration-testing)
* https://medium.com/@yusufsarikaya023/integration-test-for-azure-function-net-core-isolated-worker-47dd63afa0e7 [repo](https://github.com/yusufsarikaya023/AzureFunctionIntegrationTest)


## Approach 3

https://github.com/wigmorewelsh/FunctionTestHost

That has several issues 

* xUnit2 (which makes it incompatible in a project using xUnit3)
* Dragging in a lot of stuff eg Orleans (which I understand if you build something to fit your needs)
* Is using IHostBuilder and not Isolated Functions v2 (FunctionsApplicationBuilder, see also same problem with UnitTestEx)


## How the Durable Team is Doing Integration Testing

* https://github.com/Azure/azure-functions-durable-extension/tree/dev/test/e2e
* https://github.com/Azure/azure-functions-durable-extension/blob/dev/.github/workflows/E2ETest.yml


## Aspire and others

https://learn.microsoft.com/en-us/dotnet/aspire/serverless/functions?tabs=dotnet-cli&pivots=visual-studio

https://github.com/Avanade/unittestex?tab=readme-ov-file#HTTP-triggered-Azure-Function (Issue opened for Isolated V2 support)
