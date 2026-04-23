#!/usr/bin/env bash
# package.sh — builds and packages KSP-Connected for distribution
# ================================================================
# Produces:
#   dist/
#     KSP-Connected-v<VERSION>-mod.zip       ← drop into KSP GameData
#     KSP-Connected-Server-v<VERSION>-linux-x64.zip
#     KSP-Connected-Server-v<VERSION>-win-x64.zip
#     KSP-Connected-Server-v<VERSION>-osx-x64.zip
#     KSP-Connected-Full-v<VERSION>.zip      ← everything + install scripts
#
# Usage:
#   KSP_PATH="/path/to/KSP" ./scripts/package.sh [version]
#
# Example:
#   KSP_PATH="$HOME/.steam/steam/steamapps/common/Kerbal Space Program" \
#   ./scripts/package.sh 1.0.0

set -euo pipefail

CYAN='\033[0;36m'; YELLOW='\033[1;33m'; GREEN='\033[0;32m'; RED='\033[0;31m'; NC='\033[0m'
step() { echo -e "${YELLOW}>>> $1${NC}"; }
ok()   { echo -e "${GREEN}[OK] $1${NC}"; }
fail() { echo -e "${RED}[!!] $1${NC}"; exit 1; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(dirname "$SCRIPT_DIR")"
VERSION="${1:-1.0.0}"
DIST="$ROOT/dist"

echo -e "${CYAN}======================================${NC}"
echo -e "${CYAN}  KSP-Connected Packager v$VERSION${NC}"
echo -e "${CYAN}======================================${NC}"

# ── clean dist ───────────────────────────────────────────────────────────────
rm -rf "$DIST"
mkdir -p "$DIST"

cd "$ROOT"

# ── build shared ─────────────────────────────────────────────────────────────
step "Building Shared..."
dotnet build Shared/KspConnected.Shared.csproj -c Release -nologo -v q

# ── build client plugin ───────────────────────────────────────────────────────
KSP_PATH="${KSP_PATH:-$HOME/.steam/steam/steamapps/common/Kerbal Space Program}"

if [[ -d "$KSP_PATH" ]]; then
    step "Building KSP plugin (KSP at $KSP_PATH)..."
    dotnet build Client/KspConnected.Client.csproj -c Release -nologo -v q \
        -p:KspPath="$KSP_PATH"
    ok "Plugin DLLs ready in GameData/KspConnected/Plugins/"
else
    echo -e "${YELLOW}[WARN] KSP not found at $KSP_PATH — skipping plugin build.${NC}"
    echo "       Set KSP_PATH and re-run to include the client DLLs."
fi

# ── build server for all platforms ───────────────────────────────────────────
SERVER_RIDS=("linux-x64" "win-x64" "osx-x64")

for RID in "${SERVER_RIDS[@]}"; do
    step "Publishing server for $RID..."
    dotnet publish Server/KspConnected.Server.csproj \
        -c Release -r "$RID" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:DebugType=none \
        -o "$DIST/server-$RID" \
        -nologo -v q
    ok "Server published: $DIST/server-$RID"
done

# ── mod zip (GameData drop-in) ────────────────────────────────────────────────
step "Creating mod zip (KSP GameData drop-in)..."
MOD_ZIP="$DIST/KSP-Connected-v${VERSION}-mod.zip"
(cd "$ROOT" && zip -r "$MOD_ZIP" GameData/KspConnected -x "*/\.gitkeep")
ok "Mod zip: $MOD_ZIP"

# ── server zips ──────────────────────────────────────────────────────────────
for RID in "${SERVER_RIDS[@]}"; do
    step "Zipping server $RID..."
    SERVER_ZIP="$DIST/KSP-Connected-Server-v${VERSION}-${RID}.zip"
    cp "$ROOT/server.json" "$DIST/server-$RID/"
    (cd "$DIST/server-$RID" && zip -r "$SERVER_ZIP" .)
    ok "Server zip: $SERVER_ZIP"
done

# ── full zip (everything + install scripts) ───────────────────────────────────
step "Creating full release zip..."
FULL_ZIP="$DIST/KSP-Connected-Full-v${VERSION}.zip"
TMP="$DIST/full-tmp"
mkdir -p "$TMP"

# Source
cp -r "$ROOT/Shared"    "$TMP/"
cp -r "$ROOT/Client"    "$TMP/"
cp -r "$ROOT/Server"    "$TMP/"
cp -r "$ROOT/GameData"  "$TMP/"
cp -r "$ROOT/scripts"   "$TMP/"
cp    "$ROOT/build.sh"  "$TMP/"
cp    "$ROOT/build.bat" "$TMP/"
cp    "$ROOT/server.json" "$TMP/"
cp    "$ROOT/README.md" "$TMP/"
cp    "$ROOT/KSP-Connected.sln" "$TMP/"
# Installer scripts
cp "$DIST/server-linux-x64"/* "$TMP/" 2>/dev/null || true

(cd "$TMP" && zip -r "$FULL_ZIP" . -x "*/bin/*" -x "*/obj/*")
rm -rf "$TMP"
ok "Full zip: $FULL_ZIP"

# ── summary ──────────────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}======================================${NC}"
echo -e "${CYAN}  Packages ready in: $DIST/${NC}"
echo -e "${CYAN}======================================${NC}"
ls -lh "$DIST"/*.zip 2>/dev/null || true
echo ""
echo "  Users only need the GameData KSP mod zip + their platform server zip."
echo ""
