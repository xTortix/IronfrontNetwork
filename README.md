# Ironfront Network

Backend and online-service infrastructure for **Ironfront**, my personal multiplayer tank-game prototype.

This repository is the server-side half of the project: public API gateway, authentication, persistent player data, data-driven game catalogs, matchmaking and the control plane used to coordinate dedicated BattleServers.

> **Project status:** active prototype / learning project. This is not presented as production-ready MMO infrastructure; it documents the architecture I am actively building and iterating on.

## Architecture

```text
                         Internet
                            │
                         HTTPS
                            │
                         Caddy
                            │
                            ▼
                    Ironfront.Gateway
                    public API boundary
                      /      |      \
                     /       |       \
                    ▼        ▼        ▼
          AuthService   UserService   MatchmakingService
              │             │              │
              ▼             ▼              ▼
          PostgreSQL    PostgreSQL      PostgreSQL
                                             │
                                             ▼
                                  BattleServer registry /
                                  commands / join tickets
                                             │
                                             ▼
                                   dedicated BattleServer
                                   (separate project)
```

The Unity client talks to the **Gateway only**. Internal services bind to loopback interfaces and use an internal service key where cross-service authorization is required.

## Implemented areas

### Gateway

- public API entry point for the game client
- JWT validation and authenticated player endpoints
- request forwarding to internal services
- hangar, deck, tech-tree and matchmaking API surfaces
- game-data manifest/endpoints
- health and dependency status endpoints

### Authentication

- account registration and login
- PostgreSQL persistence through Entity Framework Core
- PBKDF2-SHA256 password hashing with per-password random salt
- signed JWT access tokens
- refresh-session persistence groundwork for token rotation

### User / progression service

- idempotent user provisioning after account creation/login
- persistent player profile
- owned vehicles / hangar state
- decks and deck slots
- active deck selection
- tech-tree research state and progression
- data-driven starter grants

### Matchmaking / BattleServer control plane

- persistent matchmaking queues
- battle-mode-specific rules loaded from game-data catalogs
- match creation and lifecycle state
- BattleServer registration / slot tracking
- command inbox for BattleServer orchestration
- short-lived battle join tickets
- reconciliation/background services for stale server/match state

### Shared game data

JSON catalogs define data that multiple services need to agree on:

- vehicles
- tech trees
- battle modes / matchmaking rules

The catalogs are validated and loaded through dedicated domain/infrastructure projects instead of being embedded directly into API handlers.

## Solution layout

```text
src/
├── Ironfront.Gateway
├── Ironfront.AuthService
├── Ironfront.Shared
├── Ironfront.GameData
├── Ironfront.GameData.Infrastructure
├── Ironfront.UserService.Domain
├── Ironfront.UserService.Application
├── Ironfront.UserService.Infrastructure
├── Ironfront.UserService
├── Ironfront.Matchmaking.Domain
├── Ironfront.Matchmaking.Application
├── Ironfront.Matchmaking.Infrastructure
└── Ironfront.MatchmakingService

tools/
└── Ironfront.Tools.NamecheapDnsUpdater

game-data/
├── vehicles.catalog.json
├── tech-trees.catalog.json
└── battle-modes.catalog.json

scripts/linux/                 publish / migration / deployment scripts
deploy/IronfrontNetwork/      deployment templates only
```

The User and Matchmaking subsystems are split into **Domain / Application / Infrastructure / Host** projects. The goal is to keep persistence and hosting details out of core domain/application logic and make service boundaries explicit.

## Technology

- C# / .NET 9
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL / Npgsql
- JWT bearer authentication
- Linux / systemd
- Caddy reverse proxy
- JSON data catalogs
- GitHub Actions build validation

## Configuration and secrets

Runtime credentials are intentionally supplied through environment variables. Real environment files, certificates, private keys, local databases and generated deployment output are excluded by `.gitignore`.

Use this template as the starting point:

```text
deploy/IronfrontNetwork/env/ironfront.env.example
```

Important variables include:

```text
IRONFRONT_AUTH_DB_CONNECTION
IRONFRONT_USER_DB_CONNECTION
IRONFRONT_MATCHMAKING_DB_CONNECTION
IRONFRONT_JWT_SIGNING_KEY
IRONFRONT_JWT_ISSUER
IRONFRONT_JWT_AUDIENCE
IRONFRONT_INTERNAL_SERVICE_KEY
IRONFRONT_BATTLE_SERVER_CONTROL_KEY
IRONFRONT_AUTHSERVICE_URL
IRONFRONT_USERSERVICE_URL
IRONFRONT_MATCHMAKING_URL
IRONFRONT_GAME_DATA_DIRECTORY
```

**Never commit a real `ironfront.local.env`, production connection string, signing key or server-control key.**

## Local development

Requires the .NET 9 SDK and PostgreSQL instances/databases for the services that use persistence.

```bash
dotnet restore IronfrontNetwork.sln
dotnet build IronfrontNetwork.sln
```

Create a local environment file from the example, fill in development-only credentials, then load it into your shell:

```bash
cp deploy/IronfrontNetwork/env/ironfront.env.example \
   deploy/IronfrontNetwork/env/ironfront.local.env

set -a
source deploy/IronfrontNetwork/env/ironfront.local.env
set +a
```

Run services individually, for example:

```bash
dotnet run --project src/Ironfront.AuthService
dotnet run --project src/Ironfront.UserService
dotnet run --project src/Ironfront.MatchmakingService
IRONFRONT_GATEWAY_HOSTING_MODE=ReverseProxy \
  dotnet run --project src/Ironfront.Gateway
```

## Linux deployment

The repository contains scripts for publishing framework-dependent Linux binaries, creating EF Core migration bundles, packaging a release and applying it on a Debian server.

```bash
./scripts/linux/publish-linux-deploy.sh
./scripts/linux/deploy-to-homeserver.sh
```

Real server configuration lives outside the application directory:

```text
/opt/warlabs/ironfront/       application files
/etc/warlabs/ironfront/       runtime secrets/config
/var/lib/warlabs/ironfront/   persistent application data
/var/log/warlabs/ironfront/   logs
```

Only deployment **templates** are versioned. Published binaries, migration executables, local env files and packaged releases are generated artifacts and are ignored by Git.

## Repository scope

The Unity game client and the dedicated BattleServer are separate projects. This repository focuses only on the online/backend layer and shared contracts required to connect those components.
