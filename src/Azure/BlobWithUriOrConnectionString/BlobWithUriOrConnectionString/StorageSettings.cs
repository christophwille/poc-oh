namespace BlobWithUriOrConnectionString;

public class StorageSettings
{
    private StorageSettings(bool uriBased, string connectionString, Dictionary<string, string>? keyValuePairs)
    {
        IsUriBased = uriBased;
        ConnectionString = connectionString;
        KeyValuePairs = keyValuePairs;
    }

    public bool IsUriBased { get; }
    public string ConnectionString { get; }

    public Dictionary<string, string>? KeyValuePairs { get; }
    public string AccountName => IsUriBased ? ConnectionString : KeyValuePairs["AccountName"];
    public string? AccountKey => IsUriBased ? null : KeyValuePairs["AccountKey"];

    public static StorageSettings Create(string connectionString)
    {
        bool uriBased = !connectionString.StartsWith("DefaultEndpointsProtocol=");

        Dictionary<string, string>? kvPairs = null;
        if (!uriBased)
        {
            kvPairs = GetKeyValuePairsFromStorageConnectionString(connectionString);
        }

        return new StorageSettings(uriBased, connectionString, kvPairs);
    }

    // https://www.craftedforeveryone.com/beginners-guide-and-reference-to-azure-blob-storage-sdk-v12-dot-net-csharp/#get_account_name_and_account_key_from_a_connection_string
    static Dictionary<string, string> GetKeyValuePairsFromStorageConnectionString(string storageConnectionString)
    {
        var settings = new Dictionary<string, string>();

        foreach (var nameValue in storageConnectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var splittedNameValue = nameValue.Split(new char[] { '=' }, 2);
            settings.Add(splittedNameValue[0], splittedNameValue[1]);
        }

        return settings;
    }
}
