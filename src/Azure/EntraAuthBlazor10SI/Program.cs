using EntraAuthBlazor10SI.Components;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

// Redirect URI: https://entraauthblazor10si.dev.localhost:7015/signin-oidc
// Signout URI: https://entraauthblazor10si.dev.localhost:7015/signout-oidc
// Make sure in the AAD App registration / Authentication to enable Id Token!

// Followed mostly this template:
//     https://github.com/VladislavAntonyuk/.NET-Templates/blob/main/templates/BlazorWebAppMicrosoftIdentityPlatform/WebApp1/Program.cs
//     Notable change: FallbackPolicy for Authorization to require authentication globally
//     Notes: changes req'd to _Imports.razor, RedirectToLogin.razor added, Routes.razor modified, Layout has username/sign out
// References:
// https://github.com/dotnet/aspnetcore/issues/51202#issuecomment-1998525088 (interesting OIDC variations later on)
// https://dev.to/the_architect/set-up-azure-entra-authentication-with-blazor-server-24mg
// https://iliaselmatani.codes/posts/azureadauthenticationblazorfluentui/

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    //.AddAuthentication(options =>
    //{
    //    // You want cookie as default and OIDC only for challenge
    //    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    //    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    //})
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);

        options.Events.OnTokenValidated = async context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            var claimsPrincipal = context.Principal;
            if (claimsPrincipal == null)
            {
                context.Fail("No claims principal found");
                return;
            }
        };
    });
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization(options =>
{
    // https://andrewlock.net/setting-global-authorization-policies-using-the-defaultpolicy-and-the-fallbackpolicy-in-aspnet-core-3/
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Template original
/*
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
*/
