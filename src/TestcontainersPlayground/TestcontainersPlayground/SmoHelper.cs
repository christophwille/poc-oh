using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace TestcontainersPlayground
{
    public record ExecResult(bool Succeeded, string Exception = null);

    public static class SmoHelper
    {
        // If you need to seed data with an exported SQL script that contains GO statements, this is your ticket
        // Eg after applying the migration to seed specific test data
        public static ExecResult ExecuteSqlScript(string script, string connectionString, ILogger? log = null)
        {
            try
            {
                SqlConnection connection = new SqlConnection(connectionString);
                var server = new Server(new ServerConnection(connection));
                int result = server.ConnectionContext.ExecuteNonQuery(script);
                return new ExecResult(true);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, ex.Message);
                return new ExecResult(false, ex.Message);
            }
        }
    }
}
