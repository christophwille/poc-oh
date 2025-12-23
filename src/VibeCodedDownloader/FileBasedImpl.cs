#!/usr/bin/dotnet run
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:package DiffPlex@1.9.0
#:package ICSharpCode.Decompiler@9.1.0.7988
#:package LibGit2Sharp@0.31.0
#:package NuGet.Protocol@7.0.1

using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO.Compression;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using LibGit2Sharp;
using DiffPlex;
using NuGet.Frameworks;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.DiffBuilder;

NuGetPackageToVerify[] packagesToVerify = [
    new NuGetPackageToVerify("Opa.Wasm", "0.91.0", "net8.0"),
];

string nugetRepository = "nugetRepository";
Directory.CreateDirectory(nugetRepository);

SourceRepository repository = NuGet.Protocol.Core.Types.Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
SourceCacheContext cache = new();
ILogger logger = NullLogger.Instance;

Queue<NuGetPackageToVerify> packagesQueue = new(packagesToVerify);

// Download all packages
List<(string PackagePath, NuGetPackageToVerify Package)> downloadedPackages = [];
while (packagesQueue.Count > 0)
{
    var package = packagesQueue.Dequeue();

    string packagePath = Path.Combine(nugetRepository, $"{package.PackageName}.{package.Version}.nupkg");

    Console.WriteLine($"Downloading {package.PackageName} {package.Version}...");

    using FileStream packageStream = File.Create(packagePath);

    var version = NuGetVersion.Parse(package.Version);
    var tfm = NuGetFramework.ParseFolder(package.LibTargetToUse);

    await resource.CopyNupkgToStreamAsync(
        package.PackageName,
        version,
        packageStream,
        cache,
        logger,
        CancellationToken.None);

    downloadedPackages.Add((packagePath, package));
    Console.WriteLine($"Downloaded: {packagePath}");

    var deps = await resource.GetDependencyInfoAsync(
        package.PackageName,
        version,
        cache,
        logger,
        CancellationToken.None);
    foreach (var group in deps.DependencyGroups)
    {
        if (!group.TargetFramework.Equals(tfm))
            continue;
        foreach (var dep in group.Packages)
        {
            Console.WriteLine($"  Dependency: {dep.Id} {dep.VersionRange}");
            var depPackage = new NuGetPackageToVerify(
                dep.Id,
                dep.VersionRange.MinVersion!.ToString(),
                package.LibTargetToUse,
                ReferenceOnly: true);
            packagesQueue.Enqueue(depPackage);
            package.Dependencies.Add(depPackage);
        }
    }
}

// Decompress all downloaded packages
List<(string ExtractFolder, NuGetPackageToVerify Package)> extractedPackages = [];
foreach (var (packagePath, package) in downloadedPackages)
{
    string extractFolder = Path.Combine(nugetRepository, Path.GetFileNameWithoutExtension(packagePath));

    Console.WriteLine($"Extracting {packagePath}...");
    ZipFile.ExtractToDirectory(packagePath, extractFolder, overwriteFiles: true);
    Console.WriteLine($"Extracted to: {extractFolder}");

    extractedPackages.Add((extractFolder, package));
}

// Decompile assemblies from extracted packages
List<(string DecompiledFolder, NuGetPackageToVerify Package)> decompiledPackages = [];
foreach (var (extractFolder, package) in extractedPackages)
{
    if (package.ReferenceOnly)
    {
        Console.WriteLine($"Skipping decompilation for reference-only package: {package.PackageName}");
        continue;
    }

    string libTargetFolder = Path.Combine(extractFolder, "lib", package.LibTargetToUse);

    if (!Directory.Exists(libTargetFolder))
    {
        Console.WriteLine($"Warning: lib folder not found at {libTargetFolder}");
        continue;
    }

    string? assemblyPath = Directory.GetFiles(libTargetFolder, "*.dll").FirstOrDefault();

    if (assemblyPath is null)
    {
        Console.WriteLine($"Warning: No DLL found in {libTargetFolder}");
        continue;
    }

    string decompileOutputFolder = Path.Combine(
        Path.GetDirectoryName(nugetRepository)!,
        $"{package.PackageName}.{package.Version}");

    Directory.CreateDirectory(decompileOutputFolder);

    Console.WriteLine($"Decompiling {assemblyPath} to {decompileOutputFolder}...");

    var resolver = new UniversalAssemblyResolver(
        assemblyPath,
        true,
        NuGetFramework.ParseFolder(package.LibTargetToUse).DotNetFrameworkName);

    foreach (var reference in package.Dependencies)
    {
        string referenceLibFolder = Path.Combine(
            nugetRepository,
            $"{reference.PackageName}.{reference.Version}",
            "lib",
            package.LibTargetToUse);

        string? referenceAssemblyPath = Directory.GetFiles(referenceLibFolder, "*.dll").FirstOrDefault();
        Console.WriteLine($"  Adding reference: {referenceAssemblyPath}");
        if (referenceAssemblyPath is not null)
        {
            resolver.AddSearchDirectory(Path.GetDirectoryName(referenceAssemblyPath)!);
        }
    }

    var decompiler = new WholeProjectDecompiler(resolver);

    decompiler.DecompileProject(
        new PEFile(assemblyPath),
        decompileOutputFolder);

    Console.WriteLine($"Decompiled: {package.PackageName}");
    decompiledPackages.Add((decompileOutputFolder, package));
}

// Generate git diffs for decompiled packages
string verifiedDiffsFolder = Path.Combine(Path.GetDirectoryName(nugetRepository)!, "verifiedDiffs");
Directory.CreateDirectory(verifiedDiffsFolder);

foreach (var (decompiledFolder, package) in decompiledPackages)
{
    Console.WriteLine($"Generating diff for {package.PackageName}...");

    string repoPath = LibGit2Sharp.Repository.Init(decompiledFolder);
    using var repo = new LibGit2Sharp.Repository(decompiledFolder);

    if (repo.Head.Tip is null)
    {
        // Add a .gitignore to avoid committing unwanted files
        string gitignorePath = Path.Combine(decompiledFolder, ".gitignore");
        File.WriteAllText(gitignorePath, "*.user\n*.suo\n.vs/\nbin/\nobj/\n");
    }

    // Stage all files
    Commands.Stage(repo, "*");

    // Get the diff: compare HEAD to Index (staged changes)
    // If there are no commits yet, compare against empty tree
    TreeChanges diff;
    if (repo.Head.Tip is null)
    {
        diff = repo.Diff.Compare<TreeChanges>(null, DiffTargets.Index);
    }
    else
    {
        diff = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.Index);
    }

    if (!diff.Any())
    {
        Console.WriteLine($"No changes detected for {package.PackageName}, skipping HTML generation.");
        continue;
    }

    // Build HTML diff
    var htmlBuilder = new StringBuilder();
    htmlBuilder.AppendLine("<!DOCTYPE html>");
    htmlBuilder.AppendLine("<html><head>");
    htmlBuilder.AppendLine($"<title>Diff: {package.PackageName} {package.Version}</title>");
    htmlBuilder.AppendLine("<style>");
    htmlBuilder.AppendLine("body { font-family: 'Consolas', 'Monaco', monospace; margin: 20px; }");
    htmlBuilder.AppendLine("h1 { color: #333; }");
    htmlBuilder.AppendLine("h2 { color: #555; margin-top: 30px; border-bottom: 1px solid #ddd; }");
    htmlBuilder.AppendLine(".file { margin-bottom: 20px; }");
    htmlBuilder.AppendLine(".diff { background: #f8f8f8; border: 1px solid #ddd; border-radius: 4px; overflow: auto; }");
    htmlBuilder.AppendLine(".line { white-space: pre; padding: 2px 10px; margin: 0; }");
    htmlBuilder.AppendLine(".added { background-color: #e6ffec; color: #22863a; }");
    htmlBuilder.AppendLine(".deleted { background-color: #ffebe9; color: #cb2431; }");
    htmlBuilder.AppendLine(".modified { background-color: #fff5b1; }");
    htmlBuilder.AppendLine(".unchanged { background-color: #f8f8f8; color: #666; }");
    htmlBuilder.AppendLine(".line-number { color: #999; margin-right: 10px; user-select: none; }");
    htmlBuilder.AppendLine("</style>");
    htmlBuilder.AppendLine("</head><body>");
    htmlBuilder.AppendLine($"<h1>Decompiled Package Diff: {package.PackageName} {package.Version}</h1>");

    var diffBuilder = new InlineDiffBuilder(new Differ());

    foreach (var change in diff)
    {
        string filePath = change.Path;
        string fullPath = Path.Combine(decompiledFolder, filePath);

        htmlBuilder.AppendLine($"<div class='file'>");
        htmlBuilder.AppendLine($"<h2>{System.Web.HttpUtility.HtmlEncode(filePath)} ({change.Status})</h2>");
        htmlBuilder.AppendLine("<div class='diff'>");

        string oldContent = repo.Head.Tip is not null && change.Status != ChangeKind.Added
            ? ((Blob)repo.Head.Tip[change.OldPath].Target).GetContentText() ?? ""
            : "";

        string newContent = File.Exists(fullPath) && change.Status != ChangeKind.Deleted ? File.ReadAllText(fullPath) : "";

        var diffResult = diffBuilder.BuildDiffModel(oldContent, newContent);

        int lineNum = 1;
        foreach (var line in diffResult.Lines)
        {
            string cssClass = line.Type switch
            {
                ChangeType.Inserted => "added",
                ChangeType.Deleted => "deleted",
                ChangeType.Modified => "modified",
                _ => "unchanged"
            };
            string prefix = line.Type switch
            {
                ChangeType.Inserted => "+",
                ChangeType.Deleted => "-",
                _ => " "
            };
            htmlBuilder.AppendLine($"<div class='line {cssClass}'><span class='line-number'>{lineNum,4}</span>{prefix} {System.Web.HttpUtility.HtmlEncode(line.Text)}</div>");
            lineNum++;
        }

        htmlBuilder.AppendLine("</div></div>");
    }

    htmlBuilder.AppendLine("</body></html>");

    string htmlFilePath = Path.Combine(verifiedDiffsFolder, $"{package.PackageName}.{package.Version}.html");
    File.WriteAllText(htmlFilePath, htmlBuilder.ToString());
    Console.WriteLine($"Created diff HTML: {htmlFilePath}");
}

Console.WriteLine("Done!");



record NuGetPackageToVerify(string PackageName, string Version, string LibTargetToUse, bool ReferenceOnly = false)
{
    public List<NuGetPackageToVerify> Dependencies { get; init; } = [];
}