# Ironfront.AuthService

Internal authentication service for Ironfront. It owns account credentials, password hashing, access-token issuance and authentication persistence.

The service binds to loopback and is intended to be reached through `Ironfront.Gateway`, not directly by public clients.

```bash
set -a
source deploy/IronfrontNetwork/env/ironfront.local.env
set +a

dotnet run --project src/Ironfront.AuthService
```
