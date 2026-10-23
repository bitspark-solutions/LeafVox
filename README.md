# LeafVox 🍃🔊

**A listening PDF reader — 100% free, 100% offline.** Opens any PDF, reads it aloud sentence-by-sentence with natural neural voices running locally, and highlights the text as it speaks. Built on .NET 10 + Avalonia 12 so the same codebase can ship to Windows, macOS, Linux, Android, and iOS later.

## Zero-cost promise
No subscriptions, no API keys, no telemetry. Every component is MIT/Apache/BSD and every voice is free/open:

| Concern | Component | License |
|---|---|---|
| UI | Avalonia 12 | MIT |
| PDF text + geometry | PdfPig | Apache-2.0 |
| PDF rendering | Docnet (PDFium) | BSD-3 |
| Neural TTS engine | sherpa-onnx | Apache-2.0 |
| Voices | Kokoro-82M (natural flagship), Piper VITS (light tier) | Apache-2.0 / CC BY per voice |
| Audio | NAudio | MIT |

## Roadmap
See `.plan/2026-10-23_master_plan.md`. Status:
- ✅ Phase 0 — scaffold + feasibility spikes (RTF 0.41 measured on Piper medium, CPU-only)
- ⬜ Phase 1 — reading spine (Core)
- ⬜ Phase 2 — TTS pipeline (prefetch synthesis + playback)
- ⬜ Phase 3 — reader UI + karaoke highlight
- ⬜ Phase 4 — reading UX polish
- ⬜ Phase 5 — free voice manager (Kokoro downloads)
- ⬜ Phase 6 — cross-platform runway
- ⬜ Phase 7 — packaging & docs

## Layout
```
src/LeafVox.Core            PDF -> sentence spine (pure, testable)
src/LeafVox.Tts             engine abstraction, prefetch pipeline, audio sinks
src/LeafVox.App             Avalonia shared UI (MVVM)
src/LeafVox.App.Desktop     Windows/Linux/macOS head
src/LeafVox.App.{Android,iOS,Browser}  heads staged for later phases
tests/LeafVox.Core.Tests    xUnit
spikes/                     Phase 0 feasibility code (kept as living docs)
scripts/verify-all.sh       local verification (no CI services)
```

## Build & run
```
dotnet build LeafVox.slnx
dotnet run --project src/LeafVox.App/LeafVox.App.Desktop
```
