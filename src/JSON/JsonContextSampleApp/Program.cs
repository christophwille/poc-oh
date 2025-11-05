using System.Text.Json;
using System.Text.Json.Serialization;

var val = JsonSerializer.Deserialize<SampleDataStructure>(@"{""Id"": 42 }", JsonContext.Default.SampleDataStructure);

Console.WriteLine(val.Id);

[JsonSerializable(typeof(SampleDataStructure))]
public partial class JsonContext : JsonSerializerContext { }

public class SampleDataStructure
{
    public int Id { get; set; }
}