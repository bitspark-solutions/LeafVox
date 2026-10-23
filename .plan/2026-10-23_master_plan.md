# LeafVox — Offline PDF Reader & Voice Player (.NET 10)

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** A 100% free, 100% offline desktop app that opens any PDF, reads it aloud sentence-by-sentence with natural local neural voices, and highlights the text being spoken — with a fast, smooth UI, built so the same codebase later ships to macOS, Linux, Android, and iOS.

## Repo hard rules (owner-set; apply to every future commit)
1. **One class per file** — every class/record/struct/enum/interface in its own file (entities, DTOs, enums included); no nested or colocated types.
2. **No competitor brand names** anywhere in the repo (code, docs, UI, commits) — describe the category generically.
3. **$0 forever** — MIT/Apache/BSD deps only; free-licensed voices with per-voice license records.
4. **No hosted CI** — verification is `scripts/verify-all.sh`, must pass before push.

**Name:** **LeafVox** (leaf = page, vox = voice). Verified free on 2026-10-23: 0 GitHub repos with that name, GitHub user/org `leafvox` is 404 (unclaimed), npm `leafvox` 404, PyPI `leafvox` 404. Alternatives that were taken: VoxReader, SpeakPDF, Readalot. Runner-up free names: PageMouth, AudLeaf.

**Architecture:** Clean-layered .NET solution: `LeafVox.Core` (pure PDF → reading-spine model: pages → blocks → sentences with bounding boxes), `LeafVox.Tts` (engine abstraction over sherpa-onnx with a sentence-prefetch synthesis pipeline and an `IAudioSink` playback seam), and `LeafVox.App` (Avalonia 12 MVVM UI: reader view, karaoke highlight, transport controls, voice manager). All platform-specific code hides behind the two seams `ITtsEngine` and `IAudioSink`, which is what makes the Mac/Linux/Android/iOS port cheap later.

**Tech Stack (every line free — MIT/Apache/BSD, $0 forever, no API keys):**

| Concern | Choice | License | Why |
|---|---|---|---|
| UI framework | **Avalonia 12.1** (net10 recommended target; mobile targets require net10) | MIT | Single codebase → Windows, macOS, Linux, iOS, Android, WASM. Avalonia 12 (Apr 2026): Android 4× faster startup, 120 FPS scrolling, page-based mobile navigation, built-in accessibility. The only .NET option that is first-class on *Linux* and *mobile* at once. |
| MVVM | CommunityToolkit.Mvvm | MIT | Source-gen viewmodels, no reflection overhead. |
| PDF text + geometry | **PdfPig 0.1.16** | Apache-2.0 | Extracts every Letter/Word with bounding boxes; DocumentLayoutAnalysis gives blocks/paragraphs (Recursive XY-Cut for multi-column, Docstrum), reading-order detection. Pure managed → runs everywhere .NET runs. |
| PDF page rendering | **Docnet.Core 2.6.0** (PDFium bindings) | BSD-3 (PDFium: Apache-3-clause) | Pixel-perfect page view option ("original layout" mode). PDFium is Chrome's renderer: fast. |
| TTS engine | **sherpa-onnx 1.13.8** (`org.k2fsa.sherpa.onnx` NuGet + per-RID runtime packages: win-x64, linux-x64/arm64, osx-x64/arm64, android-arm64/x64) | Apache-2.0 | One runtime, official C# API, offline neural TTS on every platform incl. mobile. Supports both Kokoro and Piper/VITS model families. |
| Voices (the "best free natural voices, local") | **Kokoro-82M** (Apache-2.0 model; voices af_heart/af_bella/am_michael/bf_emma…) as flagship quality + **Piper VITS** voices (Apache code, voices CC BY per-model) as light/fast tier | Apache-2.0 / CC BY | Kokoro is currently the best free local natural-speech model (TTS Arena). Both families run inside sherpa-onnx → no second engine. |
| Audio output | **NAudio** (Windows) behind `IAudioSink`; later miniaudio-backed sink for other platforms | MIT | WASAPI low-latency float playback, pause/resume/seek per sentence. |
| Persistence | SQLite via Microsoft.Data.Sqlite + JSON settings | MIT | Library, resume positions, downloaded-voice registry. |
| OCR fallback (scanned PDFs) | Tesseract (deferred to Phase 8) | BSD-3 | Scanned PDFs have no text layer; free fix, not v1. |

**Positioning note (read this before writing any public copy):** LeafVox competes in the "listen-to-your-documents" category popularized by commercial cloud TTS services, but the repo must never reference a competitor's name or trade dress — in plans, docs, README, commit messages, or UI copy. We build the *category* (document ingest, continuous narration, synced highlighting, speed control, voice picking) on our own free/local stack; that is a legitimate, independent implementation. Marketing language stays generic ("read aloud", "listen to PDFs"). The name LeafVox is original and verified unclaimed.

---

## Edge Cases (first-class design constraints)

**On document ingest (PDF parsing):**
- *No text layer (scanned image PDF)* → detect pages where extraction yields < 5 chars/page; show "Scanned PDF — OCR not yet available" badge instead of silent emptiness (OCR via Tesseract = Phase 8).
- *Encrypted PDFs* → PdfPig supports user passwords; prompt once, fail gracefully with retry.
- *Multi-column / magazine layouts* → default extractor mangles reading order; use DocstrumBoundingBoxes segmenter + UnsupervisedReadingOrderDetector (verified in PdfPig wiki) and fall back to visual-order sort by Y when reading-order detection produces < expected count.
- *Fake-bold duplicate letters* → run `DuplicateOverlappingTextProcessor.Get(letters)` (known PdfPig pitfall, straight from their FAQ) to avoid "TThhiiss" text.
- *Hyphenated line breaks* ("read-\ning") → un-hyphenate in the sentence builder so TTS says "reading" not "read ing".
- *Headers/footers/page numbers* → repeated short single-line blocks at top/bottom of every page get dropped from the spine via a cross-page repetition heuristic.
- *Corrupt/truncated PDF* → open in try/catch; surface file name + reason, never crash the app.

**At the TTS boundary:**
- *Very long sentences (lists, tables read as one line)* → hard-split synthesis inputs at 120 chars on word boundaries; tables/lists become one chunk per row.
- *First-audio latency* → pipeline synthesizes sentences N..N+2 while sentence N plays (prefetch queue, cancel-ahead on seek). Target: ≥97% of transitions gapless; worst case measured, not guessed.
- *Model download interrupted* → download to `.part`, verify size+SHA (when published), atomic rename; registry marks partial.
- *Voice license ≠ engine license* → per-voice LICENSE shown in voice manager; we only ship the app, voices are downloaded by user from official releases (keeps our repo license clean Apache/MIT).

**On the device / client:**
- *Playback device change / headset unplug* → NAudio renders to default device per stream; on device-lost exception, auto-pause and re-open.
- *Sleep/resume mid-sentence* → on app resume, snap highlight to sentence start (never mid-word desync).
- *CPU-only laptops (target user machine)* → Kokoro-82M runs ~2-4× realtime on 4-core CPU via sherpa-onnx with NumThreads=2 (docs + Unity-package reports); Piper medium runs >10× realtime → engine tier chosen by device benchmark on first run.
- *Memory on 800-page books* → pages parsed lazily (parse page k when reader scrolls to it; spine entries hold text + bbox, not images); audio chunks are streamed and dropped after play, never a full-book WAV.

**Operational:**
- *Resume integrity* → save last sentence index every 10 s + on pause/close; corrupt row → start at 0, no crash.
- *Voice file missing* → voice manager marks not-downloaded; engine init fails soft and UI falls back to "download a free voice" screen.

---

## Phase 0 — Foundation & Feasibility Spikes (execute immediately)

**Goal:** solution scaffold builds + runs; both risky assumptions (PdfPig sentence extraction quality, sherpa-onnx offline synth on this machine) proven with a throwaway console spike *before* any UI work.

### Task 0.1: Create solution scaffold
**Files:** Create `LeafVox.sln`, `Directory.Build.props`, `src/LeafVox.App/`, `src/LeafVox.Core/`, `src/LeafVox.Tts/`, `tests/LeafVox.Core.Tests/`.
Steps:
1. `dotnet new classlib -n LeafVox.Core -o src/LeafVox.Core -f net10.0` (same for `LeafVox.Tts`).
2. Install Avalonia templates: `dotnet new install Avalonia.Templates`, then `dotnet new avalonia.app -n LeafVox.App -o src/LeafVox.App`.
3. `Directory.Build.props`: `net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`.
4. Wire project refs: App → Core, Tts; Tts → Core.
5. Verification: `dotnet build LeafVox.sln` → 0 warnings, 0 errors; `dotnet run --project src/LeafVox.App` opens an empty window (kill after check).

### Task 0.2: Spike A — PDF → sentence spine (console)
**Objective:** prove PdfPig gives us clean, reading-ordered sentences + bounding boxes on a real PDF.
- `dotnet add src/LeafVox.Tts package PdfPig` (test in a `spikes/SpikePdf` console instead; package version pinned `0.1.16`).
- Test input: generate/download a 2-page PDF with multi-column text (use PdfPig's own `PdfDocumentBuilder` to emit a test PDF with known text — deterministic, no network).
- Assert: extracted sentence strings == expected sentences; every sentence has a non-empty bbox list; duplicate-letter processor applied.
- Verification: `dotnet run` prints PASS and the first 5 sentences with page numbers.

### Task 0.3: Spike B — offline neural TTS → WAV (console)
**Objective:** prove sherpa-onnx C# synthesizes a WAV on this Windows box with a free model.
- `dotnet add package org.k2fsa.sherpa.onnx` + `org.k2fsa.sherpa.onnx.runtime.win-x64`.
- Download a Piper medium voice (e.g. `en_US-lessac-medium`, ~60 MB, from `github.com/erogol/sherpa-onnx-piper-models` / official sherpa tts-models release) into `%LOCALAPPDATA%\leafvox\models\`.
- Synthesize one paragraph to WAV via `OfflineTts` + `GenerateWithConfig`, print elapsed vs audio duration (RTF measurement — the number that decides whether Kokoro ships as flagship or tier-2).
- Verification: WAV exists, plays, RTF printed. If download > 5 min on this connection → do Piper tier first, Kokoro in Phase 5 (voice manager).

**Handoff:** report spike numbers to user before Phase 1.

---

## Phase 1 — Core: Reading Spine (`src/LeafVox.Core`)

Domain model (all pure C#, no PDF/UI types leaking out):
- `PdfBook` (wraps `PdfDocument.Open`; lazy page accessor; metadata, page count, encrypted flag).
- `PageSpine` → `Block` → `Sentence { Text, PageNumber, BoundingBoxes (IReadOnlyList<Box>), Order }`.
- `ReadingSpine` — flat, ordered `IReadOnlyList<Sentence>` = the thing the player walks; supports `IndexFrom(page,y)` for "start reading from here" and `Next/Prev` sentence navigation (the user's line-by-line request).
- `SentenceBuilder` — joins words into sentences (abbreviation-aware splitter: `e.g.`, `Mr.`, decimals, all-caps runs), un-hyphenates line breaks, drops header/footer repeats.
- `PipelineSelector` — default vs Docstrum+UnsupervisedReadingOrder vs Y-sorted fallback (pick the pipeline whose sentence count vs letter count ratio is healthiest).
TDD per class with PdfPig fixtures (incl. edge-case PDFs: hyphenation, multi-column, fake bold, scanned-empty).

## Phase 2 — Tts: Engine + Prefetch Pipeline (`src/LeafVox.Tts`)

- `IVoiceRepository` — registry of installed/available free voices (id, tier, size, license, download URL).
- `VoiceDownloader` — `.part` + atomic rename + sha check flow (edge case above).
- `ITtsEngine` (`Synthesize(sentence, speed, ct) -> PcmFloatChunk`); impl `SherpaOnnxEngine` (config builder for Piper & Kokoro model dirs, `NumThreads=2`, `Provider=cpu`).
- `SpeechPipeline` — bounded channel of pre-synthesized sentences (depth 3), `Play/Next/Prev/Seek(sentenceIndex)/Stop` commands, gapless handoff between chunks, speed via `genConfig.Speed` (0.5–3.0).
- `IAudioSink`: `NaudioSink` (Windows/WASAPI) now; `miniaudio` sink Phase 6.
- `PlaybackController` — raises `SentenceStarted/SentenceFinished/PositionChanged` events UI binds to.

## Phase 3 — App shell + Reader UI (Avalonia 12)

- Window chrome: dark-first design, custom title bar, command palette later. Two view modes: **Reflow view** (spine text, virtualized `ItemsRepeater`, fastest) and **Page view** (Docnet-rendered page bitmap with sentence bbox overlay) — Reflow is default.
- Karaoke highlight: current sentence background accent + auto-scroll-into-view (scroll only when off-screen; smooth via `Transitions`).
- Transport bar: play/pause, prev/next *line*, speed stepper, seek slider across whole spine, voice quick-picker.
- "Start from here": click/tap any sentence → `Spine.IndexFrom` → pipeline seek. Also context menu: Read from here / Read this line only / Bookmark.
- Open dialog + recent files (SQLite library table), resume position per book.
- Perf rules baked into plan: parse pages on background thread (`IAsyncEnumerable<PageSpine>`), never on UI thread; UI thread only receives sentence-index updates; virtualization mandatory for >50-page books.

## Phase 4 — Reading UX polish
Bookmarks, sleep timer, per-book voice+speed override, keyboard-only control (Space, ←/→, [/], PgUp/PgDn jump chapter-ish), "read this selection", desktop notification/media-key integration (SystemMediaTransportControls on Win; other platforms later).
*Optional Task R-1:* if we ever want to study a commercial reader's client-side highlight-sync technique for inspiration, the REA tooling (npx rea-agents) can passively inspect a public web page via CDP. Not needed — our event-driven sentence pipeline already delivers the UX — and any such study informs mechanics only; no copied assets, code, branding, or proprietary voices (cloud voices behind auth can't be "borrowed" anyway under the $0 constraint).

## Phase 5 — Voice Manager (browse & download free voices, $0)
Grid of free voices by tier: Kokoro (natural, ~330 MB: af_heart, af_bella, am_michael, bf_emma, bf_isabella, bm_george…), Piper multilingual (light: 20–60 MB, 40+ languages), each with sample-audio preview (tiny baked-in samples or on-demand synth), license line, progress state from `VoiceDownloader`. This is the feature that fulfills "all the best free natural voices available locally."

## Phase 6 — Cross-platform runway
`IAudioSink` miniaudio impl (mac/linux/android); `Directory.Build.props` multi-target prep; Avalonia head re-target checklist (`net10.0-macos/ios/android` workload notes in README); CI-free local build scripts `scripts/publish-win|linux|mac.sh` (per user policy: **no GitHub Actions ever**).

## Phase 7 — Packaging & docs
Self-contained single-folder publish per RID (`dotnet publish -r win-x64 --self-contained` → free, offline install zip), icons, README with per-component license table, THIRD-PARTY-NOTICES.md (Apache/MIT attribution + voice license policy).

## Phase 8 — Deferred/stretch
Tesseract OCR for scanned PDFs; sentence-level paragraph reflow for academic PDFs with math; Android tablet layout; whisper-style word-level highlighting (Kokoro doesn't emit alignments → interpolate by char ratio; revisit if needed).

---

**Testing strategy:** xUnit for Core/Tts (spine fixtures, splitter property tests, pipeline cancellation tests); Avalonia headless-test for viewmodel wiring; manual smoke checklist per phase in `.progress/` reports.

**Risks & mitigations:** sherpa-onnx native lib bloat per platform → accepted (Apache, offline). Kokoro RTF on weak CPUs → tier picker benchmark in Phase 5. PdfPig bbox drift on Linux without fonts → documented fix (install liberation fonts; DPI note in README). Docnet PDFium iOS build → Phase 6, Page-view is optional mode so spine keeps working without it.
