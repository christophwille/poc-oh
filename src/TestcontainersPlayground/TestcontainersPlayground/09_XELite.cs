using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.XEvent.XELite;
using SampleStuff.ToTest;
using Shouldly;
using Testcontainers.MsSql;

namespace TestcontainersPlayground;

public class XELiteTests : IAsyncLifetime
{
    private ILogger logger;

    public XELiteTests(ITestOutputHelper testOutputHelper)
    {
        logger = testOutputHelper.CreateILogger();
    }

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();

    public async ValueTask InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }

    /* Standard Output:
        Connected to session
        Event:sql_statement_completed, UUID: cdfd84f9-184e-49a4-bb71-1614a9d30416, Timestamp: 2025-02-11 17:10:42Z, Fields:[duration, 179], [cpu_time, 0], [page_server_reads, 0], [physical_reads, 0], [logical_reads, 2], [writes, 0], [spills, 0], [row_count, 1], [last_row_count, 1], [line_number, 1], [offset, 0], [offset_end, 160], [statement, SELECT TOP(1) [b].[BlogId], [b].[Url]
        FROM [Blogs] AS [b]
        ORDER BY [b].[BlogId]], [parameterized_plan_handle, System.Byte[]], Actions:[sql_text, SELECT TOP(1) [b].[BlogId], [b].[Url]
        FROM [Blogs] AS [b]
        ORDER BY [b].[BlogId]\0] */
    [Fact]
    public async Task Create_Db_InTest_AndQuery()
    {
        var masterDbConn = _msSqlContainer.GetConnectionString();
        var testDbConn = masterDbConn.Replace("Database=master", $"Database=testdb");

        using (var createDbCtx = await StaticDbOperations.CreateDatabaseAsync(testDbConn)) { }

        const string sessionName = "sample_session";

        // https://learn.microsoft.com/en-us/sql/relational-databases/extended-events/quick-start-extended-events-in-sql-server?view=sql-server-ver16#create-an-event-session-using-t-sql
        string sessioninit = @$"CREATE EVENT SESSION [{sessionName}] ON SERVER 
            ADD EVENT sqlserver.sql_statement_completed(
                ACTION(sqlserver.sql_text))
            GO

            ALTER EVENT SESSION [{sessionName}] ON SERVER 
                STATE = START
            GO";

        var server = new Server(new ServerConnection(new SqlConnection(masterDbConn)));
        int result = server.ConnectionContext.ExecuteNonQuery(sessioninit);

        var cancellationTokenSource = new CancellationTokenSource();
        var xeStream = new XELiveEventStreamer(masterDbConn, sessionName);

        Task readTask = xeStream.ReadEventStream(() =>
            {
                logger.LogInformation("Connected to session");
                return Task.CompletedTask;
            },
            xevent =>
            {
                logger.LogInformation(xevent.ToString() + "\r\n");
                return Task.CompletedTask;
            },
            cancellationTokenSource.Token);

        using (var dbCtx = StaticDbOperations.GetContext(testDbConn))
        {
            int affected = await StaticDbOperations.PerformDbOperationsAsync(dbCtx);
            affected.ShouldBe(3);
        }

        // Without waiting, the event stream will not be read (simply F12 into XELiveEventStreamer)
        await Task.Delay(5000);

        try
        {
            await cancellationTokenSource.CancelAsync();
            await readTask;
        }
        catch (Exception e)
        {
        }

        string sessionclose = @$"ALTER EVENT SESSION [{sessionName}] ON SERVER 
                STATE = STOP
            GO";
        result = server.ConnectionContext.ExecuteNonQuery(sessionclose);
    }
}
