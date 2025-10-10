using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using System.IO.Compression;

// Similar to https://github.com/christophwille/AppServiceCurrentStackViaAzureSdk/ but created with CoPilot to deploy via SCM
// Had not set Stack, had not enabled SCM Basic Auth, had many properties wrong in SiteConfigProperties

ArmClient armClient = new ArmClient(new VisualStudioCredential());

// Configuration
var subscriptionId = "your-subscription-id-here";
var resourceGroupName = "demo-appservice-scmdeploy";
var appServicePlanName = "plan987654321a";
var webAppName = "site987654321a";
var location = AzureLocation.WestEurope;
var publishPath = @"D:\Demos\SiteDeployDemoApp\bin\Release\net8.0\publish";

var subscription = await armClient.GetSubscriptionResource(
    new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}")
).GetAsync();

var resourceGroups = subscription.Value.GetResourceGroups();
var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

// Create or get App Service Plan
var appServicePlan = await CreateOrGetAppServicePlan(
    resourceGroup.Value,
    appServicePlanName,
    location
);

// Create or get Web App
var webApp = await CreateOrGetWebApp(
    resourceGroup.Value,
    webAppName,
    appServicePlan,
    location
);

// Configure App Settings
await ConfigureAppSettings(webApp);

// Deploy application
await DeployApplication(webApp, publishPath);

Console.WriteLine($"Deployment completed successfully to: https://{webAppName}.azurewebsites.net");

// Helper Methods
static async Task<AppServicePlanResource> CreateOrGetAppServicePlan(
    ResourceGroupResource resourceGroup,
    string planName,
    AzureLocation location)
{
    var plans = resourceGroup.GetAppServicePlans();

    try
    {
        // Try to get existing plan
        var existingPlan = await plans.GetAsync(planName);
        Console.WriteLine($"Using existing App Service Plan: {planName}");
        return existingPlan.Value;
    }
    catch (Azure.RequestFailedException)
    {
        // Create new plan
        Console.WriteLine($"Creating App Service Plan: {planName}");

        var planData = new AppServicePlanData(location)
        {
            Sku = new AppServiceSkuDescription
            {
                Name = "B1",
                Tier = "Basic",
                Size = "B1",
                Family = "B"
            },
            Kind = "windows"
        };

        var planOperation = await plans.CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            planName,
            planData
        );

        return planOperation.Value;
    }
}

static async Task<WebSiteResource> CreateOrGetWebApp(
    ResourceGroupResource resourceGroup,
    string webAppName,
    AppServicePlanResource appServicePlan,
    AzureLocation location)
{
    var webSites = resourceGroup.GetWebSites();

    try
    {
        // Try to get existing web app
        var existingWebApp = await webSites.GetAsync(webAppName);
        Console.WriteLine($"Using existing Web App: {webAppName}");
        return existingWebApp.Value;
    }
    catch (Azure.RequestFailedException)
    {
        // Create new web app
        Console.WriteLine($"Creating Web App: {webAppName}");

        var webAppData = new WebSiteData(location)
        {
            AppServicePlanId = appServicePlan.Id,
            SiteConfig = new SiteConfigProperties
            {
                NetFrameworkVersion = "v8.0",
                IsAlwaysOn = true,
                IsHttpLoggingEnabled = true,
                IsDetailedErrorLoggingEnabled = true,
                IsHttp20Enabled = true,
                MinTlsVersion = AppServiceSupportedTlsVersion.Tls1_2,
                Use32BitWorkerProcess = false,
            },
            IsHttpsOnly = true,
        };

        var webAppOperation = await webSites.CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            webAppName,
            webAppData
        );

        var webApp = webAppOperation.Value;

        // Otherwise, Stack is not set. See also https://github.com/christophwille/AppServiceCurrentStackViaAzureSdk/blob/main/Program.cs
        var metadataResult = await webApp.GetMetadataAsync();
        var metadata = metadataResult.Value;

        metadata.Properties.Add("CURRENT_STACK", "dotnet");
        await webApp.UpdateMetadataAsync(metadata);

        await EnableScmBasicAuth(webApp);

        return webApp;
    }
}

static async Task EnableScmBasicAuth(WebSiteResource webApp)
{
    Console.WriteLine("Enabling SCM Basic Auth Publishing Credentials...");

    try
    {
        // Get the basic publishing credentials policy resource
        var scmPolicy = await webApp.GetScmSiteBasicPublishingCredentialsPolicy().GetAsync();

        // Update to allow SCM basic auth
        var policyData = new CsmPublishingCredentialsPoliciesEntityData
        {
            Allow = true,
        };

        await webApp.GetScmSiteBasicPublishingCredentialsPolicy()
            .CreateOrUpdateAsync(Azure.WaitUntil.Completed, policyData);

        Console.WriteLine("SCM Basic Auth enabled successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not enable SCM Basic Auth: {ex.Message}");
        throw;
    }
}

static async Task ConfigureAppSettings(WebSiteResource webApp)
{
    Console.WriteLine("Configuring application settings...");

    var settings = new Dictionary<string, string>
    {
        { "ConnectionStrings__DefaultConnection", "your-db-connection-string" },
    };

    var appSettings = new AppServiceConfigurationDictionary
    {
        Properties = { }
    };

    foreach (var setting in settings)
    {
        appSettings.Properties.Add(setting.Key, setting.Value);
    }

    await webApp.UpdateApplicationSettingsAsync(appSettings);
    Console.WriteLine("Application settings configured successfully.");
}

static async Task DeployApplication(WebSiteResource webApp, string publishPath)
{
    Console.WriteLine("Deploying application...");

    // Create ZIP package
    var zipPath = Path.Combine(Path.GetTempPath(), $"deploy-{Guid.NewGuid()}.zip");

    try
    {
        // Zip the publish directory
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(publishPath, zipPath);
        Console.WriteLine($"Created deployment package: {zipPath}");

        // Get publishing credentials
        var credentials = await webApp.GetPublishingCredentialsAsync(Azure.WaitUntil.Completed);
        var publishingUsername = credentials.Value.Data.PublishingUserName;
        var publishingPassword = credentials.Value.Data.PublishingPassword;

        // Deploy using Kudu ZIP API
        using var httpClient = new HttpClient();
        var base64Auth = Convert.ToBase64String(
            System.Text.Encoding.ASCII.GetBytes($"{publishingUsername}:{publishingPassword}")
        );
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);

        var kuduUrl = $"https://{webApp.Data.Name}.scm.azurewebsites.net/api/zipdeploy";

        using var fileStream = File.OpenRead(zipPath);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");

        var response = await httpClient.PostAsync(kuduUrl, streamContent);
        response.EnsureSuccessStatusCode();

        Console.WriteLine("Application deployed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during deployment: {ex.Message}");
    }
    finally
    {
        // Cleanup
        if (File.Exists(zipPath))
            File.Delete(zipPath);
    }
}