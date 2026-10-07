#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DEPLOY_ROOT="$ROOT/deploy/IronfrontNetwork"

REMOTE_HOST="${IRONFRONT_DEPLOY_HOST:-homeserver}"
REMOTE_PACKAGE="/tmp/ironfront-deploy.tar.gz"
REMOTE_STAGE="/tmp/ironfront-release-stage"
REMOTE_TARGET="/opt/warlabs/ironfront"

PACKAGE="$ROOT/deploy/ironfront-deploy-linux.tar.gz"
PACKAGE_STAGE="$(mktemp -d)"

cleanup() {
    rm -rf "$PACKAGE_STAGE"
}

trap cleanup EXIT

require_path() {
    local path="$1"

    if [[ ! -e "$path" ]]; then
        echo "ERROR: Required deployment path is missing:"
        echo "$path"
        echo
        echo "Run scripts/linux/publish-linux-deploy.sh first."
        exit 1
    fi
}

for required_path in \
    "$DEPLOY_ROOT/authservice/Ironfront.AuthService.dll" \
    "$DEPLOY_ROOT/userservice/Ironfront.UserService.dll" \
    "$DEPLOY_ROOT/gateway/Ironfront.Gateway.dll" \
    "$DEPLOY_ROOT/game-data/vehicles.catalog.json" \
    "$DEPLOY_ROOT/game-data/battle-modes.catalog.json" \
    "$DEPLOY_ROOT/migrations/authservice-migrate" \
    "$DEPLOY_ROOT/migrations/userservice-migrate" \
    "$DEPLOY_ROOT/Caddy/Caddyfile" \
    "$DEPLOY_ROOT/systemd/ironfront-auth.service" \
    "$DEPLOY_ROOT/systemd/ironfront-user.service" \
    "$DEPLOY_ROOT/systemd/ironfront-gateway.service" \
    "$DEPLOY_ROOT/env/ironfront.env.example" \
    "$DEPLOY_ROOT/start-ironfront.sh" \
    "$ROOT/scripts/linux/install-systemd-units.sh" \
    "$DEPLOY_ROOT/matchmaking/Ironfront.MatchmakingService.dll" \
    "$DEPLOY_ROOT/migrations/matchmaking-migrate" \
    "$DEPLOY_ROOT/systemd/ironfront-matchmaking.service" \
    "$ROOT/scripts/linux/apply-release-on-homeserver.sh"
do
    require_path "$required_path"
done

mkdir -p \
    "$PACKAGE_STAGE/env" \
    "$PACKAGE_STAGE/scripts/linux"

for release_directory in \
    authservice \
    userservice \
    matchmaking \
    gateway \
    game-data \
    migrations \
    Caddy \
    systemd
do
    cp -a \
        "$DEPLOY_ROOT/$release_directory" \
        "$PACKAGE_STAGE/$release_directory"
done

install -m 0644 \
    "$DEPLOY_ROOT/env/ironfront.env.example" \
    "$PACKAGE_STAGE/env/ironfront.env.example"

install -m 0755 \
    "$DEPLOY_ROOT/start-ironfront.sh" \
    "$PACKAGE_STAGE/start-ironfront.sh"

install -m 0755 \
    "$ROOT/scripts/linux/install-systemd-units.sh" \
    "$PACKAGE_STAGE/scripts/linux/install-systemd-units.sh"

install -m 0755 \
    "$ROOT/scripts/linux/apply-release-on-homeserver.sh" \
    "$PACKAGE_STAGE/scripts/linux/apply-release-on-homeserver.sh"

rm -f "$PACKAGE"

tar -czf "$PACKAGE" \
    -C "$PACKAGE_STAGE" \
    .

if tar -tzf "$PACKAGE" |
    grep -Eq '(^|/)ironfront\.local\.env$'; then
    echo "ERROR: Refusing to deploy because the package contains ironfront.local.env."
    exit 1
fi

echo
echo "=========================================="
echo "Ironfront Home Server Deploy"
echo "=========================================="
echo "Host:    $REMOTE_HOST"
echo "Target:  $REMOTE_TARGET"
echo "Package: $PACKAGE"
echo

echo "Package contents:"
tar -tzf "$PACKAGE" | sort

echo
read -r -p "Type DEPLOY to upload and apply this release: " confirmation

if [[ "$confirmation" != "DEPLOY" ]]; then
    echo "Deployment cancelled."
    exit 0
fi

echo
echo "Uploading release package..."
scp "$PACKAGE" "$REMOTE_HOST:$REMOTE_PACKAGE"

echo
echo "Applying release on $REMOTE_HOST..."
ssh -tt "$REMOTE_HOST" "
set -e

sudo rm -rf '$REMOTE_STAGE'
sudo mkdir -p '$REMOTE_STAGE'

sudo tar -xzf '$REMOTE_PACKAGE' -C '$REMOTE_STAGE'

sudo '$REMOTE_STAGE/scripts/linux/apply-release-on-homeserver.sh' \
    '$REMOTE_STAGE' \
    '$REMOTE_TARGET'

sudo rm -rf '$REMOTE_STAGE'
sudo rm -f '$REMOTE_PACKAGE'
"

echo
echo "=========================================="
echo "Deployment command completed."
echo "=========================================="
