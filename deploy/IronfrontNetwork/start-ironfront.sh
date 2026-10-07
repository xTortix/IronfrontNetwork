#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${IRONFRONT_ENV_FILE:-/etc/warlabs/ironfront/ironfront.env}"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: Environment file not found: $ENV_FILE"
    echo "Create it from:"
    echo "$ROOT/env/ironfront.env.example"
    exit 1
fi

for assembly in \
    "$ROOT/authservice/Ironfront.AuthService.dll" \
    "$ROOT/userservice/Ironfront.UserService.dll" \
    "$ROOT/matchmaking/Ironfront.MatchmakingService.dll" \
    "$ROOT/gateway/Ironfront.Gateway.dll"
do
    if [[ ! -f "$assembly" ]]; then
        echo "ERROR: Publish output not found: $assembly"
        exit 1
    fi
done

for catalog in \
    "$ROOT/game-data/vehicles.catalog.json" \
    "$ROOT/game-data/tech-trees.catalog.json" \
    "$ROOT/game-data/battle-modes.catalog.json"
do
    if [[ ! -f "$catalog" ]]; then
        echo "ERROR: Game-data catalog not found: $catalog"
        exit 1
    fi
done

set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

cleanup() {
    echo "Stopping Ironfront services..."

    [[ -n "${GATEWAY_PID:-}" ]] &&
        kill "$GATEWAY_PID" 2>/dev/null || true

    [[ -n "${MATCHMAKING_PID:-}" ]] &&
        kill "$MATCHMAKING_PID" 2>/dev/null || true

    [[ -n "${USER_PID:-}" ]] &&
        kill "$USER_PID" 2>/dev/null || true

    [[ -n "${AUTH_PID:-}" ]] &&
        kill "$AUTH_PID" 2>/dev/null || true
}

trap cleanup EXIT INT TERM

echo "Starting Ironfront.AuthService..."
(
    cd "$ROOT/authservice"
    dotnet Ironfront.AuthService.dll
) &
AUTH_PID=$!

sleep 2

echo "Starting Ironfront.UserService..."
(
    cd "$ROOT/userservice"
    dotnet Ironfront.UserService.dll
) &
USER_PID=$!

sleep 2

echo "Starting Ironfront.MatchmakingService..."
(
    cd "$ROOT/matchmaking"
    dotnet Ironfront.MatchmakingService.dll
) &
MATCHMAKING_PID=$!

sleep 2

echo "Starting Ironfront.Gateway..."
(
    cd "$ROOT/gateway"
    dotnet Ironfront.Gateway.dll
) &
GATEWAY_PID=$!

echo
echo "Ironfront Network started."
echo "AuthService:        http://127.0.0.1:${IRONFRONT_AUTHSERVICE_PORT:-5010}/internal/health"
echo "UserService:        http://127.0.0.1:${IRONFRONT_USERSERVICE_PORT:-5020}/internal/health"
echo "MatchmakingService: http://127.0.0.1:${IRONFRONT_MATCHMAKING_PORT:-5030}/control/v1/health"
echo "Gateway:            http://127.0.0.1:5000/api/health"
echo
echo "Press Ctrl+C to stop."
wait
