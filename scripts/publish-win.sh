#!/usr/bin/env bash
# Build a self-contained Windows bundle (run on Windows). No CI services needed.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet publish src/LeafVox.App/LeafVox.App.Desktop -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=false -o publish/win-x64
echo "Bundle: publish/win-x64/LeafVox.App.Desktop.exe"
