using System.Runtime.Serialization;

namespace SampleCoreWcfNet10.Contracts;

[DataContract(Namespace = TodoContract.Namespace)]
public sealed record TodoItem
{
    [DataMember(Order = 1)] public Guid Id { get; init; }
    [DataMember(Order = 2)] public string Title { get; init; } = "";
    [DataMember(Order = 3)] public bool IsCompleted { get; init; }
    [DataMember(Order = 4)] public DateTimeOffset CreatedAt { get; init; }
}

[DataContract(Namespace = TodoContract.Namespace)]
public sealed record TodoNotFoundFault
{
    [DataMember(Order = 1)] public Guid Id { get; init; }
}

public static class TodoContract
{
    // Explicit, versioned namespace: this is the wire contract partners generate proxies from.
    public const string Namespace = "urn:sample:todo:v1";
}
