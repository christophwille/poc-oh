# Prompt

Given https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview - search the Internet for articles, what is the recommended approach in an on-premises Kubernetes cluster for storing and protecting keys? Think hard, provide options, provide snippets

# Result

Good question — this is a genuine gap in the official docs, which lean heavily on Azure. Here's what the guidance and community articles converge on for **on-prem Kubernetes**, where you have to solve two separate problems: **where to persist** the key ring (shared across all pods) and **how to encrypt** it at rest.

The core constraints, straight from Microsoft's docs: when hosting in a container, keys should be persisted in a volume that outlives the container or in an external provider, and — critically — if you specify an explicit key persistence location, the data protection system deregisters the default key encryption at rest mechanism, so keys are no longer encrypted at rest. It's recommended that you additionally specify an explicit key encryption mechanism for production deployments (DPAPI doesn't exist on Linux). So on-prem you always want `PersistKeysTo...` **plus** `ProtectKeysWith...`.

## Option 1 — Database via EF Core + certificate encryption (most common recommendation)

For on-prem clusters you almost always already have a database (SQL Server/PostgreSQL), and it's inherently shared across pods and backed up. To store keys in a database using EntityFramework, configure the system with the `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` package, and encrypt with an X.509 cert (mounted as a Kubernetes TLS secret, ideally issued by cert-manager):

```csharp
// dotnet add package Microsoft.AspNetCore.DataProtection.EntityFrameworkCore

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}

var cert = X509CertificateLoader.LoadPkcs12FromFile(
    "/certs/dp-cert.pfx",                      // mounted from a k8s Secret
    builder.Configuration["DP_CERT_PASSWORD"]);

builder.Services.AddDataProtection()
    .SetApplicationName("myapp")               // stable across all replicas
    .PersistKeysToDbContext<AppDbContext>()
    .ProtectKeysWithCertificate(cert);
```

Mounting the cert:

```yaml
volumes:
  - name: dp-cert
    secret:
      secretName: dataprotection-cert   # type: kubernetes.io/tls or Opaque with .pfx
containers:
  - name: app
    volumeMounts:
      - name: dp-cert
        mountPath: /certs
        readOnly: true
```

For cert rotation, add `UnprotectKeysWithAnyCertificate` with the old certs so previously encrypted keys remain readable.

## Option 2 — Shared PersistentVolume (RWX) + certificate encryption

The doc's UNC-share pattern translated to k8s: a `ReadWriteMany` PVC (NFS, CephFS, Longhorn RWX) mounted into all replicas:

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("myapp")
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .ProtectKeysWithCertificate(cert);
```

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: dp-keys
spec:
  accessModes: ["ReadWriteMany"]     # must be RWX for multiple replicas
  storageClassName: nfs-client
  resources:
    requests:
      storage: 100Mi
```

Simple, but you need an RWX-capable storage class, and per the doc's warning you must lock down permissions so only the identity under which your web app runs has read, write, and create access to that directory (use `fsGroup`/`securityContext` and a dedicated namespace).

## Option 3 — Redis (with persistence enabled)

Microsoft explicitly lists Redis as a supported external provider, with a caveat: only Redis versions supporting Redis Data Persistence should be used to store keys (AOF/RDB enabled — losing the key ring logs out every user and orphans all encrypted data).

```csharp
// dotnet add package Microsoft.AspNetCore.DataProtection.StackExchangeRedis
var redis = ConnectionMultiplexer.Connect("redis-master.infra.svc:6379");

builder.Services.AddDataProtection()
    .SetApplicationName("myapp")
    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
    .ProtectKeysWithCertificate(cert);
```

Good fit if you already run a persistent Redis for caching/sessions.

## Option 4 — Kubernetes Secret as the key ring (niche)

One community approach mounts a k8s Secret as the keys folder, since a Secret can be mounted as a volume to a container, allowing consumption through the regular `PersistKeysToFileSystem` API, and updates to secrets seamlessly propagate to the applications. But the mount is read-only, so the app can't auto-rotate keys; the same article notes that manually syncing keys back into the cluster might work, but is probably not the solution we are looking for — you'd need a sidecar/operator writing rotated keys back via the k8s API, plus RBAC on the Secret and etcd encryption at rest. Workable, but the most operational effort. If you go this route, pair it with `DisableAutomaticKeyGeneration()` on replicas and one designated key-manager.

## What about encrypting with HashiCorp Vault (the on-prem "Key Vault")?

There's no first-party `ProtectKeysWithHashiCorpVault`. Options are community `IXmlEncryptor` implementations wrapping Vault's Transit engine, or the doc-sanctioned extensibility route — the Data Protection system is extensible enough that it's possible to keep all keys contained within an HSM and provide a custom implementation of the core encryption and decryption routines. A minimal Transit-based encryptor looks like:

```csharp
public class VaultTransitXmlEncryptor : IXmlEncryptor
{
    private readonly IVaultClient _vault; // VaultSharp, k8s auth

    public EncryptedXmlInfo Encrypt(XElement plaintextElement)
    {
        var plaintext = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(plaintextElement.ToString()));
        var result = _vault.V1.Secrets.Transit
            .EncryptAsync("dataprotection", new EncryptRequestOptions
                { Base64EncodedPlainText = plaintext }).GetAwaiter().GetResult();

        var element = new XElement("vaultEncryptedKey",
            new XElement("ciphertext", result.Data.CipherText));
        return new EncryptedXmlInfo(element, typeof(VaultTransitXmlDecryptor));
    }
}

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .Services.Configure<KeyManagementOptions>(o =>
        o.XmlEncryptor = new VaultTransitXmlEncryptor(vaultClient));
```

This gives you the closest analogue to the Azure Key Vault pattern: keys persisted somewhere shared, wrapped by a key that never leaves Vault, with pods authenticating via Kubernetes service-account auth.

## Recommendation summary

For most on-prem clusters, **Option 1 (EF Core database + cert from a k8s Secret managed by cert-manager)** is the pragmatic winner: no RWX storage requirement, transactional writes, backed up with your DB, and works with any replica count. Use Option 2 if you have solid RWX storage and no DB, Option 3 if persistent Redis is already in place, and the Vault Transit route if you have Vault and compliance requirements for centralized key wrapping. Whatever you pick: always call `SetApplicationName`, always pair persistence with a `ProtectKeysWith*` mechanism, and remember the doc's caveat that encrypting keys at rest doesn't prevent attackers with write access from creating new keys — so restrict access to the store itself, not just the encryption.