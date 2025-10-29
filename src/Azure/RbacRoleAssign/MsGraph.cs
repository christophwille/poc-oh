using Azure.Identity;

namespace RbacRoleAssign
{
    internal static class MsGraph
    {
        internal static async Task<Guid> GetPrincipalIdFromUpnAsync(string upn)
        {
            try
            {
                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var graphClient = new Microsoft.Graph.GraphServiceClient(new VisualStudioCredential(), scopes);

                var user = await graphClient.Users[upn].GetAsync();

                if (user?.Id == null)
                {
                    throw new InvalidOperationException($"User with UPN '{upn}' not found.");
                }

                Console.WriteLine($"Found user: {user.DisplayName} (ID: {user.Id})");

                return Guid.Parse(user.Id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve principal ID for '{upn}': {ex.Message}", ex);
            }
        }

    }
}
