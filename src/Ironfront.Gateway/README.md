# Ironfront.Gateway

Public API boundary for the Ironfront backend.

The client communicates with the Gateway; the Gateway validates JWTs and forwards work to the internal Auth, User and Matchmaking services. Business/persistence logic should remain in those services rather than accumulating in the public API layer.

For production-style hosting the Gateway binds to loopback on port `5000` and sits behind Caddy:

```text
Client -> HTTPS -> Caddy -> http://127.0.0.1:5000 -> Gateway
```

```bash
set -a
source deploy/IronfrontNetwork/env/ironfront.local.env
set +a

export IRONFRONT_GATEWAY_HOSTING_MODE=ReverseProxy
dotnet run --project src/Ironfront.Gateway
```
