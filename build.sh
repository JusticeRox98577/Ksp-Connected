#!/usr/bin/env bash
# ============================================================
# KSP-Connected build script
# ============================================================
# Requirements:
#   - .NET 6 SDK   (https://dotnet.microsoft.com/download)
#   - KSP 1.12 installed (set KSP_PATH below or export it)
#
# Usage:
#   ./build.sh                    # build client + server
#   ./build.sh client             # build plugin only
#   ./build.sh server             # build server only
#   ./build.sh server publish     # publish server as single binary
# ============================================================

set -e

# ---- configuration ----
: "${KSP_PATH:=$HOME/.steam/steam/steamapps/common/Kerbal Space Program}"
TARGET="${1:-all}"
PUBLISH="${2:-}"

echo "============================================"
echo " KSP-Connected Build"
echo "============================================"

build_server() {
    echo ""
    echo ">>> Building Server…"
    if [ "$PUBLISH" = "publish" ]; then
        dotnet publish Server/KspConnected.Server.csproj \
            -c Release \
            -r linux-x64 \
            --self-contained true \
            -p:PublishSingleFile=true \
            -o dist/server
        echo ">>> Server binary: dist/server/KspConnected.Server"
    else
        dotnet build Server/KspConnected.Server.csproj -c Release
        echo ">>> Server DLL: Server/bin/Release/net6.0/KspConnected.Server.dll"
    fi
}

build_client() {
    echo ""
    echo ">>> Building Client plugin…"
    echo "    KSP_PATH = $KSP_PATH"

    if [ ! -d "$KSP_PATH" ]; then
        echo ""
        echo "ERROR: KSP not found at: $KSP_PATH"
        echo "Set the KSP_PATH environment variable to your KSP installation directory."
        echo "Example: KSP_PATH='/path/to/Kerbal Space Program' ./build.sh"
        exit 1
    fi

    dotnet build Client/KspConnected.Client.csproj \
        -c Release \
        -p:KspPath="$KSP_PATH"

    echo ""
    echo ">>> Plugin DLLs copied to: GameData/KspConnected/Plugins/"
}

case "$TARGET" in
    server)
        build_server
        ;;
    client)
        build_client
        ;;
    all)
        build_server
        build_client
        ;;
    *)
        echo "Unknown target: $TARGET"
        echo "Usage: ./build.sh [all|client|server] [publish]"
        exit 1
        ;;
esac

echo ""
echo "============================================"
echo " Build complete!"
echo "============================================"
echo ""
echo "Next steps:"
echo "  Server: run  dotnet Server/bin/Release/net6.0/KspConnected.Server.dll  (or dist/server/KspConnected.Server if published)"
echo "  Client: copy GameData/KspConnected/ into your KSP GameData/ folder"
