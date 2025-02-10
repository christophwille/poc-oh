using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;
using SampleStuff.ToTest;
using System.Diagnostics;
namespace TestcontainersPlayground;

public class ExportImportBacPacTests : CustomSqlContainerAsyncLifetime
{
    public ExportImportBacPacTests(ITestOutputHelper testOutputHelper) : base("testdb", testOutputHelper)
    {
    }

    private const string BacPacExportFilename = "export.bacpac";

    [Fact, Order(1)]
    public async Task ExportBacPacTest()
    {
        using (var sqlDb = await StaticDbOperations.CreateDatabaseAsync(ConnectionString)) { }

        var sw = Stopwatch.StartNew();

        var dacExport = new DacServices(ConnectionString);
        dacExport.ExportBacpac(BacPacExportFilename, this.TargetDbName);

        logger?.LogInformation("Exporting bacpac took: " + sw.ElapsedMilliseconds + " ms");
        sw.Stop();
    }

    [Fact, Order(2)]
    public void ImportBacPacTest()
    {
        var sw = Stopwatch.StartNew();

        var dacImport = new DacServices(this.ConnectionString);
        var bacpac = BacPackage.Load(BacPacExportFilename);
        dacImport.ImportBacpac(bacpac, this.TargetDbName);

        logger?.LogInformation("Importing bacpac took: " + sw.ElapsedMilliseconds + " ms");
        sw.Stop();
    }
}
