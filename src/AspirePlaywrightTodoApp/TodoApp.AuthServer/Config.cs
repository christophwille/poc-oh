using Duende.IdentityServer.Models;

namespace TodoApp.AuthServer;

public static class Config
{
    // Injected by the AppHost at endpoint-allocation time; ports are random in test runs.
    private static string FrontendOrigin =>
        (Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:4200").TrimEnd('/');

    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("todoapi", "Todo API"),
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // Angular SPA: authorization code + PKCE, public client (token lives in the browser)
            new Client
            {
                ClientId = "angular-spa",
                ClientName = "Angular Todo SPA",
                RequireClientSecret = false,
                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { FrontendOrigin },
                PostLogoutRedirectUris = { FrontendOrigin },
                AllowedCorsOrigins = { FrontendOrigin },

                AllowedScopes = { "openid", "profile", "todoapi" },
                RequireConsent = false,
                AccessTokenLifetime = 3600,
            },
        };
}
