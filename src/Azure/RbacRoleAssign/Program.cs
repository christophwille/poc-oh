using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using RbacRoleAssign;

ArmClient armClient = new ArmClient(new VisualStudioCredential());

// Configuration
var subscriptionId = "your-subscription-id-here";
var resourceGroupName = "rg-rbac-testing";
var stgAccountName = "stgcw987654321a";
var kvName = "kvcw987654321a";
var location = AzureLocation.WestEurope;
var upnToAssign = "your-domain-account-here";

var subscription = await armClient.GetSubscriptionResource(
    new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}")
).GetAsync();

// First, grab the subscription and pre-existing resource group
var resourceGroups = subscription.Value.GetResourceGroups();
var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

// Next, get the object id of the user to assign the role to
var principalId = await MsGraph.GetPrincipalIdFromUpnAsync(upnToAssign);

// Management example Storage: https://github.com/Azure-Samples/azure-samples-net-management/blob/master/samples/storage/create-storage-account/Program.cs
// Management example Key Vault: https://github.com/Azure-Samples/azure-samples-net-management/blob/master/samples/keyvault/manage-key-vault/Program.cs
// Rbac roles: https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles

Console.WriteLine($"Creating storage account '{stgAccountName}' in resource group '{resourceGroupName}'...");
await CreateStorageAccountAsync();

Console.WriteLine($"\nCreating key vault '{kvName}' in resource group '{resourceGroupName}'...");
await CreateKeyVaultAsync();

async Task CreateStorageAccountAsync()
{
    var storageAccountCollection = resourceGroup.Value.GetStorageAccounts();

    var storageAccountData = new StorageAccountCreateOrUpdateContent(
        new StorageSku(StorageSkuName.StandardLrs),
        StorageKind.StorageV2,
        location)
    {
        AccessTier = StorageAccountAccessTier.Hot
    };

    var storageAccountOperation = await storageAccountCollection.CreateOrUpdateAsync(
        Azure.WaitUntil.Completed,
        stgAccountName,
        storageAccountData);

    var storageAccount = storageAccountOperation.Value;
    Console.WriteLine($"Storage account created: {storageAccount.Id}");

    // Assign role to the storage account
    Console.WriteLine($"Assigning 'Storage Blob Data Contributor' role to '{upnToAssign}'...");

    // Get the role definition for "Storage Blob Data Contributor"
    // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/storage#storage-blob-data-contributor
    var roleDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe";

    var roleAssignmentCollection = storageAccount.GetRoleAssignments();

    // Create a unique GUID for the role assignment
    var roleAssignmentName = Guid.NewGuid().ToString();

    var roleAssignmentData = new RoleAssignmentCreateOrUpdateContent(
        new ResourceIdentifier(roleDefinitionId),
        principalId);

    var roleAssignmentOperation = await roleAssignmentCollection.CreateOrUpdateAsync(
        Azure.WaitUntil.Completed,
        roleAssignmentName,
        roleAssignmentData);

    Console.WriteLine($"Role assignment created: {roleAssignmentOperation.Value.Id}");
    Console.WriteLine("Done!");
}

async Task CreateKeyVaultAsync()
{
    var keyVaultCollection = resourceGroup.Value.GetKeyVaults();

    var tenantId = Guid.Parse(subscription.Value.Data.TenantId.ToString());

    var keyVaultProperties = new KeyVaultProperties(tenantId, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
    {
        EnableRbacAuthorization = true, // Enable RBAC for role assignments
        EnableSoftDelete = true,
        SoftDeleteRetentionInDays = 90
    };

    var keyVaultData = new KeyVaultCreateOrUpdateContent(location, keyVaultProperties);

    var keyVaultOperation = await keyVaultCollection.CreateOrUpdateAsync(
   Azure.WaitUntil.Completed,
        kvName,
        keyVaultData);

    var keyVault = keyVaultOperation.Value;
    Console.WriteLine($"Key vault created: {keyVault.Id}");

    // Assign role to the key vault
    Console.WriteLine($"Assigning 'Key Vault Secrets Officer' role to '{upnToAssign}'...");

    // Get the role definition for "Key Vault Secrets Officer"
    // https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/security#key-vault-secrets-officer
    var roleDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/b86a8fe4-44ce-4948-aee5-eccb2c155cd7";

    var roleAssignmentCollection = keyVault.GetRoleAssignments();

    // Create a unique GUID for the role assignment
    var roleAssignmentName = Guid.NewGuid().ToString();

    var roleAssignmentData = new RoleAssignmentCreateOrUpdateContent(
      new ResourceIdentifier(roleDefinitionId),
        principalId);

    var roleAssignmentOperation = await roleAssignmentCollection.CreateOrUpdateAsync(
        Azure.WaitUntil.Completed,
        roleAssignmentName,
        roleAssignmentData);

    Console.WriteLine($"Role assignment created: {roleAssignmentOperation.Value.Id}");
    Console.WriteLine("Key vault setup complete!");
}