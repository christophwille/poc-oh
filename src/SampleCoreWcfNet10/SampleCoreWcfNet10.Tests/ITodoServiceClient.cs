using System.Runtime.Serialization;
using System.ServiceModel;

namespace SampleCoreWcfNet10.Tests;

// Client-side view of the contract, as a partner would get it from svcutil/WSDL. Deliberately NOT sharing
// the server types: the test then proves wire compatibility, not just that C# compiles against itself.
[ServiceContract(Namespace = "urn:sample:todo:v1", Name = "ITodoService")]
public interface ITodoServiceClient
{
    [OperationContract] TodoDto[] GetAll();
    [OperationContract] TodoDto? Get(Guid id);
    [OperationContract] TodoDto Add(string title);
    [OperationContract, FaultContract(typeof(TodoNotFoundFaultDto))] TodoDto Complete(Guid id);
    [OperationContract] bool Delete(Guid id);
}

[DataContract(Namespace = "urn:sample:todo:v1", Name = "TodoItem")]
public sealed class TodoDto
{
    [DataMember(Order = 1)] public Guid Id { get; set; }
    [DataMember(Order = 2)] public string Title { get; set; } = "";
    [DataMember(Order = 3)] public bool IsCompleted { get; set; }
    [DataMember(Order = 4)] public DateTimeOffset CreatedAt { get; set; }
}

[DataContract(Namespace = "urn:sample:todo:v1", Name = "TodoNotFoundFault")]
public sealed class TodoNotFoundFaultDto
{
    [DataMember(Order = 1)] public Guid Id { get; set; }
}
