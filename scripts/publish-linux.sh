#!/usr/bin/env bash
# Build a self-contained Linux bundle (run on Linux).
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet publish src/LeafVox.App/LeafVox.App.Desktop -c Release -r linux-x64 --self-contained -o publish/linux-x64
echo "Bundle: publish/linux-x64/LeafVox.App.Desktop"
echo "Users need system fonts for best PDF parsing: sudo apt install fonts-liberation fontconfig"
