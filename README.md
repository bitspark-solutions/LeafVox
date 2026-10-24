# LeafVox 🍃🔊

**A listening PDF reader — 100% free, 100% offline.** Opens any PDF, reads it aloud sentence-by-sentence with natural neural voices running locally, and highlights the text as it speaks. Built on .NET 10 + Avalonia 12, so the same codebase targets Windows, macOS, Linux, Android and iOS.

## Features (all shipped, all $0)
- 📂 Open any PDF → clean reflowed reader with per-sentence layout analysis
- 🔊 Continuous narration with **karaoke highlight + auto-scroll**, gapless via 2-sentence prefetch
- 🎯 **Read from anywhere**: click / double-tap / right-click a sentence — or restore exactly where you stopped last time (resume is automatic, per book)
- 🔂 Read-this-line-only, ⏮/⏭ sentence steps, speed 0.5×–3×, ⌨ keyboard: `Space` play/pause, `←/→` sentence, `Esc` stop
- 🗣 Free voice manager: one-click download of the whole **Kokoro-82M** natural tier and 10+ **Piper** light voices (incl. Bangla, Hindi, Arabic, DE/FR/ES) — licenses shown per voice
- 🕘 Recents library, 🌙 sleep timer, per-app voice/speed settings
- 🧠 Scanned-PDF detection (tells you which pages have no text layer instead of failing silently)

## Zero-cost promise
No subscriptions, no API keys, no telemetry, no hosted CI. Every dependency is MIT/Apache/BSD — see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Voices are downloaded by the user from official free releases; licenses are shown in-app.

### Measured on a CPU-only laptop (no GPU, no cloud)
| Tier | Voice | RTF |
|---|---|---|
| Light | Piper en_US-lessac-medium | **0.06–0.13** (8–17× realtime) |
| Natural | Kokoro-82M | **< 3× realtime** — verified in the test suite |

## Build & run
```bash
dotnet build LeafVox.slnx
dotnet run --project src/LeafVox.App/LeafVox.App.Desktop
bash scripts/verify-all.sh        # build → 39 tests → 3 spikes (the CI we refuse to host)
```
Publish bundles: `scripts/publish-win.sh`, `scripts/publish-linux.sh`, `scripts/publish-mac.sh [arm64|x64]` (each run on its own OS).

## Layout
```
src/LeafVox.Core            PDF → sentence spine (pure, 23 tests)
src/LeafVox.Tts             engine seam + prefetch pipeline + sinks (16 tests, incl. live-model tests)
src/LeafVox.App             Avalonia shared UI (MVVM)
src/LeafVox.App.Desktop     Windows/Linux/macOS head
src/LeafVox.App.{Android,iOS,Browser}   heads staged — wire workloads, reuse everything
spikes/                     feasibility code kept as living docs
scripts/                    verify-all.sh + per-OS publish scripts
```

## Roadmap (Phase 8 — deferred, honest list)
Original-layout page view (PDFium/Docnet overlay) · OCR for scanned PDFs (Tesseract) · Android/iOS workload enablement · bookmark list UI · word-level highlight timing.

Status: Phases 0–7 complete; see `.plan/2026-10-23_master_plan.md` and `.progress/`.
