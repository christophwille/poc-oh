using Newtonsoft.Json;

var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.All,
};

var container = new MessageContainer()
{
    Id = Guid.NewGuid().ToString(),
    Type = typeof(ASimpleMessage).Name,
    Data = JsonConvert.SerializeObject(
        new ASimpleMessage("Hello World", "Everywhere"),
        settings)
};

Console.WriteLine(container.Data);
var data = JsonConvert.DeserializeObject<IMessageMarker>(container.Data, settings);

Console.WriteLine((data as ASimpleMessage).Message);

public class MessageContainer
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Data { get; set; }
}

public interface IMessageMarker { };

public record ASimpleMessage(string Message, string Location) : IMessageMarker;