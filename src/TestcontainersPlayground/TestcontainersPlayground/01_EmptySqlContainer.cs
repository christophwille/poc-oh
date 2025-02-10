using Microsoft.Data.SqlClient;
using Shouldly;
using Testcontainers.MsSql;

namespace TestcontainersPlayground;

public class EmptySqlContainer
{
    [Fact]
    public async Task Empty_Sql_Container_MasterOnly_ExecuteCommand()
    {
        await using (var msSqlContainer = new MsSqlBuilder().Build())
        {
            await msSqlContainer.StartAsync(TestContext.Current.CancellationToken);

            string connectionString = msSqlContainer.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";

            var actual = await command.ExecuteScalarAsync() as int?;
            actual.ShouldBe(1);
        }
    }
}
