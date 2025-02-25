# Testcontainers Playground

https://xunit.net/docs/getting-started/v3/whats-new

Various options (per test, per fixture, per collection)
https://blog.jetbrains.com/dotnet/2023/10/24/how-to-use-testcontainers-with-dotnet-unit-tests/

https://dotnet.testcontainers.org/

https://dotnet.testcontainers.org/modules/mssql/ 
  see also https://github.com/testcontainers/testcontainers-dotnet/blob/develop/src/Testcontainers.MsSql/MsSqlBuilder.cs


TODO

* Using the new https://www.nuget.org/packages/Testcontainers.XunitV3/#versions-body-tab (available now)
* https://github.com/jbogard/Respawn on top of db Testcontainer
* Custom db Testcontainer with restored db for faster startup than migration
* Test Azure Functions using Testcontainers


## Notes

XELite isn't exactly great, maybe going full SMO is a more reliable option
https://github.com/microsoft/sqltoolsservice/blob/main/src/Microsoft.SqlTools.ServiceLayer/Profiler/ProfilerService.cs
