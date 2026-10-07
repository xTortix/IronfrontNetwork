#!/usr/bin/env bash
set -euo pipefail

ROOT="/opt/warlabs/ironfront"
ENV_DIRECTORY="/etc/warlabs/ironfront"
ENV_FILE="$ENV_DIRECTORY/ironfront.env"

if [[ $EUID -ne 0 ]]; then
    echo "Please run with sudo:"
    echo "sudo /opt/warlabs/ironfront/scripts/linux/install-systemd-units.sh"
    exit 1
fi

if ! id ironfront >/dev/null 2>&1; then
    useradd \
        --system \
        --home "$ROOT" \
        --shell /usr/sbin/nologin \
        ironfront
fi

mkdir -p \
    "$ROOT" \
    "$ROOT/authservice" \
    "$ROOT/userservice" \
    "$ROOT/matchmaking" \
    "$ROOT/gateway" \
    "$ROOT/game-data" \
    "$ENV_DIRECTORY" \
    /var/lib/warlabs/ironfront \
    /var/log/warlabs/ironfront

chown -R ironfront:ironfront \
    "$ROOT/authservice" \
    "$ROOT/userservice" \
    "$ROOT/matchmaking" \
    "$ROOT/gateway" \
    "$ROOT/game-data" \
    /var/lib/warlabs/ironfront \
    /var/log/warlabs/ironfront

chown -R root:root \
    "$ROOT/Caddy" \
    "$ROOT/systemd" \
    "$ROOT/env" \
    "$ROOT/migrations" \
    "$ROOT/scripts"

chown root:root "$ROOT/start-ironfront.sh"
chmod 0755 "$ROOT/start-ironfront.sh"

chown root:root "$ENV_DIRECTORY"
chmod 0755 "$ENV_DIRECTORY"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "WARNING: $ENV_FILE does not exist yet."
    echo "Create it from:"
    echo "$ROOT/env/ironfront.env.example"
else
    chown root:root "$ENV_FILE"
    chmod 0600 "$ENV_FILE"
fi

install -o root -g root -m 0644 \
    "$ROOT/systemd/ironfront-auth.service" \
    /etc/systemd/system/ironfront-auth.service

install -o root -g root -m 0644 \
    "$ROOT/systemd/ironfront-user.service" \
    /etc/systemd/system/ironfront-user.service

install -o root -g root -m 0644 \
    "$ROOT/systemd/ironfront-matchmaking.service" \
    /etc/systemd/system/ironfront-matchmaking.service

install -o root -g root -m 0644 \
    "$ROOT/systemd/ironfront-gateway.service" \
    /etc/systemd/system/ironfront-gateway.service

systemctl daemon-reload

systemctl enable \
    ironfront-auth.service \
    ironfront-user.service \
    ironfront-matchmaking.service \
    ironfront-gateway.service

echo
echo "Ironfront systemd units installed."
echo
echo "Restart services with:"
echo "sudo systemctl restart ironfront-auth ironfront-user ironfront-matchmaking ironfront-gateway"
echo
echo "Inspect logs with:"
echo "sudo journalctl -u ironfront-auth -u ironfront-user -u ironfront-matchmaking -u ironfront-gateway -f"
