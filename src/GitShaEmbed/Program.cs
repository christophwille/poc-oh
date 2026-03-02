// https://github.com/devlooped/GitInfo unusable because of license requirements (as usual)

// https://github.com/adamralph/minver
// https://github.com/dotnet/Nerdbank.GitVersioning
// https://github.com/GitTools/GitVersion go-to tool when full versioning is required

// Lighweight
// https://gist.github.com/ElanHasson/30384431651534b215e754a554b5b6ae
// https://github.com/MarkPflug/MSBuildGitHash

using System.Reflection;

string? gitHash = Assembly
    .GetEntryAssembly()!
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .FirstOrDefault(attr => attr.Key == "GitHash")?.Value;

Console.WriteLine($"Git hash: {gitHash}");