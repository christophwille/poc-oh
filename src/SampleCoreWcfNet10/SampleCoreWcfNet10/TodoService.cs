using CoreWCF;
using Microsoft.AspNetCore.Authorization;
using SampleCoreWcfNet10.Contracts;

namespace SampleCoreWcfNet10;

// [Authorize] is enforced by CoreWCF through ASP.NET Core authorization: only an authenticated
// (= allow-listed client certificate) caller reaches any operation.
[Authorize]
public sealed class TodoService(InMemoryTodoStore store) : ITodoService
{
    public TodoItem[] GetAll() => store.GetAll();

    public TodoItem? Get(Guid id) => store.Get(id);

    public TodoItem Add(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new FaultException("Title is required.");
        return store.Add(title.Trim());
    }

    public TodoItem Complete(Guid id) =>
        store.Complete(id) ?? throw new FaultException<TodoNotFoundFault>(new TodoNotFoundFault { Id = id }, "Todo not found.");

    public bool Delete(Guid id) => store.Delete(id);
}
