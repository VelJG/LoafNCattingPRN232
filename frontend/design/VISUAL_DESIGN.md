# Loaf'NCatting Web Visual Design

This document is the visual source of truth for the PRN232 React frontend.
Every customer and admin screen should follow it unless the team explicitly
agrees to revise the design direction.

## 1. Design Direction

The web experience adapts the Flutter application's approved **Orange Meow
UI** direction. It should feel:

- Warm, clean, and easy to use before it feels playful.
- Friendly and recognizably cat-cafe themed without becoming childish.
- Consistent between the Customer Portal and Admin/Staff Dashboard.
- More spacious and information-dense than the mobile app where the larger
  viewport makes that useful.

Cat motifs belong in brand moments, Cat Gallery, selected navigation,
friendly empty states, and small status details. They should not decorate
every card, form, or administrative table.

## 2. Brand Assets

Primary logo:

```text
public/loafncatting-logo.png
```

Rules:

- Use the real transparent logo asset. Do not replace it with emoji, text
  symbols, CSS drawings, or an approximate cat icon.
- Keep the original aspect ratio and use `object-fit: contain`.
- Place the logo primarily on white, cream, or other quiet backgrounds.
- Do not stretch, recolor, rotate, or add a heavy shadow to the logo.
- Use icons from the selected icon library for interface controls. Do not use
  emoji as UI icons.

## 3. Color Tokens

The following tokens come from the Flutter theme and should be exposed as CSS
custom properties.

| Token | Value | Usage |
| --- | --- | --- |
| `--color-primary` | `#FF6B35` | Primary actions, selected states, price emphasis |
| `--color-primary-soft` | `#FF8C5A` | Hero gradients and secondary orange accents |
| `--color-primary-deep` | `#D2691E` | Warm shadows and dark orange text accents |
| `--color-peach` | `#F4A460` | Small decorative and tertiary accents |
| `--color-cream` | `#FFF4E6` | Main application background |
| `--color-cream-light` | `#FFF8DC` | Selected navigation and pale callouts |
| `--color-surface` | `#FFFFFF` | Cards, forms, tables, drawers |
| `--color-text` | `#5F2B15` | Main headings and body text |
| `--color-text-muted` | `#8A6B5B` | Supporting copy and metadata |
| `--color-border` | `#F1D7C5` | Card, input, and divider borders |
| `--color-success` | `#2E7D4F` | Available, completed, healthy states |
| `--color-error` | `#C2412D` | Errors, unavailable, destructive actions |

Additional blue or rose colors may be used for operational status chips, but
orange remains the only brand-primary color.

### Color balance

- Use white and cream for most of the viewport.
- Orange should guide attention, not flood every surface.
- Brown is the default text color; pure black should be avoided.
- Customer heroes may use the soft-orange-to-primary-orange gradient.
- Admin pages should use gradients sparingly and prioritize white surfaces.

## 4. Typography

Use a clean sans-serif stack suitable for Vietnamese and English:

```css
font-family: Inter, ui-sans-serif, system-ui, -apple-system,
  BlinkMacSystemFont, "Segoe UI", sans-serif;
```

Recommended hierarchy:

| Role | Size | Weight | Line height |
| --- | --- | --- | --- |
| Display / customer hero | `clamp(2.5rem, 5vw, 4.75rem)` | 800-900 | 0.98-1.05 |
| Page title | `clamp(2rem, 3vw, 3rem)` | 800 | 1.05-1.15 |
| Section title | `1.75-2.25rem` | 800 | 1.15 |
| Card title | `1.05-1.25rem` | 700-800 | 1.25 |
| Body | `0.95-1rem` | 400-500 | 1.55-1.7 |
| Label / eyebrow | `0.75-0.82rem` | 700-800 | 1.2 |

Avoid decorative serif fonts for interface copy. The serif lettering inside
the logo remains part of the image asset only.

## 5. Spacing and Layout

Use an 8-pixel spacing rhythm with practical intermediate values.

```text
4, 8, 12, 16, 20, 24, 32, 40, 48, 64, 80
```

Web layout rules:

- Customer content width: approximately `1180-1240px`.
- Standard page gutters: `24px` desktop, `16px` mobile.
- Major page sections: `64-88px` vertical spacing on desktop.
- Cards should normally use `16-24px` internal padding.
- Keep compact metadata close to its parent, but give different sections
  clear breathing room.
- Avoid long full-width text lines. Body copy should generally remain below
  `680px`.

## 6. Shape and Elevation

Recommended radii:

| Component | Radius |
| --- | --- |
| Inputs and primary buttons | `16px` |
| Standard cards | `18px` |
| Image frames | `16-20px` |
| Hero blocks | `22-28px` |
| Chips and status pills | `999px` |

Use warm, low-opacity shadows rather than neutral black shadows:

```css
box-shadow: 0 12px 32px rgba(210, 105, 30, 0.10);
```

Cards should still have a subtle border so they remain legible without the
shadow. Avoid stacking multiple heavy shadows.

## 7. Core Components

### Buttons

- Primary: orange background, white text, minimum height `48px`.
- Secondary: white or pale cream background, orange/brown text, orange or
  warm border.
- Disabled: muted cream border/background with muted text.
- Every icon-only button requires an accessible label and a visible hover and
  keyboard-focus state.

### Inputs

- White surface, `16px` radius, warm border, comfortable vertical padding.
- Orange focus border/ring.
- Labels remain visible; placeholders are supplementary only.
- Validation belongs close to the relevant field.

### Cards

- White surface, warm border, restrained warm shadow.
- Product cards prioritize real imagery, product name, price, availability,
  and one clear Add action.
- Cat cards may be more expressive with larger imagery and personality chips.
- Admin cards should be compact, scannable, and data-oriented.

### Chips and statuses

- Filter chips use a white default state and orange selected state.
- Status colors must have a text label; color alone is not enough.
- Do not hardcode backend status IDs. Bind visual states to semantic names
  returned by the API or a frontend mapping layer.

### Empty, loading, error, and success states

Every data-driven screen must define all four states.

- Loading: skeleton or compact spinner without shifting the page layout.
- Empty: friendly, concise message with one useful next action.
- Error: plain explanation and retry action where possible.
- Success: clear confirmation near the action that caused it.

Cat/paw imagery may appear in friendly empty states, but not in serious
errors or destructive confirmation dialogs.

## 8. Customer Portal Layout

Desktop:

- Sticky horizontal header with logo, Menu, Meet the Cats, Book a Table,
  account, and cart.
- Menu uses a responsive product grid with search and category filters.
- Cart opens as a right-side drawer for quick review.
- Checkout uses two columns: form/content on the left and sticky order summary
  on the right.
- Reservation uses a two-column flow: visit details and matching tables.
- Cat Gallery uses three or four columns depending on available width.

Mobile:

- Collapse navigation without hiding the primary cart action.
- Use one-column content and full-width primary actions.
- Avoid horizontal scrolling except for deliberate filter-chip rows.
- Keep touch targets at least `44px` high and wide.

## 9. Admin/Staff Layout

- Use a left sidebar on desktop instead of copying the mobile seven-item
  bottom navigation.
- Keep a compact top bar for shift/date context and the signed-in user.
- Dashboard metrics use a responsive grid.
- Orders, reservations, users, products, and tables should use data tables on
  desktop and stacked cards on small screens.
- Customer and Admin areas share tokens and base components, but Admin uses
  fewer gradients and playful motifs.

## 10. Responsive Breakpoints

Use content-driven breakpoints rather than device names. Starting values:

```css
@media (max-width: 1024px) { /* compact desktop / tablet */ }
@media (max-width: 760px)  { /* mobile navigation and one-column forms */ }
@media (max-width: 520px)  { /* narrow mobile refinements */ }
```

Required checks:

- No horizontal page scroll at `390px`.
- Primary actions remain visible and reachable.
- Card text and prices do not clip.
- Admin tables either scroll inside their own container or switch to cards.

## 11. Accessibility

- Body text and controls must meet WCAG AA contrast where practical.
- Do not place white text on pale orange without verifying contrast.
- Preserve visible keyboard focus indicators.
- Use semantic headings in order and semantic buttons/links for actions.
- Images require meaningful `alt` text, except decorative images which use an
  empty `alt`.
- Do not communicate availability, payment, or order status with color only.
- Respect `prefers-reduced-motion` for non-essential transitions.

## 12. Images and Icons

- Use real product, cafe, and cat images with consistent warm photography.
- Prefer source assets or properly licensed imagery; keep attribution notes
  where required.
- Use `object-fit: cover` for content photography and `contain` for the logo.
- Avoid generic gray placeholder boxes in final UI.
- The current React icon source is `react-icons/md`, matching the Material icon
  language used by the Flutter app.

## 13. Frontend Architecture Rules

- Keep domain types in `src/types`.
- Keep temporary realistic data in `src/data`.
- Access data through repository/service interfaces in `src/services`; pages
  should not import mock data directly when that data will later come from an
  API.
- Keep shared state such as cart behavior in `src/state`.
- Keep reusable visual components in `src/components`.
- Keep customer and admin layouts separate in `src/layouts`.
- Pages should compose shared components rather than duplicate visual rules.
- When the backend becomes available, replace the mock repository
  implementation without redesigning the screens.

## 14. Do and Do Not

Do:

- Reuse the exact Orange Meow tokens.
- Use realistic mock data until API contracts exist.
- Keep interfaces concise and action-focused.
- Verify desktop and mobile states after every meaningful design batch.

Do not:

- Copy the Flutter mobile layout one-to-one onto desktop.
- Introduce a second unrelated color palette.
- Replace the logo or interface icons with emoji.
- Add size, topping, review, promotion, or other product features unless the
  backend/team scope confirms them.
- Hardcode role or status numeric IDs.
- Ship a data page without loading, empty, error, and success behavior.
