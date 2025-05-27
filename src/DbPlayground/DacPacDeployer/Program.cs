using DacPacDeployer;
using Microsoft.Extensions.Logging;

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger("Program");

var deployer = new Deployer(logger);

byte[] dacpacBytes = File.ReadAllBytes("../../../../SampleDb/bin/Debug/SampleDb.dacpac");
MemoryStream ms = new MemoryStream(dacpacBytes);

string connStr = "Data Source=.;Integrated Security=true;Encrypt=True;TrustServerCertificate=True";
string targetDb = "pubs1";
var report = deployer.GenerateDeployReport(connStr, targetDb, ms);

var reportDeserialized = report.DeserializeReport();

Console.WriteLine($"Alerts: {reportDeserialized.Alerts.Count} Warnings: {reportDeserialized.Warnings.Count} Errors: {reportDeserialized.Errors.Count}");

// Aspire: https://erikej.github.io/sql/dacfx/aspire/2025/05/18/sql-aspire-dacfx.html
var result = deployer.DeployDacPac(connStr, targetDb, ms);

Console.WriteLine(result.Succeeded ? "Deployment succeeded" : "Deployment failed");
Console.WriteLine("-----");

result.Messages.ForEach(m => Console.WriteLine(m));
Console.ReadLine();