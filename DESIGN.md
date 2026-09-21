---
version: alpha
name: RailWeaver
description: Light control room on a drafting table. A calm, gray, industrial planning interface where color only appears for deviations and services, with a quiet nod to Argentine railway lettering.
colors:
  # Surfaces (light, faintly green-tinted neutrals; never pure white)
  ground: "oklch(92.0% 0.004 122)"        # #E4E5E2 app background
  panel: "oklch(94.8% 0.004 122)"         # #EDEEEB top bar, rails, docks
  raised: "oklch(97.3% 0.003 106)"        # #F6F6F4 detail panels, inputs, charts
  border-subtle: "oklch(84.2% 0.007 124)" # #CACCC7 dividers, table rules
  border-strong: "oklch(60.1% 0.010 135)" # #7E827C control outlines (>= 3:1 on raised)
  # Ink
  ink: "oklch(25.7% 0.012 248)"           # #1F2429 primary text
  ink-muted: "oklch(42.5% 0.012 253)"     # #4A4F55 secondary text
  ink-subtle: "oklch(50.0% 0.011 243)"    # #5E6469 labels, units, axes
  # Infrastructure ink: the only "brand" color
  primary: "oklch(27.6% 0.029 251)"       # #1D2936 proposed/built infrastructure, primary buttons
  on-primary: "oklch(97.3% 0.003 106)"    # #F6F6F4
  # Map
  map-ground: "oklch(93.2% 0.005 118)"    # #E8E9E5
  map-contour: "oklch(85.4% 0.009 129)"   # #CDD0CA
  map-contour-major: "oklch(76.5% 0.011 131)" # #B0B4AD
  map-water: "oklch(71.6% 0.039 242)"     # #8FA7BA
  map-urban: "oklch(87.3% 0.007 124)"     # #D4D6D1
  map-road: "oklch(83.2% 0.009 129)"      # #C6C9C3
  map-rail-existing: "oklch(61.1% 0.011 141)" # #80857F
  # Services (muted; never compete with state colors)
  service-passenger: "oklch(46.8% 0.057 202)" # #2E6468
  service-freight: "oklch(46.0% 0.049 330)"   # #684E65
  # States: the only saturated colors in the product
  warning: "oklch(59.1% 0.132 65)"        # #B26A00 strokes, fills, glyphs
  warning-text: "oklch(49.2% 0.110 66)"   # #8A5200 text on raised / warning-surface
  warning-surface: "oklch(91.5% 0.047 84)" # #F2E1C0
  alarm: "oklch(51.5% 0.171 28)"          # #B5302A
  alarm-surface: "oklch(90.9% 0.029 28)"  # #F4DAD6
  action: "oklch(48.0% 0.131 258)"        # #2A5CA6 operator decision required
  action-surface: "oklch(91.6% 0.018 258)" # #DCE4F0
typography:
  label:
    fontFamily: Schibsted Grotesk
    fontSize: 11px
    fontWeight: 500
    lineHeight: 1.3
    letterSpacing: 0.07em
  caption:
    fontFamily: Schibsted Grotesk
    fontSize: 12px
    fontWeight: 400
    lineHeight: 1.35
  body:
    fontFamily: Schibsted Grotesk
    fontSize: 13px
    fontWeight: 400
    lineHeight: 1.4
  body-strong:
    fontFamily: Schibsted Grotesk
    fontSize: 13px
    fontWeight: 600
    lineHeight: 1.4
  title:
    fontFamily: Schibsted Grotesk
    fontSize: 17px
    fontWeight: 650
    lineHeight: 1.25
    letterSpacing: -0.005em
  heading:
    fontFamily: Schibsted Grotesk
    fontSize: 22px
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: -0.01em
  data:
    fontFamily: Fragment Mono
    fontSize: 12.5px
    fontWeight: 400
    lineHeight: 1.35
    fontFeature: '"tnum" 1'
  data-large:
    fontFamily: Fragment Mono
    fontSize: 15px
    fontWeight: 400
    lineHeight: 1.3
    fontFeature: '"tnum" 1'
  identifier:
    fontFamily: Big Shoulders Stencil
    fontSize: 17px
    fontWeight: 700
    lineHeight: 1
    letterSpacing: 0.04em
rounded:
  none: 0px
  sm: 2px
  md: 4px
spacing:
  xxs: 2px
  xs: 4px
  sm: 8px
  md: 12px
  lg: 16px
  xl: 24px
  xxl: 32px
components:
  app-shell:
    backgroundColor: "{colors.ground}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
  map-canvas:
    backgroundColor: "{colors.map-ground}"
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.sm}"
    padding: 7px 11px
  button-secondary:
    backgroundColor: "{colors.raised}"
    textColor: "{colors.ink}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.sm}"
    padding: 7px 11px
  top-bar:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink}"
    height: 44px
  side-rail:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink-muted}"
    width: 168px
  detail-panel:
    backgroundColor: "{colors.raised}"
    textColor: "{colors.ink}"
    width: 320px
    padding: 16px
  status-bar:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink-subtle}"
    typography: "{typography.data}"
    height: 26px
  alert-warning:
    backgroundColor: "{colors.warning-surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    padding: 10px
  alert-alarm:
    backgroundColor: "{colors.alarm-surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    padding: 10px
  chip-action:
    backgroundColor: "{colors.action-surface}"
    textColor: "{colors.action}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.sm}"
    padding: 2px 6px
  metric-value:
    textColor: "{colors.ink}"
    typography: "{typography.data-large}"
  metric-value-warning:
    textColor: "{colors.warning-text}"
    typography: "{typography.data-large}"
  metric-value-alarm:
    textColor: "{colors.alarm}"
    typography: "{typography.data-large}"
  metric-label:
    textColor: "{colors.ink-subtle}"
    typography: "{typography.label}"
  km-post:
    backgroundColor: "{colors.raised}"
    textColor: "{colors.primary}"
    typography: "{typography.identifier}"
    rounded: "{rounded.sm}"
---

# RailWeaver design system

This file is the single source of truth for RailWeaver's visual language. Agents building or changing anything in `src/web` must read it first. Rationale and alternatives are recorded (in Spanish) in `docs/decisions/ADR-009-visual-system.md`; open questions about color semantics and heritage lettering live in `docs/research/visual-identity/`.

## Overview

**A light control room on a drafting table.** RailWeaver is an industrial planning and simulation sandbox, not a game and not a marketing site. The interface borrows two real professional languages:

1. **High-performance HMI (the ISA-101 approach used in industrial control rooms):** the normal state is calm and gray, and color is spent only where something deviates or needs a decision. Anything colored on screen is, by definition, worth looking at.
2. **Technical drawings:** longitudinal profiles, time-distance diagrams, dotted grids, dimension ticks and a title block. These are the native graphics of railway planning.

A third, very quiet layer is **Argentine railway heritage**: stencil lettering, like the marking on rolling stock, used **only** for identifiers and km posts. Never flags, national colors, crests or nostalgia imagery.

Core principles:

- **Normal is gray.** Surfaces, basemap, existing infrastructure and most numbers are neutral.
- **Infrastructure is ink, services are muted hue, deviations are saturated.** These three layers never borrow each other's colors.
- **Every state is color + shape + words.** Color alone never carries meaning (color vision deficiency).
- **Measured values are data.** Anything measured or simulated is set in the mono face with tabular figures and an explicit unit.
- **Explain, don't decorate.** Every warning says what happened, why, and what it causes.

Theme: **light only** for now. A dark theme is a possible future addition built from the same token roles; do not add dark styles until a spec asks for them.

## Colors

The palette is defined in OKLCH (hex in comments for convenience). Neutrals carry a faint green-gray tint (hue ~120) so they read as chosen, not default.

**Surfaces.** Three steps create depth without shadows: `ground` (app background) < `panel` (chrome: top bar, rails, docks) < `raised` (content: detail panels, inputs, charts). Never use pure white or pure black.

**Ink.** `ink` for primary text, `ink-muted` for secondary text, `ink-subtle` for labels, units and axes. All three pass WCAG AA on every surface (`ink-subtle` on `ground` is 4.7:1).

**Primary = infrastructure ink.** `primary` is a deep blue-black used for proposed or built infrastructure on the map, for the primary button, and for the brand wordmark. It is RailWeaver's only brand color.

**Services.** `service-passenger` (muted teal) and `service-freight` (muted plum) color services in charts and on the map. They are deliberately low-chroma so they never compete with a state color. Freight is not brown on purpose: brown would sit on the same hue as `warning`. New service categories must stay at about 45% lightness and chroma ≤ 0.06, and keep a hue distance of at least 40° from `warning`, `alarm` and `action`.

**States.** These are the only saturated colors, each paired with a fixed shape:

| State | Color | Shape | Meaning |
|---|---|---|---|
| Warning | `warning` / `warning-text` on `warning-surface` | triangle | A limit is exceeded or an assumption is at risk; the plan still works |
| Alarm | `alarm` on `alarm-surface` | square | Invalid, unsafe or blocking; must be resolved |
| Action required | `action` on `action-surface` | diamond | The user has a pending decision |
| Disabled / unavailable | `ink-subtle` | circle outline | Not applicable or no data |

- Use `warning-text` (not `warning`) for text; `warning` is for strokes, fills and glyphs (3.9:1).
- **Green is not used for "success" or "OK".** In railways green means *clear track*. Confirmation uses ink plus a check glyph and words.
- Real railway signal aspects (red, yellow, green) may appear **only inside signal symbols**, with their real meaning, once signalling is researched. Until then they are pending; see the research note.
- `map-water` is the only chromatic tone in the basemap, and it is kept desaturated.

## Typography

Three families, all SIL Open Font License, all self-hosted when implemented:

- **Schibsted Grotesk** (UI): labels, body, titles. Fallback: `ui-sans-serif, system-ui, sans-serif`.
- **Fragment Mono** (data): every measured, simulated or coordinate value, times, seeds. Fallback: `ui-monospace, monospace`.
- **Big Shoulders Stencil** (identifiers): corridor, line, train and rolling-stock IDs (`C-01`, `P101`) and km posts (`PK 30`). Nothing else. Fallback: `Fragment Mono`.

Rules:

- The scale is `label 11` / `caption 12` / `body 13` / `data 12.5` / `data-large 15` / `title 17` / `heading 22` (px). Do not add sizes outside it.
- The UI is dense and desktop-first: 13px body is intentional.
- `label` is uppercase with 0.07em tracking, in `ink-subtle`.
- Numbers always use tabular figures (`font-variant-numeric: tabular-nums`), including in UI-font contexts.
- Format numbers with the active UI locale via `Intl` (for es-AR: `38,4 km`, `1,2 %`). The UI language itself is undecided (ADR-008).
- Units follow the value after a space and use `ink-subtle`: `38,4 km`.
- The stencil face is a whisper. If it appears in a sentence, a heading or a button, that is a bug.

## Layout

The map is the product and gets most of the screen. The shell is a fixed desktop layout:

```text
┌──────────────────────── top bar 44 (phases, world, stage, seed) ───────────────────────┐
│ side rail 168 │                     map                          │ detail panel 320     │
│ layers, tools │                                                  │ (persistent)         │
├───────────────┴──────────────── analysis dock (profile, time-distance) ─────────────────┤
└───────────────────────────── status bar 26 (coords, CRS, scale, data notes) ───────────┘
```

- **Persistent info panels** sit at the edges (side rail, detail panel, analysis dock). **Temporary task panels** (configuration, editing) take at most about one third of the width and close when the task ends. The map is never covered by more than one temporary panel.
- The top bar shows the usage loop as ordered phases (Explore → Plan → Build → Schedule → Simulate → Analyze). The numbering is real because the loop is a sequence.
- Spacing uses a 4px base (`xxs`–`xxl`). Inside panels, group rows with 1px rules instead of gaps between cards.
- Label/value lists use two columns with a vertical rule, aligned on baselines.
- The minimum supported width is desktop (≥ 1180px). Narrower viewports are out of scope until a spec says otherwise.

## Elevation & Depth

- No drop shadows on panels, cards or buttons. Depth comes from the three surface steps and 1px borders (`border-subtle` for dividers, `border-strong` for control outlines).
- The only exception is menus and tooltips floating over the map: a single 1px `border-strong` outline plus `0 2px 6px oklch(25% 0.01 250 / 0.12)`.
- On the map, infrastructure lines get a casing in `map-ground` so they separate from contours without a glow.

## Shapes

- Radius is `none` (0) for panels and the shell, `sm` (2px) for controls, chips, alerts and km posts, and `md` (4px) as the maximum anywhere. No pills and no `rounded-2xl`.
- State glyphs are fixed geometry: triangle (warning), square (alarm), diamond (action), circle outline (disabled).
- A station is a square with an ink outline on `raised`. A km post is a small rectangle with an ink outline and stencil text, like the real post by the track.

## Components

- **Buttons:** `button-primary` (infrastructure ink) for the single main action of a panel, and `button-secondary` (outline in `border-strong`) for the rest. Labels say exactly what happens ("Generate variant", not "Continue"). There is at most one primary button per panel.
- **Detail panel:** identifier (stencil) plus a type label, then the title, then a subtitle with provenance ("Generated 2 min ago"). Below: metric list, alerts, pending decision, then actions pinned to the bottom.
- **Metric list:** `metric-label` above `metric-value`. A metric in a deviation state takes the state color **and** its glyph.
- **Alert:** 1px border in the state color, tinted surface, glyph + title, then short paragraphs led by **Why:** and **Consequence:**. Location and values go in the data face. Assumptions are labeled as such ("assumed limit, to validate").
- **Action chip:** diamond + text in `action`. It marks a decision the user owes, not a generic link.
- **Layer list:** square checkboxes (filled ink = on) and a keyboard shortcut hint in the data face.
- **Status bar:** coordinates, CRS, scale, data source and an explicit notice when example data is shown.

## Motion

- UI transitions last 120–200 ms with ease-out, never ease-in, bounce or elastic. Animate only opacity and transform.
- No entrance choreography, no staggered reveals, no ambient animation. Motion exists to preserve context (a panel opening from its trigger, a selection moving), never to decorate.
- Simulation motion (trains moving) is a separate concern: it is driven by simulation snapshots and interpolated by the renderer (ADR-005, ADR-006). It is not UI animation.
- Respect `prefers-reduced-motion`: replace transitions with instant state changes.

## Cartography

- The basemap is subdued and neutral: relief as contours or a gray hillshade, desaturated water, urban areas as flat neutral fills, and roads thin and unlabeled unless a layer asks for them.
- Existing railways are dashed `map-rail-existing`. Proposed or built infrastructure is solid `primary` with a `map-ground` casing. Deviating segments are overdrawn in `warning` or `alarm`, and the triangle or square glyph is placed next to the segment.
- A scale bar and north arrow are always present. Plan-like views and exports get a **title block** (identifier, title, scale, revision, stage), as on an engineering drawing.
- Known conflict: the colorful raster basemap adopted in RW-001 does not follow these rules. Resolving it is part of the frontend migration spec.

## Charts

- **Longitudinal profile:** terrain as a `border-subtle` area plus an `ink` or `primary` line. Exceeded-limit stretches are shaded bands in `warning` at low opacity, with edge lines and a data-face annotation (`max 2.4 % > 1.2 %`).
- **Time-distance diagram (train graph):** time on x, stations on y, services in their service colors (freight dashed), crossings and meets marked with a small outlined circle.
- Grids are dotted `border-subtle` hairlines. Axes use the data face in `ink-subtle`. Every tick label names a value the chart actually reaches.
- No chart library is chosen yet; use inline SVG until a spec justifies one.

## Iconography

- No icon library is adopted yet. Draw the few needed glyphs as inline SVG: 1.5px strokes, square caps, a 16px grid, `currentColor`.
- Avoid generic "AI" glyphs (sparkles, lightning bolts, rocket, magic wand) and icons inside tinted rounded squares.

## Voice

- Specific over clever. Name things the way the user recognizes them (corridor, grade, crossing), and in technical mode use technical terms (headway, block occupancy).
- Every deviation message follows **what / why / consequence**, and marks assumptions and example data explicitly.
- No exclamation marks, no "Oops", no marketing verbs ("elevate", "supercharge").

## Brand mark

**The mark is a meet on a time-distance diagram:**
- Three stations are drawn as horizontal lines and two trains as diagonals.
- One train waits in the middle passing loop (the flat step) while the other passes through.
- It is how trains *weave* through time on a train graph, which is what the name says and what the product does.

Files in `assets/brand/`:

| File | Use |
|---|---|
| `railweaver-mark.svg` | Default mark, 32px and up |
| `railweaver-mark-small.svg` | 24px and below (no station lines, heavier strokes) |
| `favicon.svg` | Browser favicon; switches to `on-primary` when the browser chrome is dark |

Rules:

- **One color only:** `primary` on light surfaces, `on-primary` on `primary` or `ink`. No gradients, outlines, shadows, and no service or state colors.
- **Geometry:** the crossing is a real gap (an SVG mask), not a stroke in the background color, so the mark works on any surface. Do not redraw or re-proportion it.
- **Lockup:** mark + "RailWeaver" in Schibsted Grotesk 700, `primary`. The mark height equals about 1.5× the cap height of the wordmark, with a gap of about 0.3× the mark height. The optional descriptor below the wordmark uses the `label` style.
- **Clear space:** at least a quarter of the mark's height on every side.
- **Minimum size:** 16px, using the small variant.
- **Meaning:** the mark is not a UI state glyph and never carries meaning inside the map or charts.

## Do's and Don'ts

Do:

- Keep the normal state gray and spend color only on states and services.
- Pair every state color with its glyph and a sentence.
- Set every measured value in Fragment Mono with a unit.
- Use the stencil face only for identifiers and km posts.
- Reference tokens (CSS custom properties generated from this file). Never hard-code colors.
- Show provenance and uncertainty (example data, assumed limits, confidence).

Don't:

- Don't use green for success, and don't use red, amber or blue decoratively.
- Don't use Inter, Roboto, Arial, Space Grotesk or system fonts as the primary face.
- Don't add gradients, glassmorphism, glows, drop shadows on cards, or colored accent bars on cards.
- Don't use pills, `rounded-2xl`, or uniform `shadow-lg` cards.
- Don't use Argentine flag colors, crests or nostalgic imagery; the heritage is only in the lettering.
- Don't add a dark theme, icon library or chart library without a spec.
