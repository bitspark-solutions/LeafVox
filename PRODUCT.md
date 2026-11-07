# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Users

Primary user: an individual working at a desk (focus/night context confirmed by the owner) who wants to *listen* to PDF documents and books instead of reading them — reports, papers, imported fiction — with their eyes free for note-taking or rest. Reasonable, inexpensive, and privacy-respecting: no accounts, no cloud, no subscriptions. Secondary (staged, not ship-blocking): students and readers requiring non-English voices (the free voice tier includes Bangla, Hindi, Arabic, DE/FR/ES).

## Product Purpose

Open any PDF and have it read aloud sentence-by-sentence with natural neural voices running locally, while the text is karaoke-highlighted in sync. Success = "opened a PDF, pressed play, and kept listening without touching it until done": continuous playback with no perceptible gaps, resumable per book, from any half-read position.

## Positioning

The only completely free listening PDF reader: 100% offline, 100% local voices (Kokoro-82M natural tier + Piper light tier via the sherpa-onnx runtime), no API keys, no telemetry, no hosted CI. A competitor could copy the reader UX but not the *zero-cost, fully local* stack with per-voice free license display.

## Operating Context

Desktop personal computers (Windows/macOS/Linux; Android/iOS/Browser heads staged). Used on CPU-only laptops; the same binary targets multiple OSes. Voice models and OCR language data are downloaded by the user at runtime from official free releases (never redistributed by the repo). Verification is a local script (`scripts/verify-all.sh`), not hosted CI.

## Capabilities and Constraints

- Continuous narration: 2-sentence prefetch pipeline, karaoke highlight, auto-scroll, speed 0.5×–3×, prev/next line, read-this-line-only, resume per book.
- Two reader views: continuous page viewer (rendered pdfium pages — default) and reflowed text flow list.
- Voice manager with one-click free voice downloads, per-voice license display.
- Local OCR rescue for scanned pages (Tesseract). Bookmarks, sleep timer, recents library.
- Hard repo rules: one class per file; **never name competitors anywhere** (code, docs, UI); $0 forever (MIT/Apache/BSD deps only); no hosted CI.
- Performance envelope: CPU-only laptop, Kokoro < 3× realtime, Piper ≥ 8× realtime; page renders at 150 DPI with a rolling 24-bitmap cache.

## Brand Commitments

Name: **LeafVox 🍃🔊** (leaf = page, vox = voice), verified unclaimed 2026-10-23. Voice/tone: friendly, honest, plain-spoken ("read from anywhere", "no subscriptions"). Identity constraints: no competitor names or trade dress; per-voice licenses must be user-visible.

## Evidence on Hand

README.md (feature list, RTF measurements), `.plan/2026-10-23_master_plan.md` (architecture, edge cases), `.progress/*` ship reports, spike PDFs under `spikes/`. No marketing assets, testimonials, or benchmarks beyond the RTF table — future work must not fabricate any.

## Product Principles

1. Trust through locality: everything happens on the user's machine; the UI must feel private and calm, not cloud-tool-like.
2. The book is the star: chrome recedes; reading position and the page are always visible and beautiful.
3. Free means frictionless: downloads/licenses/speed controls surfaced plainly, never nagging.
4. One codebase, many desks: design language must translate Windows/macOS/Linux without OS-pastiche.

## Accessibility & Inclusion

Keyboard-first transport (Space/←/→/Esc + media keys) is a feature, not an afterthought. Continuous narration serves dyslexic and low-vision readers; text sizing, highlight contrast, and large hit targets on the page overlay are first-class. Multi-language voices are part of the core promise.
