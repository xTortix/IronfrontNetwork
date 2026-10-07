# Deploy Folder

This folder contains **templates and operator files** for the Linux deployment of Ironfront Network.

Generated runtime binaries are deliberately not committed. `publish-linux-deploy.sh` creates them locally when preparing a release.

## Versioned here

```text
deploy/IronfrontNetwork/
├── Caddy/Caddyfile
├── env/ironfront.env.example
├── systemd/
│   ├── ironfront-auth.service
│   ├── ironfront-user.service
│   ├── ironfront-matchmaking.service
│   └── ironfront-gateway.service
└── start-ironfront.sh
```

## Generated and ignored

```text
authservice/
userservice/
matchmaking/
gateway/
game-data/
migrations/
namecheap-ddns/
deploy/ironfront-deploy-*.tar.gz
```

Real secrets belong outside the repository:

```text
/etc/warlabs/ironfront/ironfront.env
```

Publish and package from the repository root:

```bash
./scripts/linux/publish-linux-deploy.sh
./scripts/linux/deploy-to-homeserver.sh
```
