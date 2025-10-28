using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Web.Deployment;
using System.IO.Compression;

// Copy of https://github.com/christophwille/poc-oh/tree/main/src/Azure/AppSvcScmDeploy

ArmClient armClient = new ArmClient(new VisualStudioCredential());

// Configuration
var subscriptionId = "your-subscription-id-here";
var resourceGroupName = "demo-appservice-wawsdeploy"; // Must exist beforehand
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
        var settingsResponse = await webApp.GetPublishingProfileXmlWithSecretsAsync(new CsmPublishingProfile
        {
            Format = PublishingProfileFormat.WebDeploy,
        });
        string publishSettingsXml;
        using (var reader = new StreamReader(settingsResponse.Value))
        {
            publishSettingsXml = await reader.ReadToEndAsync();
        }

        var publishSettings = new WAWSDeploy.PublishSettings(publishSettingsXml);

        var destinationPath = publishSettings.SiteName;

        // THAT new ALREADY CRASHES
        var sourceBaseOptions = new DeploymentBaseOptions();
        /*
        System.IO.FileNotFoundException
              HResult=0x80070002
              Message=Could not load file or assembly 'System.Configuration.Install, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.
              Source=Microsoft.Web.Deployment
              StackTrace:
               at Microsoft.Web.Deployment.DeploymentBaseOptions..ctor()
               at Microsoft.Web.Deployment.DeploymentBaseOptions..ctor()
               at Program.<<<Main>$>g__DeployApplication|0_4>d.MoveNext() in D:\GitWorkspace\poc-oh\src\Azure\AppSvcWawsDeploy\Program.cs:line 232

              This exception was originally thrown at this call stack:
                Program.<Main>$.__DeployApplication|0_4(Azure.ResourceManager.AppService.WebSiteResource, string) in Program.cs 
        */
        var sourceProvider = DeploymentWellKnownProvider.Package;
        var targetProvider = DeploymentWellKnownProvider.ContentPath;
        var syncOptions = new DeploymentSyncOptions();

        var deploymentBaseOptions = new DeploymentBaseOptions
        {
            ComputerName = publishSettings.ComputerName,
            UserName = publishSettings.Username,
            Password = publishSettings.Password,
            AuthenticationType = "basic"
        };

        using (var deploymentObject = DeploymentManager.CreateObject(sourceProvider, zipPath, sourceBaseOptions))
        {
            // Note: would be nice to have an async flavor of this API...
            DeploymentChangeSummary result = deploymentObject.SyncTo(targetProvider, destinationPath, deploymentBaseOptions, syncOptions);
        }

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