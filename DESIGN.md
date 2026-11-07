# DESIGN.md — LeafVox desktop (Observatory Night)

Durable visual decisions, recorded from the built implementation. Product truth lives in
`PRODUCT.md`; the strategy contract for this surface lives in `.impeccable/surfaces/app-shell.md`.

## Concept

The window is a **night-sky chart plate on a deep blue-black observatory desk**. The open PDF
is the only warm object on the desk — a paper plate floating on the abyss — and the line being
spoken is ringed in **night-vision amber-red**, exactly like a chart annotation under a red
flashlight. The interface is the annotation layer around a paper object, not a dashboard.
Secondary information reads like chart coordinates: tiny, tabular, quiet micro-labels.

Direction: Observatory Night (impeccable concept-seed, assigned candidate 4,
seed `a2b77bb69b57301a750128e97e6393b3`, Operate mode).

## Tokens (defined in `src/LeafVox.App/LeafVox.App/App.axaml`)

### Surfaces — stepped by lightness, never pure black
| Token | Value | Role |
|---|---|---|
| `Brush.Abyss` | `#0A0D16` | Window canvas, page-viewer background |
| `Brush.Panel` | `#10151F` | Command deck, transport rail |
| `Brush.Elevated` | `#161C29` | Menus, elevated overlays |
| `Brush.Hairline` | `#16FFFFFF` | 1px separators between rails |
| `Brush.Stroke` | `#2AFFFFFF` | 1px outlines of chips |

### Text — chalk on slate (never gray-on-gray tinting)
| Token | Value | Contrast on Abyss/Panel |
|---|---|---|
| `Brush.Chalk` | `#E9EDF5` | body/primary, ~16:1 |
| `Brush.Chalk.Secondary` | `#98A3B8` | micro labels, inactive words, ~7:1 |
| `Brush.Chalk.Faint` | `#7C87A3` | folio page labels, hints, ~4.6:1 (kept above the floor) |

### Accent — one night-vision amber-red, three jobs only
Spoken-line annotation, play/playing state, the single primary action.
| Token | Value |
|---|---|
| `Brush.Accent` | `#FF7A50` |
| `Brush.Accent.Deep` | `#E4573C` (primary button background; **warm ink `#241109`** on it, not white — white touches ~3.7:1) |
| `Brush.Accent.Hover` | `#FF9C78` |
| `Brush.Accent.Wash` | `#26FF7A50` (current-row tint, page highlight fill) |
| `Brush.Accent.Ghost` | `#0DFF7A50` (hover wash) |
| `Brush.Ready` | `#7BD389` (installed/ready state in voice manager only) |
| `Brush.Close.Hover` | `#E81123` (window close button only) |

Accent is *never* used for: text color of normal labels, borders >1px, gradients, or decoration.

## Type

- Single family (Fluent defaults). Sizes: 11 micro / 13 deck+titles / 14 voice-row titles /
  16 flow body (line-height 24) / 15 empty-state line.
- **Micro-labels (11px, `TextBlock.micro`)** are the coordinate voice: statuses, positions,
  speed readout, hints. `folio` (11px, Faint, Medium) marks page numbers.
- Status flashes then **holds ~4 seconds** in accent (`statusHot` class bound to `StatusHot`),
  then settles back to quiet chalk-secondary — a change is seen once, not persisted.

## Layout

```
┌─ custom title rail 42px ────────────────────────────────────────┐
│ leaf mark · LeafVox / listening reader   [— ☐ ✕]               │ ← abyss, hairline below
├─ command deck ─────────────────────────────────────────────────┤
│ [Open PDF]  Recent  Flow  ☆  ⌘   BookTitle   OCR  voice ─ chip │ ← panel, hairline below
│                                      Status (flash+hold)       │
├─ reader ───────────────────────────────────────────────────────┤
│  page plate (pdfium)  │ flow rows (amber 2px marker on spoken) │ ← abyss
├─ transport rail ───────────────────────────────────────────────┤
│ ║2px accent underline = playing state (visible ⇔ IsPlaying)    │
│ ^ ▶(wedge) v ■   line i / N ──scrubber──   Line once, Sleep,   │ ← panel
│                                            speed ─s── 0.75×    │
└────────────────────────────────────────────────────────────────┘
```

- One accent **room** button (`Open PDF`) in the deck; everything else is quiet deck words,
  hairline chips, or drawn glyphs.
- Transport icons are **drawn vector paths** (chevrons for line steps, wedge/bars play-pause,
  square stop, star + ribbon bookmarks, leaf mark); `$parent[Button].Foreground` keeps them
  state-aware. **No emoji/unicode glyphs anywhere in the UI.**

## Window chrome (Avalonia 12)

`WindowDecorations="BorderOnly"` + `ExtendClientAreaToDecorationsHint` — system border and
resizing preserved, theme title bar removed; our own 42px rail draws the mark, wordmark,
drag zone (`BeginMoveDrag`), double-tap maximize, and drawn minimize/maximize/close
(close hovers `#E81123`).

## Signature behaviors

- **Playing underline**: a 2px accent line at the top of the transport rail appears only while
  speaking — state carried by the line's presence, not by hue swaps on controls.
- **Spoken line on the plate**: page-viewer highlight rects use `Accent.Wash` fill + 1.2px
  `Accent` stroke; flow rows get `Accent.Wash` tint + a 2px accent marker at the row's left edge.
- **Dashed = missing discipline**: a voice chip reads `voice: —` until a voice is set; empty
  deck states are absence of content, not error chrome.
- **Empty state teaches**: drawn leaf glyph, "Open a PDF to begin listening", and the keyboard
  map as a micro hint (`Space play · ← → line · Esc stop`).
- **Whole-book scrubber**: Thumb drag freezes live sync (`BeginScrub`/`EndScrub`), release seeks
  (audible seek when playing/paused, position-only jump when stopped, no voice needed).

## Craft-floor ledger

- Gradients: none. Glass/blur: none. Kicker/eyebrow labels: none.
- Borders above 1px: none (the 2px playing line and 2px row marker are *annotation lines*,
  reserved accent jobs, not borders).
- Depth: plate/table contrast + hairlines only; no shadows needed on rails.
- Verified contrast: body ≥16:1, secondary ≥7:1, micro/faint ≥4.6:1; Open-button ink ≥4.9:1.

## Verification status

- `dotnet build LeafVox.slnx` clean; 43 tests green (27 Core, 16 TTS; one transient TTS
  flake re-run passed); desktop smoke launch ran and stayed alive.
- **There is no rendered-screenshot verification**: this is a native Avalonia (XAML) app and the
  session has no native capture capability, so the finish look is verified against the built
  XAML/token sources, not pixels. Re-look before trusting fine detail.
