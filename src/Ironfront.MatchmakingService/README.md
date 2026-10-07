# Ironfront.MatchmakingService

Internal matchmaking and BattleServer-control service.

Responsibilities currently include battle queues, match lifecycle/provisioning, BattleServer registration and commands, join-ticket issuance/validation and reconciliation of server/match state.

```bash
set -a
source deploy/IronfrontNetwork/env/ironfront.local.env
set +a

dotnet run --project src/Ironfront.MatchmakingService
```
