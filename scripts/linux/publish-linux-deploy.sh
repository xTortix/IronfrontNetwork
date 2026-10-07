#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DEPLOY_ROOT="$ROOT/deploy/IronfrontNetwork"
ENV_FILE="${IRONFRONT_ENV_FILE:-$DEPLOY_ROOT/env/ironfront.local.env}"

CONFIGURATION="Release"
RUNTIME="linux-x64"
SELF_CONTAINED="false"

printf '\n==========================================\n'
printf 'Ironfront Network - Linux Deploy Publish\n'
printf '==========================================\n'
printf 'Root:        %s\n' "$ROOT"
printf 'DeployRoot:  %s\n' "$DEPLOY_ROOT"
printf 'Runtime:     %s\n' "$RUNTIME"
printf 'Config:      %s\n' "$CONFIGURATION"
printf '\n'

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet was not found. Install the .NET SDK on the development machine."
    exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: Local environment file was not found:"
    echo "$ENV_FILE"
    echo
    echo "Set IRONFRONT_ENV_FILE to another path if necessary."
    exit 1
fi

if [[ ! -f "$ROOT/game-data/vehicles.catalog.json" ]]; then
    echo "ERROR: Vehicle catalog was not found:"
    echo "$ROOT/game-data/vehicles.catalog.json"
    exit 1
fi

if [[ ! -f "$ROOT/game-data/tech-trees.catalog.json" ]]; then
    echo "ERROR: Tech tree catalog was not found:"
    echo "$ROOT/game-data/tech-trees.catalog.json"
    exit 1
fi

if [[ ! -f "$ROOT/game-data/battle-modes.catalog.json" ]]; then
    echo "ERROR: Battle mode catalog was not found:"
    echo "$ROOT/game-data/battle-modes.catalog.json"
    exit 1
fi

set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

rm -rf \
    "$DEPLOY_ROOT/authservice" \
    "$DEPLOY_ROOT/userservice" \
    "$DEPLOY_ROOT/gateway" \
    "$DEPLOY_ROOT/game-data" \
    "$DEPLOY_ROOT/matchmaking" \
    "$DEPLOY_ROOT/migrations"

mkdir -p \
    "$DEPLOY_ROOT/authservice" \
    "$DEPLOY_ROOT/userservice" \
    "$DEPLOY_ROOT/gateway" \
    "$DEPLOY_ROOT/game-data" \
    "$DEPLOY_ROOT/matchmaking" \
    "$DEPLOY_ROOT/migrations"

printf '\nPublishing Ironfront.AuthService...\n'
dotnet publish \
    "$ROOT/src/Ironfront.AuthService/Ironfront.AuthService.csproj" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained "$SELF_CONTAINED" \
    -o "$DEPLOY_ROOT/authservice"

printf '\nPublishing Ironfront.UserService...\n'
dotnet publish \
    "$ROOT/src/Ironfront.UserService/Ironfront.UserService.csproj" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained "$SELF_CONTAINED" \
    -o "$DEPLOY_ROOT/userservice"

printf '\nPublishing Ironfront.MatchmakingService...\n'
dotnet publish \
    "$ROOT/src/Ironfront.MatchmakingService/Ironfront.MatchmakingService.csproj" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained "$SELF_CONTAINED" \
    -o "$DEPLOY_ROOT/matchmaking"

printf '\nPublishing Ironfront.Gateway...\n'
dotnet publish \
    "$ROOT/src/Ironfront.Gateway/Ironfront.Gateway.csproj" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained "$SELF_CONTAINED" \
    -o "$DEPLOY_ROOT/gateway"

printf '\nCopying game-data catalogs...\n'

install -m 0644 \
    "$ROOT/game-data/vehicles.catalog.json" \
    "$DEPLOY_ROOT/game-data/vehicles.catalog.json"

install -m 0644 \
    "$ROOT/game-data/tech-trees.catalog.json" \
    "$DEPLOY_ROOT/game-data/tech-trees.catalog.json"

install -m 0644 \
    "$ROOT/game-data/battle-modes.catalog.json" \
    "$DEPLOY_ROOT/game-data/battle-modes.catalog.json"

printf '\nCreating AuthService migration bundle...\n'
"$ROOT/scripts/linux/create-auth-migration-bundle.sh"

printf '\nCreating UserService migration bundle...\n'
"$ROOT/scripts/linux/create-user-migration-bundle.sh"

printf '\nCreating MatchmakingService migration bundle...\n'
"$ROOT/scripts/linux/create-matchmaking-migration-bundle.sh"

chmod +x "$DEPLOY_ROOT/start-ironfront.sh" 2>/dev/null || true

printf '\n==========================================\n'
printf 'Linux deploy publish completed successfully.\n'
printf '==========================================\n'
printf 'Deploy folder:\n%s\n\n' "$DEPLOY_ROOT"

printf 'Included runtime services:\n'
printf '%s\n' \
    '  - AuthService' \
    '  - UserService' \
    '  - MatchmakingService' \
    '  - Gateway' \
    '  - game-data/vehicles.catalog.json' \
    '  - game-data/tech-trees.catalog.json' \
    '  - game-data/battle-modes.catalog.json' \
    '  - AuthService migration bundle' \
    '  - UserService migration bundle' \
    '  - MatchmakingService migration bundle'
printf '\n'
