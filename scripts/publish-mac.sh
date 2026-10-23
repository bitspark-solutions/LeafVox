#!/usr/bin/env bash
# Build self-contained macOS bundles (run on a Mac; pick arch or build both).
set -euo pipefail
cd "$(dirname "$0")/.."
ARCH="${1:-$(uname -m)}"; [ "$ARCH" = "arm64" ] && RID=osx-arm64 || RID=osx-x64
dotnet publish src/LeafVox.App/LeafVox.App.Desktop -c Release -r "$RID" --self-contained -o "publish/$RID"
echo "Bundle: publish/$RID/LeafVox.App.Desktop"
