---
name: Awesome Design
description: Design-system reference library (DESIGN.md format) for UI work. Use whenever designing, restyling, or auditing a UI — pick the design system whose aesthetic matches the product, apply its tokens (color palette, typography scale, spacing, depth, component styling), and stay consistent with it. Includes curated system analyses: Raycast (sleek dark chrome, gradient accents), Superhuman (premium dark, keyboard-first), Linear (ultra-minimal, precise).
---

# Awesome Design

Curated DESIGN.md design systems (from [VoltAgent/awesome-design-md](https://github.com/VoltAgent/awesome-design-md), MIT — analysis of publicly observable patterns, not affiliated with the brands).

## How to use

1. Read the product's intent and pick the closest aesthetic in `designs/`:
   - `raycast-DESIGN.md` — productivity tool, sleek dark chrome, vibrant accent gradients
   - `superhuman-DESIGN.md` — premium dark UI, keyboard-first, soft accent glow
   - `linear.app-DESIGN.md` — ultra-minimal, precise, single accent, high information density
2. Extract the design tokens (palette + roles, type scale, spacing scale, radius/shadow system, component styling with hover/selected/disabled states).
3. Adapt — never copy blindly: keep the product's own identity colors and semantics, but adopt the *structure*: consistent spacing scale, restrained accent usage (one accent color reserved for primary action + active state), disciplined type hierarchy, elevation through subtle surface steps rather than borders.
4. Record the final choices in the project root `DESIGN.md` so later UI work stays on-brand.

## Non-negotiables to carry over from every system

- One accent, used sparingly: primary CTA, active item, focus ring. Never gradient-on-everything.
- 4/8 px spacing rhythm; paddings grow with container size.
- Text hierarchy through size + weight + opacity, not color noise; secondary text ≥ 60% contrast of primary.
- Restraint: thin 1px separators at low opacity, radius consistent (4–10 px), max one level of nesting.
- Dark surfaces stepped by lightness (canvas < surface < elevated), never pure black-on-black.
