#!/usr/bin/env bash
# KSP-Connected one-click installer for Linux / macOS
# =====================================================
# Usage:
#   chmod +x scripts/install.sh
#   ./scripts/install.sh
#
# Or supply KSP path:
#   KSP_PATH="/path/to/KSP" ./scripts/install.sh

set -euo pipefail

# ── colours ──────────────────────────────────────────────────────────────────
CYAN='\033[0;36m'; YELLOW='\033[1;33m'; GREEN='\033[0;32m'; RED='\033[0;31m'; NC='\033[0m'
header() { echo -e "\n${CYAN}============================================================${NC}"; echo -e "${CYAN}  $1${NC}"; echo -e "${CYAN}============================================================${NC}"; }
step()   { echo -e "${YELLOW}  >>> $1${NC}"; }
ok()     { echo -e "${GREEN}  [OK] $1${NC}"; }
fail()   { echo -e "${RED}  [!!] $1${NC}"; exit 1; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

# ── banner ────────────────────────────────────────────────────────────────────
header "KSP-Connected Installer"
echo "  Multiplayer mod for Kerbal Space Program 1.12"

# ── locate KSP ───────────────────────────────────────────────────────────────
step "Locating KSP installation..."

KSP_CANDIDATES=(
    "${KSP_PATH:-}"
    "$HOME/.steam/steam/steamapps/common/Kerbal Space Program"
    "$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
    "/usr/local/games/Kerbal Space Program"
    # macOS
    "$HOME/Library/Application Support/Steam/steamapps/common/Kerbal Space Program"
)

RESOLVED_KSP=""
for p in "${KSP_CANDIDATES[@]}"; do
    [[ -z "$p" ]] && continue
    if [[ -f "$p/KSP.x86_64" || -f "$p/KSP_x64" || -f "$p/KSP.app/Contents/MacOS/KSP" ]]; then
        RESOLVED_KSP="$p"
        break
    fi
done

if [[ -z "$RESOLVED_KSP" ]]; then
    echo ""
    echo -e "${YELLOW}  KSP not found automatically.${NC}"
    read -rp "  Enter full path to your KSP folder: " RESOLVED_KSP
    if [[ ! -d "$RESOLVED_KSP/GameData" ]]; then
        fail "KSP not found at: $RESOLVED_KSP (no GameData folder)"
    fi
fi

ok "KSP found at: $RESOLVED_KSP"

# ── check .NET 6 SDK ─────────────────────────────────────────────────────────
step "Checking .NET 6 SDK..."

DOTNET_OK=false
if command -v dotnet &>/dev/null; then
    VER=$(dotnet --version 2>/dev/null || true)
    if [[ "$VER" =~ ^[6-9]\. || "$VER" =~ ^[1-9][0-9]\. ]]; then
        DOTNET_OK=true
        ok ".NET SDK $VER found."
    fi
fi

if [[ "$DOTNET_OK" == "false" ]]; then
    step ".NET 6 SDK not found. Installing..."
    if [[ "$(uname)" == "Darwin" ]]; then
        if command -v brew &>/dev/null; then
            brew install --cask dotnet-sdk
        else
            fail ".NET 6 SDK not found. Install it from https://dotnet.microsoft.com/download/dotnet/6.0 then re-run."
        fi
    else
        # Linux — try the official Microsoft install script
        curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 6.0 --install-dir "$HOME/.dotnet"
        export PATH="$HOME/.dotnet:$PATH"
        export DOTNET_ROOT="$HOME/.dotnet"
    fi
    ok ".NET SDK installed."
fi

# ── build ─────────────────────────────────────────────────────────────────────
cd "$REPO_ROOT"

step "Building shared library..."
dotnet build Shared/KspConnected.Shared.csproj -c Release -nologo -v q

step "Building KSP client plugin..."
dotnet build Client/KspConnected.Client.csproj -c Release -nologo -v q \
    -p:KspPath="$RESOLVED_KSP"
ok "Plugin DLLs compiled."

step "Building server..."
dotnet build Server/KspConnected.Server.csproj -c Release -nologo -v q
ok "Server built."

# ── install plugin ────────────────────────────────────────────────────────────
step "Installing mod into KSP GameData..."

DEST="$RESOLVED_KSP/GameData/KspConnected"
[[ -d "$DEST" ]] && rm -rf "$DEST"
cp -r "$REPO_ROOT/GameData/KspConnected" "$RESOLVED_KSP/GameData/"
ok "Mod installed to: $DEST"

# ── server launcher script ────────────────────────────────────────────────────
LAUNCHER="$REPO_ROOT/start-server.sh"
SERVER_DLL="$REPO_ROOT/Server/bin/Release/net6.0/KspConnected.Server.dll"
cat > "$LAUNCHER" <<EOF
#!/usr/bin/env bash
# Start the KSP-Connected multiplayer server
cd "$(dirname "\$0")"
dotnet "$SERVER_DLL" "\$@"
EOF
chmod +x "$LAUNCHER"
ok "Server launcher: $LAUNCHER"

# ── done ──────────────────────────────────────────────────────────────────────
header "Installation Complete!"
echo ""
echo "  Mod installed to:"
echo -e "    ${CYAN}$DEST${NC}"
echo ""
echo "  To host a game, run the server:"
echo -e "    ${CYAN}./start-server.sh${NC}  (or: dotnet \"$SERVER_DLL\")"
echo ""
echo "  In KSP → Space Center → KSP-Connected window → enter host IP → Connect"
echo ""
