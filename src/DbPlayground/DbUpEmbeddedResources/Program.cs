// https://github.com/DbUp/DbUp/issues/817

// warning MSB4011: "...\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.targets" cannot be imported again. It was already imported at "...\DbUpEmbeddedResources\DbUpEmbeddedResources.csproj (19,3)". This is most likely a build authoring error. This subsequent import will be ignored.
// This is a warning you have to live with

using DbUp;
using DbUpEmbeddedResources;
using System.Reflection;

var connectionString =
    args.FirstOrDefault()
    ?? "Server=.;Database=MyApp;Integrated Security=true;Encrypt=True;TrustServerCertificate=True";

EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader =
    DeployChanges.To
        .SqlDatabase(connectionString)
        .WithGzippedScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly()) // the only changed line from sample at https://dbup.readthedocs.io/en/latest/
        .LogToConsole()
        .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(result.Error);
    Console.ResetColor();
#if DEBUG
    Console.ReadLine();
#endif
    return -1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Success!");
Console.ResetColor();
return 0;
