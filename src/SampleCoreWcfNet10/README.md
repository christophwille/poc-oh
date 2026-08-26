# SampleCoreWcfNet10 — CoreWCF on .NET 10 with mutual TLS

A minimal SOAP TODO service built with [CoreWCF](https://github.com/CoreWCF/CoreWCF), secured with
client certificates (mTLS), plus an integration test project that exercises the real endpoint over
real TLS with a real WCF client.

```
SampleCoreWcfNet10/          the service (ASP.NET Core host + CoreWCF)
SampleCoreWcfNet10.Tests/    xunit v3 integration tests on Microsoft.Testing.Platform
```

## Quick start

```powershell
cd SampleCoreWcfNet10
.\New-DevCerts.ps1                 # creates certs/ca.cer, certs/server.pfx, certs/contoso.pfx
# paste the printed SHA-256 fingerprint into appsettings.json -> Mtls:Partners[0].Sha256Thumbprint
dotnet run                         # https://localhost:5443/TodoService.svc

# smoke test
Invoke-WebRequest https://localhost:5443/TodoService.svc?wsdl -SkipCertificateCheck `
    -Certificate (Get-PfxCertificate certs\contoso.pfx)
```

```powershell
dotnet test                        # from the solution folder
```

## Scenario

- B2B: the consumer belongs to a different organisation (no shared AD, no shared identity provider).
- The consumer is an older application: a classic SOAP client (WCF, .NET Framework, Java, …) that can
  neither obtain OAuth tokens nor easily add custom HTTP headers.
- Storage is in-memory; the point of the sample is the hosting and security setup, not persistence.

## Security choices

### Options considered

| Option | Verdict |
|---|---|
| **Client certificate (mTLS)** | **Chosen.** Happens at the TLS layer, below SOAP. Every HTTP stack of the last 20 years can present a client certificate; the SOAP envelope stays plain. |
| UserName over TLS (`TransportWithMessageCredential`) | Fallback if a partner cannot handle certificates. Puts a WS-Security `UsernameToken` into the envelope — fine WCF-to-WCF, hit-or-miss for other stacks (timestamps, `mustUnderstand`, and with `wsHttpBinding` also WS-Addressing). |
| JWT / bearer via ASP.NET Core auth | Best for greenfield, but an old client cannot fetch tokens or attach headers. |
| Windows / Negotiate (Kerberos, NTLM) | Intranet only; no shared domain across organisations. |
| WS-Federation / WS-Trust | Not supported by CoreWCF. |

Wider interop therefore meant **mTLS**, not message security.

### How mTLS is implemented

The layers, from the wire upwards (`SampleCoreWcfNet10/Program.cs`):

1. **Kestrel — `ClientCertificateMode.RequireCertificate`**
   The TLS handshake fails without a client certificate; unauthenticated traffic never reaches
   application code. `AllowAnyClientCertificate()` is set on purpose: Kestrel only knows the OS trust
   store, so chain validation is delegated to the next layer.

2. **ASP.NET Core certificate authentication — pinned private CA**
   `Microsoft.AspNetCore.Authentication.Certificate` with
   - `CertificateTypes.Chained` — self-signed certificates are rejected,
   - `X509ChainTrustMode.CustomRootTrust` with the partner CA from `Mtls:CaCertificatePath` as the
     *only* trusted root — certificates from public CAs or the machine store do not count,
   - `RevocationMode = NoCheck` — the private CA publishes no CRL/OCSP; revocation is handled by the
     allow-list below.

3. **Explicit allow-list — `Mtls:Partners`**
   Being issued by the right CA is necessary but not sufficient. `OnCertificateValidated` looks up the
   certificate's **SHA-256** fingerprint in configuration; unknown certificates fail authentication
   (HTTP 401). A match produces a `ClaimsPrincipal` carrying the partner name and fingerprint, so
   revoking a partner is a config change, and the service knows who is calling.

4. **CoreWCF — `HttpClientCredentialType.InheritedFromHost`**
   The binding is `BasicHttpBinding(BasicHttpSecurityMode.Transport)` — plain SOAP 1.1 over HTTPS,
   the most interoperable choice. `InheritedFromHost` tells CoreWCF not to do its own authentication
   and to trust `HttpContext.User` from the ASP.NET Core pipeline.

5. **Authorization — `[Authorize]` on the service class**
   CoreWCF honours the standard ASP.NET Core `IAuthorizeData` attributes; every operation requires an
   authenticated (= allow-listed) caller. Per-partner policies can be added the usual ASP.NET Core way.

Other hardening in the sample:

- `MtlsOptions` is validated with data annotations and `ValidateOnStart()` — a missing CA path or an
  empty allow-list fails startup instead of silently opening or closing the service.
- `IncludeExceptionDetailInFaults` is enabled only in the Development environment.
- The service and data contracts carry an explicit, versioned XML namespace (`urn:sample:todo:v1`)
  and ordered `DataMember`s, so the WSDL partners generate proxies from is stable.
- WSDL (`?wsdl`) is served over HTTPS only and sits behind mTLS like everything else.
- `New-DevCerts.ps1` removes the generated keys from the user certificate store; `certs/` is gitignored.

### What a partner needs

- A client certificate issued by the CA configured in `Mtls:CaCertificatePath` (with the
  *clientAuth* EKU), and its SHA-256 fingerprint registered in `Mtls:Partners`.
- A SOAP client using `basicHttpBinding` with transport security and that certificate as client
  credential — for WCF: `HttpClientCredentialType.Certificate`.

## Integration tests

The tests deliberately avoid `TestServer`: it has no TLS, so it cannot exercise the security setup.
Instead `TodoServiceFixture` uses .NET 10's `WebApplicationFactory<Program>.UseKestrel()` to host the
real application on a real socket, configured only through the same configuration keys production
uses (`Kestrel:*`, `Mtls:*`).

- `TestCertificates` builds a throw-away PKI in memory per run: CA, server certificate, an
  allow-listed partner certificate, a CA-issued but *not* allow-listed certificate, and a certificate
  from a foreign CA.
- `ITodoServiceClient` is a client-side mirror of the contract (what a partner would get from
  `svcutil`), not a reference to the server types — the tests prove wire compatibility.
- The WCF client pins the test server certificate instead of trusting the machine store.

Covered: full TODO lifecycle, typed `FaultContract`, validation fault, no certificate (handshake
fails), CA-issued-but-unlisted certificate and foreign-CA certificate (both rejected).

## Per-partner authorization (not implemented)

Today every allow-listed partner may call every operation: `[Authorize]` on `TodoService` only
requires an authenticated caller. If partners need different permissions (e.g. one may only read),
extend the existing pipeline rather than adding a second one — the certificate handler already turns
the certificate into a `ClaimsPrincipal`, so roles are just more claims on it.

1. **Configuration** — give each partner its roles (`Security/MtlsOptions.cs`):

   ```csharp
   public sealed class Partner
   {
       [Required] public string Name { get; set; } = "";
       [Required] public string Sha256Thumbprint { get; set; } = "";
       public List<string> Roles { get; set; } = [];
   }
   ```

   ```json
   "Partners": [
     { "Name": "Contoso",  "Sha256Thumbprint": "…", "Roles": [ "todo:read", "todo:write" ] },
     { "Name": "Fabrikam", "Sha256Thumbprint": "…", "Roles": [ "todo:read" ] }
   ]
   ```

2. **Claims** — emit the roles in `OnCertificateValidated` (`Program.cs`):

   ```csharp
   var claims = new List<Claim>
   {
       new(ClaimTypes.Name, partner.Name),
       new(ClaimTypes.Thumbprint, fingerprint),
   };
   claims.AddRange(partner.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
   context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
   ```

3. **Enforce** — CoreWCF evaluates ASP.NET Core `IAuthorizeData` attributes on the service class *and*
   on individual operations through `IAuthorizationService`, so the standard attribute works
   (`TodoService.cs`):

   ```csharp
   [Authorize]                                  // baseline: must be an allow-listed partner
   public sealed class TodoService(InMemoryTodoStore store) : ITodoService
   {
       [Authorize(Roles = "todo:read")]  public TodoItem[] GetAll() => …;
       [Authorize(Roles = "todo:read")]  public TodoItem? Get(Guid id) => …;
       [Authorize(Roles = "todo:write")] public TodoItem Add(string title) => …;
       [Authorize(Roles = "todo:write")] public TodoItem Complete(Guid id) => …;
       [Authorize(Roles = "todo:write")] public bool Delete(Guid id) => …;
   }
   ```

   Prefer policies over raw role strings once there is more than one rule:

   ```csharp
   builder.Services.AddAuthorizationBuilder()
       .AddPolicy("TodoWriter", p => p.RequireRole("todo:write"));
   // [Authorize(Policy = "TodoWriter")]
   ```

   CoreWCF also ships its own `[CoreWCF.AuthorizeRole("todo:write")]`, which checks
   `IPrincipal.IsInRole`; it works with the same claims, but the ASP.NET Core attribute is preferred
   because it plugs into policies, requirements and handlers you may already have.

4. **Test** — add a partner with read-only roles to `TodoServiceFixture` (a third `UseSetting`
   block for `Mtls:Partners:1:*`, using `Certs.UnknownPartner` or a new certificate) and assert that
   `Add` throws `MessageSecurityException` (HTTP 403) while `GetAll` succeeds.

Skipped in the sample because the scenario has one partner with one permission set; every layer
above already exists, so this is additive configuration, not a redesign.

## Deliberate simplifications

- In-memory store, per process — swap for a database when data must survive a restart.
- Synchronous operations — there is no I/O to await.
- One flat allow-list, no per-partner roles — see [Per-partner authorization](#per-partner-authorization-not-implemented).
