using CoreWCF;

namespace SampleCoreWcfNet10.Contracts;

[ServiceContract(Namespace = TodoContract.Namespace)]
public interface ITodoService
{
    [OperationContract]
    TodoItem[] GetAll();

    [OperationContract]
    TodoItem? Get(Guid id);

    [OperationContract]
    TodoItem Add(string title);

    [OperationContract]
    [FaultContract(typeof(TodoNotFoundFault))]
    TodoItem Complete(Guid id);

    [OperationContract]
    bool Delete(Guid id);
}
