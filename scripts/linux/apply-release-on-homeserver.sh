#!/usr/bin/env bash
set -euo pipefail

STAGE_ROOT="${1:?Missing stage directory argument.}"
TARGET_ROOT="${2:-/opt/warlabs/ironfront}"

ENV_FILE="/etc/warlabs/ironfront/ironfront.env"
BACKUP_PARENT="/opt/warlabs/ironfront-backups"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_ROOT="$BACKUP_PARENT/$TIMESTAMP"

RELEASE_PATHS=(
    authservice
    userservice
    matchmaking
    gateway
    game-data
    migrations
    Caddy
    systemd
    env
    scripts
    start-ironfront.sh
)

require_stage_path() {
    local relative_path="$1"

    if [[ ! -e "$STAGE_ROOT/$relative_path" ]]; then
        echo "ERROR: Deployment stage is missing:"
        echo "$STAGE_ROOT/$relative_path"
        exit 1
    fi
}
require_environment_variable() {
    local variable_name="$1"

    if [[ -z "${!variable_name:-}" ]]; then
        echo "ERROR: Missing required environment variable:"
        echo "$variable_name"
        exit 1
    fi
}

wait_for_http() {
    local url="$1"
    local service_name="$2"
    local attempt

    for attempt in $(seq 1 30); do
        if curl \
            --fail \
            --silent \
            --show-error \
            --max-time 3 \
            "$url" >/dev/null; then
            echo "Health check passed: $service_name"
            return 0
        fi

        sleep 1
    done

    echo "ERROR: Health check failed after 30 seconds:"
    echo "$service_name -> $url"

    return 1
}

if [[ $EUID -ne 0 ]]; then
    echo "Please run this script through sudo."
    exit 1
fi

if [[ ! -d "$STAGE_ROOT" ]]; then
    echo "ERROR: Deployment stage directory does not exist:"
    echo "$STAGE_ROOT"
    exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: Server environment file does not exist:"
    echo "$ENV_FILE"
    echo
    echo "Create it from:"
    echo "$STAGE_ROOT/env/ironfront.env.example"
    exit 1
fi

for required_path in \
    authservice/Ironfront.AuthService.dll \
    userservice/Ironfront.UserService.dll \
    gateway/Ironfront.Gateway.dll \
    game-data/vehicles.catalog.json \
    game-data/tech-trees.catalog.json \
    game-data/battle-modes.catalog.json \
    migrations/authservice-migrate \
    migrations/userservice-migrate \
    systemd/ironfront-auth.service \
    systemd/ironfront-user.service \
    systemd/ironfront-gateway.service \
    matchmaking/Ironfront.MatchmakingService.dll \
    migrations/matchmaking-migrate \
    systemd/ironfront-matchmaking.service \
    scripts/linux/install-systemd-units.sh \
    start-ironfront.sh
do
    require_stage_path "$required_path"
done

set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

for required_variable in \
    IRONFRONT_AUTH_DB_CONNECTION \
    IRONFRONT_USER_DB_CONNECTION \
    IRONFRONT_JWT_SIGNING_KEY \
    IRONFRONT_JWT_ISSUER \
    IRONFRONT_JWT_AUDIENCE \
    IRONFRONT_INTERNAL_SERVICE_KEY \
    IRONFRONT_AUTHSERVICE_PORT \
    IRONFRONT_AUTHSERVICE_URL \
    IRONFRONT_USERSERVICE_PORT \
    IRONFRONT_USERSERVICE_URL \
    IRONFRONT_DEFAULT_STARTER_VEHICLE_ID \
    IRONFRONT_DEFAULT_STARTER_DECK_NAME \
    IRONFRONT_GAME_DATA_DIRECTORY \
    IRONFRONT_MATCHMAKING_DB_CONNECTION \
    IRONFRONT_BATTLE_SERVER_CONTROL_KEY \
    IRONFRONT_MATCHMAKING_PORT \
    IRONFRONT_GATEWAY_HOSTING_MODE
do
    require_environment_variable "$required_variable"
done

if [[ "$IRONFRONT_GAME_DATA_DIRECTORY" != "$TARGET_ROOT/game-data" ]]; then
    echo "ERROR: IRONFRONT_GAME_DATA_DIRECTORY must be:"
    echo "$TARGET_ROOT/game-data"
    echo
    echo "Actual value:"
    echo "$IRONFRONT_GAME_DATA_DIRECTORY"
    exit 1
fi

echo
echo "=========================================="
echo "Ironfront Debian Release Apply"
echo "=========================================="
echo "Stage:       $STAGE_ROOT"
echo "Target:      $TARGET_ROOT"
echo "Backup:      $BACKUP_ROOT"
echo

mkdir -p "$TARGET_ROOT" "$BACKUP_PARENT"

echo "Stopping currently installed services..."
systemctl stop ironfront-gateway.service 2>/dev/null || true
systemctl stop ironfront-user.service 2>/dev/null || true
systemctl stop ironfront-auth.service 2>/dev/null || true
systemctl stop ironfront-matchmaking.service 2>/dev/null || true

mkdir -p "$BACKUP_ROOT"

echo "Backing up current release files..."
for release_path in "${RELEASE_PATHS[@]}"; do
    source_path="$TARGET_ROOT/$release_path"
    backup_path="$BACKUP_ROOT/$release_path"

    if [[ -e "$source_path" ]]; then
        mkdir -p "$(dirname "$backup_path")"
        mv "$source_path" "$backup_path"
    fi
done

echo "Installing new release files..."
for release_path in \
    authservice \
    userservice \
    matchmaking \
    gateway \
    game-data \
    migrations \
    Caddy \
    systemd \
    env \
    scripts
do
    cp -a \
        "$STAGE_ROOT/$release_path" \
        "$TARGET_ROOT/$release_path"
done

install \
    -o root \
    -g root \
    -m 0755 \
    "$STAGE_ROOT/start-ironfront.sh" \
    "$TARGET_ROOT/start-ironfront.sh"

chmod +x \
    "$TARGET_ROOT/migrations/authservice-migrate" \
    "$TARGET_ROOT/migrations/userservice-migrate" \
    "$TARGET_ROOT/migrations/matchmaking-migrate" \
    "$TARGET_ROOT/scripts/linux/install-systemd-units.sh"

chown -R ironfront:ironfront \
    "$TARGET_ROOT/authservice" \
    "$TARGET_ROOT/userservice" \
    "$TARGET_ROOT/matchmaking" \
    "$TARGET_ROOT/gateway" \
    "$TARGET_ROOT/game-data" \
    "$TARGET_ROOT/migrations"

chown -R root:root \
    "$TARGET_ROOT/Caddy" \
    "$TARGET_ROOT/systemd" \
    "$TARGET_ROOT/env" \
    "$TARGET_ROOT/scripts" \
    "$TARGET_ROOT/start-ironfront.sh"

echo "Installing systemd units..."
"$TARGET_ROOT/scripts/linux/install-systemd-units.sh"

set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

echo "Applying AuthService migrations..."
"$TARGET_ROOT/migrations/authservice-migrate" \
    --connection "$IRONFRONT_AUTH_DB_CONNECTION"

echo "Applying UserService migrations..."
"$TARGET_ROOT/migrations/userservice-migrate" \
    --connection "$IRONFRONT_USER_DB_CONNECTION"

echo "Applying MatchmakingService migrations..."
"$TARGET_ROOT/migrations/matchmaking-migrate" \
    --connection "$IRONFRONT_MATCHMAKING_DB_CONNECTION"

echo "Starting updated services..."
systemctl restart ironfront-auth.service
wait_for_http \
    "http://127.0.0.1:${IRONFRONT_AUTHSERVICE_PORT}/internal/health" \
    "Ironfront AuthService"

systemctl restart ironfront-user.service
wait_for_http \
    "http://127.0.0.1:${IRONFRONT_USERSERVICE_PORT}/internal/health" \
    "Ironfront UserService"

systemctl restart ironfront-matchmaking.service
wait_for_http \
    "http://127.0.0.1:${IRONFRONT_MATCHMAKING_PORT}/control/v1/health" \
    "Ironfront MatchmakingService"

systemctl restart ironfront-gateway.service
wait_for_http \
    "http://127.0.0.1:5000/api/health" \
    "Ironfront Gateway"

echo
echo "=========================================="
echo "Deployment completed successfully."
echo "=========================================="
echo
echo "Backup retained at:"
echo "$BACKUP_ROOT"
echo
echo "Public gateway health:"
echo "https://gateway.warlabs.net/api/health"
