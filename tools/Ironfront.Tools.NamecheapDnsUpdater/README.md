# Ironfront.Tools.NamecheapDnsUpdater

This tool updates the Dynamic DNS record for the Ironfront Gateway domain on Namecheap.

The intended public development domain is:

```text
gateway.warlabs.net
```

## Purpose

If the server is hosted from a home network, the public IPv4 address may change.

This tool detects the current public IPv4 address and sends an update request to Namecheap Dynamic DNS.

## Required Namecheap setup

In Namecheap, create a Dynamic DNS record:

```text
Type:  A + Dynamic DNS Record
Host:  gateway
Domain: warlabs.net
```

This results in:

```text
gateway.warlabs.net
```

Dynamic DNS must be enabled in the Namecheap domain settings.

Use the Namecheap Dynamic DNS password, not the normal Namecheap account password.

## Environment variables

The tool reads its configuration from environment variables.

Temporary PowerShell variables:

```powershell
$env:IRONFRONT_DDNS_DOMAIN = "warlabs.net"
$env:IRONFRONT_DDNS_HOST = "gateway"
$env:IRONFRONT_DDNS_PASSWORD = "your-namecheap-dynamic-dns-password"
```

Persistent Windows user environment variables:

```powershell
setx IRONFRONT_DDNS_DOMAIN "warlabs.net"
setx IRONFRONT_DDNS_HOST "gateway"
setx IRONFRONT_DDNS_PASSWORD "your-namecheap-dynamic-dns-password"
```

Important: `setx` stores the values in the Windows user environment. These values are not stored in the project folder or Git repository, but they are also not encrypted.

## Running the tool

From the solution folder:

```powershell
dotnet run --project tools\Ironfront.Tools.NamecheapDnsUpdater
```

Or with explicit command line arguments:

```powershell
dotnet run --project tools\Ironfront.Tools.NamecheapDnsUpdater -- --host gateway --domain warlabs.net --password "your-namecheap-dynamic-dns-password"
```

Using environment variables is preferred, because it keeps secrets out of source code and command history.

## Security note

Never commit real passwords, API keys or certificates to Git.

Recommended:

```text
Secrets       → Environment variables
Certificates → Local certs folder, ignored by Git
Code          → Safe to commit
```
