# Ironfront.UserService

Internal persistent player-state service. It owns player provisioning, hangar/vehicle ownership, deck state and tech-tree progression.

The service binds to loopback and authenticates internal requests with `IRONFRONT_INTERNAL_SERVICE_KEY`.

```bash
set -a
source deploy/IronfrontNetwork/env/ironfront.local.env
set +a

dotnet run --project src/Ironfront.UserService
```
