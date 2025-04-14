using Microsoft.Data.SqlClient;

// string connectionString = "Server=.;Database=pubs;Integrated Security=true;Encrypt=True;TrustServerCertificate=True";
string connectionString = "Server=127.0.0.1,10109;Database=pubs;User Id=sa;Password=YourS3cureP@ass;Encrypt=True;TrustServerCertificate=True";
string query = "SELECT * FROM authors";

try
{
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        connection.Open();

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    Console.WriteLine("Authors:");
                    while (reader.Read())
                    {
                        string authorId = reader["au_id"].ToString();
                        string authorName = reader["au_lname"].ToString();
                        Console.WriteLine($"ID: {authorId}, Name: {authorName}");
                    }
                }
                else
                {
                    Console.WriteLine("No authors found.");
                }
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
