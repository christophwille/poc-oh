using System.Collections.Concurrent;
using SampleCoreWcfNet10.Contracts;

namespace SampleCoreWcfNet10;

// ponytail: in-memory, per-process. Swap for a DB when data must survive a restart.
public sealed class InMemoryTodoStore
{
    private readonly ConcurrentDictionary<Guid, TodoItem> _items = new();

    public TodoItem[] GetAll() => _items.Values.OrderBy(t => t.CreatedAt).ToArray();

    public TodoItem? Get(Guid id) => _items.GetValueOrDefault(id);

    public TodoItem Add(string title)
    {
        var item = new TodoItem { Id = Guid.NewGuid(), Title = title, CreatedAt = DateTimeOffset.UtcNow };
        _items[item.Id] = item;
        return item;
    }

    public TodoItem? Complete(Guid id)
    {
        while (_items.TryGetValue(id, out var current))
        {
            var updated = current with { IsCompleted = true };
            if (_items.TryUpdate(id, updated, current))
                return updated;
        }
        return null;
    }

    public bool Delete(Guid id) => _items.TryRemove(id, out _);
}
