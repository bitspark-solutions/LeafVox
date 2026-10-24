# Third-Party Notices — LeafVox

LeafVox itself is MIT-licensed. It links/runs atop the following open-source
components. Licenses below are as of the pinned versions in
`Directory.Packages.props` (2026-10-23).

| Component | Version | License | Used for |
|---|---|---|---|
| Avalonia (UI, Themes.Fluent, Fonts.Inter, Desktop) | 12.1.4 | MIT | App UI, cross-platform windowing |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | MVVM source-generated view-models |
| PdfPig (UglyToad.PdfPig) | 0.1.16 | Apache-2.0 | PDF parsing, text + layout extraction |
| PDFtoImage | 5.2.1 | MIT | PDF page rendering (pdfium → SkiaSharp) |
| SkiaSharp (+ native assets) | 3.119.2 | MIT | raster surfaces for page view |
| bblanchon.PDFium (Win32/Linux/macOS) | 147.x | BSD-3 (PDFium) | PDF rasterization engine |
| Tesseract (charweini) | 5.2.0 | Apache-2.0 | OCR fallback for scanned pages (Windows natives bundled) |
| sherpa-onnx (org.k2fsa.sherpa.onnx + per-RID runtime pkgs) | 1.13.8 | Apache-2.0 | Offline neural TTS inference (ONNX Runtime based) |
| ONNX Runtime (transitive via sherpa-onnx binaries) | — | MIT | Model execution |
| NAudio + NAudio.Dmo | 3.3.0 | MIT | Windows audio playback (DirectSound) |
| Miniaudio-CS (Estrol) | 1.1.0 | MIT (bindings); miniaudio itself is public domain (unlicense) | macOS/Linux audio playback |
| eSpeak NG (data shipped inside TTS model archives) | — | GPL-3.0 (data + phonemizer inside third-party model artifacts) | Phonemization for Piper/VITS/Kokoro voices — distributed by the model archives, not by this repo |
| xUnit / Microsoft.NET.Test.Sdk / coverlet | as pinned | MIT / Apache-2.0 | Testing only |

## Voices and models
Voice models are downloaded at runtime by the user from the official
`k2-fsa/sherpa-onnx` GitHub releases. They are NOT redistributed by this repo.
Each archive carries its own MODEL_CARD / LICENSE:

* Kokoro-82M models — Apache-2.0 (weights and code).
* Piper VITS voices — engine Apache-2.0; per-voice dataset licenses vary
  (shown inside each voice's MODEL_CARD, surfaced in LeafVox's voice manager).
* Mimic3 voices — see bundled MODEL_CARD (typically CC-BY-NC-SA; personal use).

Displaying the license line per voice in the voice manager satisfies the
attribution requirements; redistribute any voice elsewhere only per its own terms.

## OCR language data
`eng.traineddata` is downloaded by the user on first "OCR scanned pages" click from
the official `tesseract-ocr/tessdata_fast` repo (Apache-2.0, ~4 MB). Not redistributed.
