---
name: Lumina Tutors
description: Multi-tenant education management system for Vietnamese private schools — authoritative, refined, trustworthy.
colors:
  navy: "#0f1c2e"
  navy-light: "#1a2d45"
  navy-muted: "#243650"
  gold: "#c9a84c"
  gold-light: "#e2c97e"
  gold-pale: "#f5ecd1"
  ivory: "#faf8f4"
  surface: "#fefefe"
  success: "#27ae60"
  danger: "#e74c3c"
  warning: "#f39c12"
  info: "#2980b9"
  text-primary: "#2c3e50"
  text-secondary: "#374151"
  text-muted: "#9ca3af"
  border-default: "#d1d5db"
  border-light: "#e5e7eb"
  bg-muted: "#f3f4f6"
typography:
  display:
    fontFamily: "'Playfair Display', Georgia, serif"
    fontSize: "2.4rem"
    fontWeight: 700
    lineHeight: 1.2
  headline:
    fontFamily: "'Playfair Display', Georgia, serif"
    fontSize: "1.6rem"
    fontWeight: 700
    lineHeight: 1.3
  title:
    fontFamily: "'Playfair Display', Georgia, serif"
    fontSize: "1.15rem"
    fontWeight: 700
    lineHeight: 1.4
  body:
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: 1.6
  label:
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
    lineHeight: 1.4
    letterSpacing: "0.06em"
rounded:
  sm: "8px"
  md: "12px"
  lg: "16px"
  pill: "9999px"
spacing:
  xs: "8px"
  sm: "12px"
  md: "24px"
  lg: "32px"
  xl: "60px"
components:
  button-primary:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.surface}"
    rounded: "{rounded.sm}"
    padding: "9px 20px"
  button-primary-hover:
    backgroundColor: "{colors.navy-light}"
    textColor: "{colors.surface}"
  button-gold:
    backgroundColor: "{colors.gold}"
    textColor: "{colors.navy}"
    rounded: "{rounded.sm}"
    padding: "9px 20px"
  button-gold-hover:
    backgroundColor: "{colors.gold-light}"
    textColor: "{colors.navy}"
  button-outline:
    backgroundColor: "transparent"
    textColor: "{colors.text-secondary}"
    rounded: "{rounded.sm}"
    padding: "9px 20px"
  nav-item-default:
    backgroundColor: "transparent"
    textColor: "rgba(255,255,255,0.75)"
    padding: "10px 24px"
  nav-item-active:
    backgroundColor: "rgba(201,168,76,0.12)"
    textColor: "{colors.gold}"
    padding: "10px 24px"
  card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.md}"
    padding: "24px"
  input-default:
    backgroundColor: "{colors.surface}"
    textColor: "#111827"
    rounded: "{rounded.sm}"
    padding: "10px 14px"
  input-focus:
    backgroundColor: "{colors.surface}"
    textColor: "#111827"
    rounded: "{rounded.sm}"
    padding: "10px 14px"
---

# Design System: Lumina Tutors

## 1. Overview

**Creative North Star: "The Headmaster's Register"**

A school register is an authoritative object: precise columns, institutional typography, a physical weight that signals seriousness. Every entry in it matters. Lumina Tutors' design system draws from this metaphor — a premium professional tool that carries the composure of an institution that has been trusted with real students and real accountability. The deep navy and restrained gold are ink and seal, not brand spray. Playfair Display headings land on the screen with the same gravity they would on an official certificate.

This is not a product that wants to look modern. It wants to look correct. The ivory content surfaces feel like quality paper; the navy sidebar feels like the binding of a serious book. Gold marks what matters (active states, primary call-to-action, the brand mark) and is used nowhere else. The system earns trust through consistency and precision, not through personality gestures.

The design explicitly rejects: generic SaaS bootstrapped dashboards with white backgrounds and blue primary buttons that could belong to any product; playful edtech aesthetics with gamification, bright primary colors, or Duolingo-style illustration; enterprise-grey SharePoint energy with no visual identity; neon dark fintech interfaces that belong to a trading platform, not a school.

**Key Characteristics:**
- Deep navy structural shell surrounding warm ivory content surfaces
- Gold used at strict rarity: active navigation, primary CTA, brand mark only
- Serif display typeface (Playfair Display) for all headings; neutral sans-serif (Inter) for everything else
- Flat surfaces by default; depth appears only as a response to interaction or hierarchy
- Vietnamese-first: formal institutional tone, not startup-casual
- Role-differentiated density: teacher workflows are quick and tactical; accountant workflows are data-dense

## 2. Colors: The Register Palette

Two primary colors carry the entire structural load. Everything else is functional.

### Primary
- **Headmaster's Navy** (`#0f1c2e`): The dominant color. Sidebar background, primary button fill, heading text, border accents. Used at full saturation when structural authority is needed.
- **Council Room Navy** (`#1a2d45`): Slightly lifted navy for sidebar hover states, gradient endpoints on primary buttons. Creates depth within the navy range without introducing a new hue.
- **Archive Navy** (`#243650`): Lower-emphasis navy. Used in sidebar gradient layers and muted navy contexts.

### Secondary
- **Honor Roll Gold** (`#c9a84c`): The singular accent. Active navigation state, gold button variant, brand icon background, focus ring on inputs. Used on no more than 10% of any given screen. Its rarity is the point.
- **Commendation Gold** (`#e2c97e`): Lighter gold for button hover states and gradient endpoints. Never used alone as an accent; always as a complement to Honor Roll Gold in transitions.
- **Diploma Cream** (`#f5ecd1`): The palest gold tint. Hover backgrounds on outline buttons, input focus tint. Warms the surface without competing with the structural gold.

### Neutral
- **Exam Paper Ivory** (`#faf8f4`): The main application background. Warm, not clinical. The slightest warmth toward gold chroma prevents it from reading as generic white.
- **Record Page White** (`#fefefe`): Card surfaces, form backgrounds, the topbar. One step whiter than Ivory; creates a clear surface layer hierarchy without shadows.
- **Ink Navy** (`#2c3e50`): Primary body text. Dark enough for WCAG AA contrast on Ivory and White surfaces.
- **Draft Grey** (`#374151`): Secondary body text, table cell content.
- **Faded Annotation** (`#9ca3af`): Muted text: subtitles, placeholder copy, table header labels. Never used for interactive elements.
- **Rule Line** (`#e5e7eb`): Table row dividers, card section borders.
- **Column Line** (`#d1d5db`): Form input borders at rest.
- **Ledger Background** (`#f3f4f6`): Table thead background, muted container fills.

### Status
- **Present Green** (`#27ae60`): Attendance present, payment confirmed, success alerts.
- **Absent Red** (`#e74c3c`): Attendance absent, overdue debt, error states.
- **Tardy Amber** (`#f39c12`): Late attendance, pending status, warning alerts.
- **Reference Blue** (`#2980b9`): Informational alerts, neutral status badges.

### Named Rules
**The Gold Scarcity Rule.** Honor Roll Gold (`#c9a84c`) appears on active navigation items, the brand mark, and the primary gold button variant. It does not appear on decorative elements, section headings, body text emphasis, or any surface not directly actionable or identity-bearing. If you reach for gold and the element is not one of those three cases, stop.

**The Two-Surface Rule.** Content lives on Record Page White (`#fefefe`) cards. The page itself is Exam Paper Ivory (`#faf8f4`). The sidebar is Headmaster's Navy. Three surface levels, no more. Nesting a white card inside another white card collapses the hierarchy.

## 3. Typography

**Display Font:** Playfair Display, 700 weight (with Georgia, serif fallback)
**Body Font:** Inter, 400/500/600/700 weights (with system-ui, -apple-system fallback)

**Character:** Playfair Display brings institutional gravitas to every heading: it reads like a diploma, a certificate, a formal notice. Inter provides the utilitarian precision that dense operational data requires. The pairing works because they never compete: Playfair is reserved for headings only; Inter handles everything else.

### Hierarchy
- **Display** (700, 2.4rem, 1.2 line-height): Login page title and brand surface hero text. Playfair Display at its most commanding. Appears on one element per screen at most.
- **Headline** (700, 1.6rem, 1.3 line-height): Page-level titles (`lt-page-title`). Anchors the current section. One per view.
- **Title** (700, 1.15rem, 1.4 line-height): Topbar section label, card titles. Sub-section anchors. Always Playfair.
- **Body** (400, 0.9375rem / 15px, 1.6 line-height): All prose, form labels at full weight (600), table cell content. Inter. Max line length 65–75ch on reading surfaces.
- **Label** (600, 0.75rem, 1.4 line-height, 0.06em letter-spacing, uppercase): Table column headers, navigation section dividers (`TỔng QUAN`, `HỌC VỤ`), badge text, stat card labels. Inter, all-caps, tracked out. Hierarchy through sparsity, not size.

### Named Rules
**The Playfair Discipline Rule.** Playfair Display is used exclusively for headings (display, headline, title roles). It does not appear in body copy, table cells, labels, badges, or navigation items. Every Playfair occurrence on a screen must correspond to a named hierarchy level. If a heading is not one of those five roles, it should be Inter.

**The Scale Ratio Rule.** Adjacent heading steps must have at least a 1.25 ratio (body 15px → label 12px → title 17.25px → headline 24px → display 36px). A flat scale where all headings are the same size defeats the register metaphor.

## 4. Elevation

This system is flat by default. Surfaces do not accumulate shadows to signal hierarchy; they use the Two-Surface Rule (Ivory page / White card / Navy sidebar) for spatial layering without depth cues.

Shadows appear only as a response to state: resting cards carry the minimum ambient shadow required to lift them off the page; the sticky topbar signals its fixed position with a hairline shadow; buttons lift on hover to confirm they are interactive.

### Shadow Vocabulary
- **Card ambient** (`0 2px 12px rgba(0,0,0,0.07)`): Applied to all `.lt-card` and `.lt-stat` elements at rest. Barely perceptible. Separates the white card surface from the ivory page background. Does not imply elevation.
- **Topbar position** (`0 1px 4px rgba(0,0,0,0.08)`): Applied to the sticky topbar. Signals that the bar is in a fixed layer above scrolling content. Not decorative.
- **Button lift** (`0 4px 14px rgba(15,28,46,0.35)`): Applied to primary buttons on hover. Confirms interactivity. Disappears on active/press.
- **Gold button lift** (`0 4px 14px rgba(201,168,76,0.45)`): Gold variant of button lift on hover. Same role; tinted toward the gold hue.
- **Brand icon glow** (`0 8px 30px rgba(201,168,76,0.35)`): Reserved for the logo icon in the login panel hero. Ceremonial; does not transfer to other elements.

### Named Rules
**The Flat-By-Default Rule.** Shadows appear only in response to state (hover, sticky position, modal overlay). Decorative box-shadows on decorative elements are prohibited. If an element is not interactive and not positionally fixed, it does not have a shadow — or its shadow is the Card ambient at minimum intensity.

## 5. Components

### Buttons

Measured and assured. Buttons lift slightly on hover; the transition is 200ms with the standard ease curve (`cubic-bezier(.4,0,.2,1)`). They respond without theatrics.

- **Shape:** Gently rounded corners (8px radius). Not pill, not sharp. Institutional, not casual.
- **Primary (Navy):** Headmaster's Navy background with a subtle gradient to Council Room Navy. Record Page White text. `padding: 9px 20px`. Box shadow lifts on hover from `0 2px 8px rgba(15,28,46,0.25)` to `0 4px 14px rgba(15,28,46,0.35)`.
- **Gold:** Honor Roll Gold to Commendation Gold gradient. Headmaster's Navy text (high contrast). Used for the most prominent call-to-action on a given screen, never alongside a primary navy button of equal weight. `padding: 9px 20px`.
- **Outline:** Transparent background, 1.5px Column Line border, Draft Grey text. On hover: Headmaster's Navy border, Ink Navy text, Diploma Cream background. Reserved for secondary actions.
- **Danger:** Absent Red fill. Reserve for destructive confirmation only (delete, revoke). Not for warnings.
- **Size modifiers:** Small (`5px 12px`, 0.8rem) for table row actions; Large (`12px 28px`, 1rem) for page-level primary actions; Icon (`8px`, equal padding) for topbar icon buttons.

### Cards / Containers

- **Corner Style:** Gently curved (12px radius, `--card-radius`).
- **Background:** Record Page White (`#fefefe`).
- **Shadow Strategy:** Card ambient shadow at rest (`0 2px 12px rgba(0,0,0,0.07)`). No shadow change on hover unless the card is itself interactive (e.g., stat cards lift 2px on hover with increased shadow).
- **Border:** None by default. Section dividers within cards use a 1px Rule Line (`#f0f0f0`), not full borders.
- **Internal Padding:** Card body 24px all sides. Card header 18px vertical, 24px horizontal. Card footer 14px vertical, 24px horizontal with Ledger Background (`#fafafa`).
- **Header:** Card title in Title typography (Playfair 700, 1rem). Right-side slot for actions.

### Stat Cards

A distinct layout from standard cards: icon block (52px square, 12px radius, tinted icon background) + value (1.65rem, 800 weight, Headmaster's Navy) + label (Label typography, Faded Annotation). Used on dashboard overviews only. Interactive: lifts 2px on hover. Does not use the hero-metric pattern (big number + gradient accent): the icon carries visual identity, not color fills.

### Inputs / Fields

- **Style:** 1.5px Column Line border at rest. Record Page White background. 8px radius. Body typography (Inter 0.9rem).
- **Focus:** Border shifts to Honor Roll Gold. Gold focus ring: `0 0 0 3px rgba(201,168,76,0.15)`. No background color change.
- **Invalid:** Border shifts to Absent Red. No focus ring.
- **Disabled:** 60% opacity, pointer-events none.
- **Labels:** Label typography, Draft Grey, 6px below the label.

### Navigation (Sidebar)

The sidebar is the most distinctive component. It operates as an inverted surface: Headmaster's Navy background, white-tinted text, gold active state.

- **Section labels:** 0.65rem, 600 weight, 0.12em tracked, uppercase, `rgba(255,255,255,0.35)`. Structural dividers, not interactive.
- **Nav items:** 0.875rem, 500 weight, `rgba(255,255,255,0.75)` at rest. On hover: white text, `rgba(255,255,255,0.06)` background.
- **Active state:** Honor Roll Gold text, `rgba(201,168,76,0.12)` background, 3px Honor Roll Gold left-edge indicator. The 3px indicator is structural (it marks the active item's position in the nav column) and is the one permitted side-stripe in this system.
- **User footer:** Avatar (34px circle, gold gradient), name (0.82rem, 600 weight, white), role code (0.68rem, uppercase, gold).

### Tables

- **Headers:** Ledger Background (`#f5f5f3`-toned ivory). Label typography. 1px Rule Line bottom border. Left-aligned. Non-wrapping.
- **Rows:** 1px `#f3f4f6` bottom divider. `#fafaf8` on row hover (barely perceptible warmth, not a full highlight). No alternating rows.
- **Cells:** Body typography, Draft Grey. 13px vertical, 16px horizontal padding. Vertical-align middle.
- **Last row:** No bottom border.

### Badges

Soft-tinted pills, not solid fills. The background is a low-opacity tint of the status color; the text is a fully saturated darker tone of the same hue. 20px border-radius. 0.72rem, 600 weight. 3px 10px padding.

- Success: `rgba(39,174,96,0.1)` bg, `#16a34a` text.
- Danger: `rgba(231,76,60,0.1)` bg, `#dc2626` text.
- Warning: `rgba(243,156,18,0.12)` bg, `#d97706` text.
- Navy: `rgba(15,28,46,0.08)` bg, Headmaster's Navy text.
- Gold: `rgba(201,168,76,0.15)` bg, `#92620a` text (darkened gold for AA contrast).
- Info: `rgba(41,128,185,0.1)` bg, `#1d4ed8` text.
- Gray: Ledger Background bg, Faded Annotation text.

### QR Attendance Box (Signature Component)

The QR attendance view is a high-stakes moment in the teacher's daily workflow: a 10-minute countdown, a large QR code, a timer that turns red and pulses when expiring. The box is white on white with a thick Headmaster's Navy border (3px, 16px radius) to frame the code as an official printed artifact. The countdown uses tabular-nums for stable layout. When the timer hits ~30 seconds, the color shifts to Absent Red with a 1s opacity pulse animation — the one permitted use of animation as an urgent signal.

## 6. Do's and Don'ts

### Do:
- **Do** use Playfair Display for all heading levels and nothing else. If it's not a page title, section title, or card title, it's Inter.
- **Do** keep Honor Roll Gold to active navigation states, the brand mark, and the gold button variant. Every other use requires a conscious decision.
- **Do** use the Two-Surface Rule: Ivory page, White card, Navy sidebar. These three surfaces create all spatial hierarchy.
- **Do** use status badges (soft-tinted pills) for all categorical labels: attendance status, payment status, role codes, grade descriptors.
- **Do** size buttons to context: Small for table row actions, default for card-level actions, Large for the single primary CTA on a page.
- **Do** apply WCAG AA contrast to all text: 4.5:1 for body text, 3:1 for large text and UI components. Faded Annotation (`#9ca3af`) on Ivory fails AA — use it only for supplementary text that is never the primary information.
- **Do** pair color-coded status indicators with a text label or icon. Never communicate attendance status, debt status, or grade result through color alone.
- **Do** write Vietnamese copy as formal institutional prose. Role names, section headers, and alert messages should read as official communications, not casual notifications.
- **Do** respect `prefers-reduced-motion`: skip the `fadeUp` entrance animation and the QR timer pulse for users who have opted out.

### Don't:
- **Don't** build a generic SaaS bootstrapped dashboard: white background, blue primary buttons, flat icons with zero personality. Lumina Tutors has a visual identity; new screens inherit it.
- **Don't** use playful edtech aesthetics: bright primary colors, gamification badges, illustrated onboarding characters, Duolingo-style UI elements. This tool is for school staff, not students in a learning app.
- **Don't** produce enterprise-grey SharePoint energy: dense grey forms, no typographic hierarchy, no brand presence in the layout.
- **Don't** use neon dark fintech aesthetics: dark backgrounds with cyan, green, or purple neon accents belong to a trading platform, not a Vietnamese school management system.
- **Don't** use gradient text (`background-clip: text` with a gradient fill). Emphasis is achieved through weight, scale, or color choice. Never through a gradient.
- **Don't** use side-stripe borders greater than 1px as decorative accents on cards, callouts, or list items. The 3px gold indicator on the active nav item is the sole exception because it serves a structural role (position marker in a vertical column). Alerts using `border-left: 4px` are legacy; migrate them to full-border or tinted-background treatments on new surfaces.
- **Don't** place a white card inside another white card. Nested cards collapse the Two-Surface Rule.
- **Don't** use Honor Roll Gold on body text, decorative dividers, section headings, or any element that is not one of: active nav item, brand mark, gold button variant.
- **Don't** animate CSS layout properties (width, height, padding, top, left). Transition only opacity, transform, box-shadow, color, background-color.
- **Don't** use glassmorphism (backdrop-filter blur) decoratively. If a blur is needed (modal scrim, dropdown), it serves a structural function; it is not a style.
- **Don't** use the hero-metric template: big number centered on a gradient card with a decorative accent color. Stat cards in this system pair a tinted icon block with value and label — the icon carries identity, not a gradient fill.
