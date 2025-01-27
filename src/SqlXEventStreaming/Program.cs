using Microsoft.SqlServer.XEvent.XELite;

string connectionString = "Data Source=.;Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True";
string sessionName = "sample_session";

StreamAndWait(connectionString, sessionName);

static void StreamAndWait(string connectionString, string sessionName)
{
    var cancellationTokenSource = new CancellationTokenSource();

    var xeStream = new XELiveEventStreamer(connectionString, sessionName);

    Console.WriteLine("Press any key to stop listening...");
    Task waitTask = Task.Run(() =>
    {
        Console.ReadKey();
        cancellationTokenSource.Cancel();
    });

    Task readTask = xeStream.ReadEventStream(() =>
    {
        Console.WriteLine("Connected to session");
        return Task.CompletedTask;
    },
        xevent =>
        {
            Console.WriteLine(xevent);
            Console.WriteLine("");
            return Task.CompletedTask;
        },
        cancellationTokenSource.Token);


    try
    {
        Task.WaitAny(waitTask, readTask);
    }
    catch (TaskCanceledException)
    {
    }

    if (readTask.IsFaulted)
    {
        Console.Error.WriteLine("Failed with: {0}", readTask.Exception);
    }
}

/*
 * First two to create & start, last one to stop

CREATE EVENT SESSION [sample_session] ON SERVER 
ADD EVENT sqlserver.sql_statement_completed(
    ACTION(sqlserver.sql_text))
GO

ALTER EVENT SESSION [sample_session] ON SERVER 
    STATE = START
GO

ALTER EVENT SESSION [sample_session] ON SERVER 
    STATE = STOP
GO
*/