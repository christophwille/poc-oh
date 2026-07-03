using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Test;
using TodoApp.AuthServer.Pages;

namespace TodoApp.AuthServer;

/// <summary>
/// In-memory user list that can be mutated at runtime (test seeding). The wrapped
/// <see cref="TestUserStore"/> holds the same list by reference, so additions and
/// removals are immediately visible to the login page and profile service.
/// </summary>
public class MutableUserStore
{
    private readonly List<TestUser> _users = TestUsers.Users;

    public MutableUserStore() => Store = new TestUserStore(_users);

    public TestUserStore Store { get; }

    public IReadOnlyList<TestUser> All
    {
        get { lock (_users) return _users.ToList(); }
    }

    public bool Add(string subjectId, string username, string password)
    {
        lock (_users)
        {
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            _users.Add(new TestUser
            {
                SubjectId = subjectId,
                Username = username,
                Password = password,
                IsActive = true,
                Claims = { new Claim(JwtClaimTypes.Name, username) }
            });
            return true;
        }
    }

    public bool Remove(string username)
    {
        lock (_users)
        {
            return _users.RemoveAll(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)) > 0;
        }
    }
}
