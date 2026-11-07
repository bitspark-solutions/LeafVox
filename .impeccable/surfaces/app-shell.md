# Surface: LeafVox desktop app shell (MainWindow + MainView + VoiceManagerWindow)

Scope: Operate (app UI). Visitor: the owner reading/listening at a night desk.

## DIRECTION CONTRACT

THESIS: The window is a night-sky chart plate on a deep blue-black observatory desk; the open PDF is the only warm object on it, and the line being spoken is ringed in night-vision amber-red — the interface is the annotation layer around a paper object, not a dashboard.

OWN-WORLD: Cool navy-abyss surfaces stepped by lightness (#0A0D16 canvas, #10151F deck, #161C29 elevated), hairline #FFFFFF14 separators, warm paper accents only where a document object sits; one annotation accent #FF7A50 (amber-red, red-flashlight discipline) reserved for the spoken line, playing state, primary action, and held changes; chalk-white text, secondary text at 60–70%; micro-labels at 11px like chart coordinates; no gradient decoration, no colored borders above 1px, icons drawn as vector paths, never emoji.

STORY: "This is my reading desk at night: the page floats here, the reader reads, and I always know exactly which line is speaking." Statuses flash once and hold; navigation is by line and page position, always labeled.

FIRST VIEWPORT: Custom dark chrome title rail (drag area, drawn window controls) above a slim command deck (Open as the single accent action left; view toggle, bookmarks, voice chip right; book title + held status center); center area = the paper plate (page viewer) or banded flow rows; bottom transport rail = drawn-icon transport cluster (prev/play/next/stop), center scrubber with coordinate-style "line i / N", right speed readout + sleep. Accent appears first as the Open action and, once playing, as the underline progress of the transport rail.

FORM: Assigned direction (candidate 4), seed a2b77bb69b57301a750128e97e6393b3 — Observatory Night, code-led build (Operate surface).

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance.

## Audience / job / constraints (from PRODUCT.md)

Listen to PDFs hands-free at a desk; must run offline, free, private; keyboard transport is a product principle; no competitor names anywhere; one class per file; personal night-desk ambiguity precedent (world choice) recorded above.

## States that matter

Playing / paused / stopped; no voice installed (dashed chip = the rail's dashed = missing discipline, donated from the emission-line challenger after its declined verdict); loading voice; OCR offer on scanned pages; empty (no document) teaches the interface; resize between narrow and wide keeps deck and transport functional.

## Unresolved

None blocking; zoom on page stack deferred (pre-existing scope).
