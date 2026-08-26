# Local development only: private CA, server cert and one partner client cert in ./certs.
# Paste the printed SHA-256 fingerprint into appsettings.json -> Mtls:Partners, then `dotnet run`.
# Smoke test: Invoke-WebRequest https://localhost:5443/TodoService.svc?wsdl -Certificate (Get-PfxCertificate certs/contoso.pfx) -SkipCertificateCheck
$ErrorActionPreference = 'Stop'
$dir = Join-Path $PSScriptRoot 'certs'
New-Item -ItemType Directory -Force $dir | Out-Null
$password = ConvertTo-SecureString 'dev' -AsPlainText -Force

$ca = New-SelfSignedCertificate -Subject 'CN=Sample Todo Dev CA' -KeyUsage CertSign, CRLSign -KeyExportPolicy Exportable `
    -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5) -TextExtension '2.5.29.19={critical}{text}ca=1'
Export-Certificate -Cert $ca -FilePath (Join-Path $dir 'ca.cer') | Out-Null

$server = New-SelfSignedCertificate -Subject 'CN=localhost' -DnsName localhost -Signer $ca -KeyExportPolicy Exportable `
    -CertStoreLocation Cert:\CurrentUser\My -TextExtension '2.5.29.37={text}1.3.6.1.5.5.7.3.1'
Export-PfxCertificate -Cert $server -FilePath (Join-Path $dir 'server.pfx') -Password $password | Out-Null

$client = New-SelfSignedCertificate -Subject 'CN=Contoso' -Signer $ca -KeyExportPolicy Exportable `
    -CertStoreLocation Cert:\CurrentUser\My -TextExtension '2.5.29.37={text}1.3.6.1.5.5.7.3.2'
Export-PfxCertificate -Cert $client -FilePath (Join-Path $dir 'contoso.pfx') -Password $password | Out-Null

# Keys now live in the files; do not leave them in the user store.
$ca, $server, $client | ForEach-Object { Remove-Item "Cert:\CurrentUser\My\$($_.Thumbprint)" }

$sha256 = $client.GetCertHashString([System.Security.Cryptography.HashAlgorithmName]::SHA256)
"Contoso client cert SHA-256: $sha256"
