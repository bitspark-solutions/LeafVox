# LeafVox — Phases 5–7 Progress Report

Date: 2026-10-23 (same-day continuation of `2026-10-23_phase0_foundation_progress_report.md`)
Plan: `.plan/2026-10-23_master_plan.md` · Remote: github.com/bitspark-solutions/LeafVox (main)

## What got built since the last report

### Phase 1 — Reading spine (pushed `5da274e`)
`PdfBook, LineExtractor, SimpleSegmenter, SentenceBuilder, HeaderFooterFilter, TextNormalizer, ReadingSpine (+FromSentences test seam), Box/Line/PageText/Sentence` — all pure `net10.0`, one class per file per owner rule. 23 tests.
API discoveries (vs training data): `PdfRectangle.Centroid` not `.Center`; `TextBlock.TextLines` not `.Lines`; `doc.Information` not `.Metadata`; `ParsingOptions` has `Password/SkipMissingFonts`, no `UseDctDecode`; NN extractor emits empty whitespace Words (trim+filter).

### Phase 2 — TTS engine + pipeline (pushed `d7258e9`)
`ITtsEngine/SherpaOnnxEngine, IAudioSink/NaudioSink/CountingProvider/IAudioPositionProvider, PcmChunk/PcmBatch, ChunkSplitter (≤120 chars), SpeechPipeline (bounded channel prefetch=2), VoiceCatalog (14 voices, all URLs+sizes live-verified against the tts-models release), VoiceDownloader (.part + tar + tolerant dir find), VoiceRepository, Paths, ModelKind/VoiceTier/InstallState/InstalledVoice`. 13 tests.
Notable fixes: NAudio 3 split DirectSound into `NAudio.Dmo`; `BufferedWaveProvider.Read` is no longer virtual (composition wrapper instead); `IWaveProvider.Read(Span<byte>)` new signature; pipeline highlight events paced off consumed-audio position (not wall clock) so the karaoke marker can't run ahead of the sound; epoch-swap race in `Play()` removed.

### Phase 3 + Voice UI — Reader app (pushed `62e13b3`)
Avalonia 12 MVVM: `MainViewModel` (parse off-thread, transport, karaoke SetCurrent+ScrollIntoView), `SentenceVm`, `MainView` dark UI (header, virtualized reflow list, transport bar, speed slider 0.5–3×, context menu "Read from here", double-tap), `VoiceManagerWindow` (catalog grid + progress + Use-this), live integration tests (real lessac voice end-to-end through the pipeline).
Avalonia 12 gotchas: `TextBlock` has no `CornerRadius` (Border wrapper for highlight); no `Window.GetWindow` (use `TopLevel.GetTopLevel`); flyout `Opening` is `EventHandler` (EventArgs, not RoutedEventArgs). App smoke: launches, stays up, zero exception output.

### Phase 4 — UX polish (pushed `ab5919b`)
`AppSettings/SettingsStore` (JSON, atomic-free simple), `LibraryEntry/LibraryStore` (recents + resume position + per-book speed), resume-on-open with highlight restore, save-every-10-sentences + on stop/finish, `PlayOnce` sentence pipeline seam (+test), sleep timer (🌙 15m toggle), read-this-line button, keyboard (`Space/←/→/Esc`), recent files flyout built on `Opening`, active-voice pick (`Use this` → settings → engine unload on change). 38 tests green at this point.
Deviation from plan (documented): persistence is **JSON under %LOCALAPPDATA%/leafvox/data**, not SQLite — one-file store, zero native deps, identical for the port targets; Microsoft.Data.Sqlite stays out. Bookmarks LIST UI deferred to Phase 8 (read-from-here + resume cover the stated need).

### Phases 5 + 6 — Natural tier verified + cross-platform runway (pushed `14652eb`)
- Downloaded and verified **Kokoro-82M (kokoro-en-v0_19, 320 MB)** through the SAME code paths the app uses: extracted `model.onnx + voices.bin + tokens.txt + espeak-ng-data + LICENSE`; new test `Kokoro_Natural_Voice_Synthesizes_Real_Audio` asserts real seconds of audio at CPU RTF < 3 (passes; light tier ran RTF 0.06 on this machine).
- `MiniAudioSink` — unsafe miniaudio bindings (`Estrol.Miniaudio-CS`, ships win/linux/mac natives): static `UnmanagedCallersOnly` data-callback pulling from a reservoir queue, device-pointer→instance registry (no GC holes), clock freezes on pause. **Spike C on the live Windows device: played 0.85 s of 1 s written; pause holds; resume advances** → same C# code is the macOS/Linux path.
- `SinkFactory` (Windows→NAudio proven path, else MiniAudio), per-OS conditional sherpa-onnx native runtime PackageReferences in the Desktop head, `scripts/publish-{win,linux,mac}.sh`, `verify-all.sh` now runs Spike C too.
- Android/iOS heads: on disk since Phase 0 (`avalonia.xplat`); enabling = `dotnet workload install android ios` + adding runtime.android-arm64 package ref — deliberately NOT flipped on this machine (no workloads, would break the solution build; owner decision when a Mac/Android device is in hand).

### Phase 7 — Packaging & docs (this commit)
`LICENSE` (MIT, Bitspark Solutions), `THIRD-PARTY-NOTICES.md` (pinned versions + per-voice license policy + eSpeak-NG-in-archive note), README rewritten with measured numbers, `.gitattributes` (kills CRLF churn), verify-all gate includes: build → 39 tests → 3 spikes.

## Repo rules enforced
- One class per file everywhere (owner rule 2026-10-23) — swept; only template false-positives remain.
- Brand hygiene: competitor-name sweep re-run clean (`grep -ril` → 0 files).
- No hosted CI; everything provable locally via `scripts/verify-all.sh`.

## Current totals
- 39 unit/integration tests green + 3 device spikes (PDF spine 5/5, Piper RTF 0.06–0.13, MiniAudio live pass) + Kokoro live test.
- 8 pushes to origin/main: 9382ff3 → this one.

## Phase 8 (deferred, unchanged from plan)
Page-view (Docnet/PDFium overlay), OCR (Tesseract), mobile workload enablement + stores, bookmark list UI, word-level timing, media-key/SMTC integration.
