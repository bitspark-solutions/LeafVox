# LeafVox — Phase 0 Foundation Progress Report

Date: 2026-10-23
Plan: `.plan/2026-10-23_master_plan.md`

## What got built
- Solution `LeafVox.slnx` (.NET 10, SDKs present: 10.0.301 + 10.0.402):
  - `src/LeafVox.Core` (PdfPig 0.1.16)
  - `src/LeafVox.Tts` (org.k2fsa.sherpa.onnx 1.13.8 + runtime.win-x64, NAudio 3.3.0)
  - `src/LeafVox.App` + `.Desktop` (Avalonia 12.1.4 xplat template; Android/iOS/Browser heads on disk, not yet in solution — Phase 6)
  - `tests/LeafVox.Core.Tests` (xUnit)
- Root `Directory.Build.props` + `Directory.Packages.props` (central package management; template's nested props removed).
- Full solution builds: **0 warnings, 0 errors**.

## Spike results (real runs, not assumptions)
### Spike A — PDF → sentence spine: 5/5 PASS
- Built a deterministic 2-page PDF with PdfPig `PdfDocumentBuilder`, extracted with `NearestNeighbourWordExtractor`.
- Verified: word count, bounding boxes (all widths > 0), exact text round-trip, hyphen-join across visual lines, sentence splitting.
- **Lessons learned** (feed into Phase 1):
  - `PdfRectangle` API is `Centroid`/`TopLeft`/`BottomLeft` — no `.Center`; `PdfPoint` lives in `UglyToad.PdfPig.Core`; `Standard14Font` in `UglyToad.PdfPig.Fonts.Standard14Fonts`.
  - NN-extracted `Word.Text` includes trailing whitespace **and** the collection contains empty whitespace-words → must `Trim()` + filter empties or joins get double spaces.
  - Word enumeration order from `GetWords()` follows content stream (fine for simple docs; multi-column needs the Docstrum block pipeline — planned).

### Spike B — offline neural TTS → WAV: PASS
- Model: `en_US-lessac-medium` (Piper VITS via sherpa-onnx), 60 MB, downloaded to `%LOCALAPPDATA%\leafvox\models\`. Tarball filenames are `en_US-lessac-medium.onnx` + `tokens.txt` + `espeak-ng-data/` (NOT prefixed `vits-piper-…` as docs imply).
- Measured on this machine (CPU-only, NumThreads=2):
  - model load: **2,082 ms**
  - synth **2,241 ms audio in 5.49 s → RTF 0.408** (≈2.4× realtime) → sentence prefetch (depth 2) will fully hide gaps.
  - speed control works (`1.5x` → 3.98 s audio in 304 ms incl. retiming).
  - streaming callback (`GenerateWithConfig` + progress callback) works — that's the Phase 2 pipeline seam.
- C# API signatures (verified against `scripts/dotnet/OfflineTts.cs`): `Generate(string text, float speed, int speakerId)`, `GenerateWithConfig(text, OfflineTtsGenerationConfig{Sid,Speed,SilenceScale}, OfflineTtsCallbackProgressWithArg)`, `SampleRate` (22050), `audio.SaveToWaveFile(path)`.

## Decisions made
- **Name: LeafVox** — 0 GitHub repos, user/org/npm/PyPI all unclaimed (verified 2026-10-23 via API).
- Avalonia xplat template over plain desktop app: mobile heads scaffolded from day 1 → the "later Mac/Linux/Android/iOS" requirement costs near-zero restructure.
- sherpa-onnx chosen as the single TTS runtime for both voice tiers (Kokoro flagship + Piper light) — one engine, official C# bindings, Apache-2.0, RID packages for win/linux/osx/android.
- REA (morluto/rea) assessed and not needed for v1: commercial cloud-TTS readers hide their value behind auth + proprietary voices (incompatible with our $0/offline constraint); we rebuilt the category feature set (highlight-sync, transport, speed, voice picker) locally. Optional mechanics-only study task kept in Phase 4. Brand hygiene: no competitor names in any repo file, ever.

## Issues hit & fixed
1. `dotnet new sln` now emits `.slnx` (NET 10 default) — solution files renamed accordingly.
2. Write-guard on template csproj → used `patch` not `write_file`.
3. `notify` param on background terminal rejected both `true` and pattern-list forms this build → ran downloads foreground with generous timeout instead.

## Next (Phase 1)
`LeafVox.Core` domain: `PdfBook`, `ReadingSpine`, `SentenceBuilder` (abbrev-aware splitter, un-hyphenate, header/footer drop), `PipelineSelector` — TDD against spike fixtures + a real-world PDF corpus.
