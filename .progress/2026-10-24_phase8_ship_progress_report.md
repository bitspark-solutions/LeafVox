# LeafVox — Phase 8 Ship Report (page view, OCR, bookmarks)

Date: 2026-10-24 · Plan: `.plan/2026-10-23_master_plan.md` · Commits: `2934a2e`, `b8fd8c4` (+ README touch-up) on origin/main

## Delivered this pass

### Page view (Phase 3/8 "original layout" gap — closed)
- Docnet 2.6 evaluated first: renderer gated behind a license-token model → **rejected under the $0 rule**.
- Replaced with **PDFtoImage 5.2.1** (MIT; SkiaSharp + bblanchon pdfium natives for win/linux/mac/android/ios — verified from the nupkg).
- `PageRenderer` (LRU-cached, png-for-OCR path included), header **📄 Page / 📃 Flow** toggle, page image in a Viewbox with a Canvas overlay drawing the current sentence's boxes (pt→px via measured pixel scale), auto-scroll to the highlight, **double-tap anywhere on the page → read from that sentence**.
- Spike D (now in verify-all): 2-page render, page sizes, ink-pixel proof, 104 ms → 42 ms warm.

### OCR for scanned pages
- `Tesseract 5.2.0` (Apache-2.0; Windows natives bundled, system libtesseract elsewhere — surfaced as a clean message).
- `eng.traineddata` (~4 MB, Apache-2.0, tessdata_fast) auto-downloads to app data on first use.
- `ReadingSpine.Build(book, lineOverrides)` seam + Core test `LineOverrides_Rescue_Scanned_Pages`.
- VM: `HasScannedPages` → 🔍 OCR button, per-page 300 dpi raster → line+box recovery → spine rebuild keeping selection.
- Spike E: **exact text round-trip from a pixels-only page in 496 ms**, boxes sane (top=791 vs placed 780+ascent).

### Bookmarks + media keys
- `LibraryEntry.Bookmarks` + `LibraryStore.ToggleBookmark`; ★ button on current sentence; 📑 flyout jump list.
- `MediaPlayPause/Stop/Next/Previous` transport while focused (Avalonia key names verified by compile).

## Verification (final)
`bash scripts/verify-all.sh` → build ✓ · **40/40 tests** (24 Core + 16 Tts incl. both live-model tests) · one-class-per-file CLEAN · **spikes A–E all PASS** · `== ALL VERIFIED ==` GATE_EXIT=0. App smoke-launch: 6 s alive, no errors (with page-view code active).

## Still deferred (documented in README)
Android/iOS workload enablement + store packaging · word-level timing · OS-level media integration (SMTC/MPRIS) · mobile layouts. Everything the owner asked for on 2026-10-23 is now shipped: open a PDF, continuous natural local voice reading with highlight, start from any spot, $0 across the board.
