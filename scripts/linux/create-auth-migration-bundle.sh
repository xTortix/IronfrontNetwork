#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DEPLOY_ROOT="$ROOT/deploy/IronfrontNetwork"
RUNTIME="linux-x64"
OUTPUT="$DEPLOY_ROOT/migrations/authservice-migrate"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet was not found. Install the .NET SDK on the development machine."
    exit 1
fi

echo "Restoring repository-local .NET tools..."
(
    cd "$ROOT"
    dotnet tool restore
)

mkdir -p "$DEPLOY_ROOT/migrations"
rm -f "$OUTPUT"

(
  cd "$ROOT"
  dotnet ef migrations bundle \
  --project "$ROOT/src/Ironfront.AuthService" \
  --startup-project "$ROOT/src/Ironfront.AuthService" \
  -r "$RUNTIME" \
  -o "$OUTPUT"
)

chmod +x "$OUTPUT"

echo "Migration bundle created: $OUTPUT"
