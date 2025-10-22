using Extism;
using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ShopSamplePlugin;

public class Functions
{
    [DllImport("extism", EntryPoint = "api_getcustomerdata")]
    public static extern ulong GetCustomerData(ulong offset);

    public static void Main()
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "calculate_discount")]
    public static int CalculateDiscount()
    {
        var payload = Pdk.GetInputJson(JsonContext.Default.CalculateDiscountRequest);

        if (payload is null)
        {
            Pdk.Log(LogLevel.Error, "Failed to deserialize input.");
            return 3;
        }

        using var block = Pdk.Allocate(@"{ ""CustomerId"": 4711 }");
        var ptr = GetCustomerData(block.Offset);
        var response = MemoryBlock.Find(ptr).ReadString();

        Pdk.Log(LogLevel.Info, "Host function response: " + response);

        var output = new CalculateDiscountResponse
        {
            DiscountPercent = payload.TotalInCents > 10000 ? 10 : 0
        };

        Pdk.SetOutputJson(output, JsonContext.Default.CalculateDiscountResponse);
        return 0;
    }
}

[JsonSerializable(typeof(CalculateDiscountRequest))]
[JsonSerializable(typeof(CalculateDiscountResponse))]
public partial class JsonContext : JsonSerializerContext { }

public class CalculateDiscountRequest
{
    public int CustomerId { get; set; }
    public int TotalInCents { get; set; }
}

public class CalculateDiscountResponse
{
    public int DiscountPercent { get; set; }
}

public class GetCustomerDataRequest
{
    public int CustomerId { get; set; }
}

public class GetCustomerDataResponse
{
    public string SomethingImportant { get; set; }
}