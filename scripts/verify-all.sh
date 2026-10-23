#!/usr/bin/env bash
# LeafVox local verification — replaces any CI service (user policy: no GitHub Actions).
# Run from repo root:  bash scripts/verify-all.sh
set -euo pipefail
cd "$(dirname "$0")/.."

echo "== build =="
dotnet build LeafVox.slnx -c Debug --nologo -v q

echo "== unit tests =="
dotnet test tests/LeafVox.Core.Tests -c Debug --nologo -v q

echo "== spike A: PDF spine =="
dotnet run --project spikes/SpikePdf

echo "== spike B: offline TTS =="
dotnet run --project spikes/SpikeTts

echo "== ALL VERIFIED =="
