# Customer Experience V2 Design

## Goal

Rebuild the authenticated customer experience from `Replicate all screens/design_handoff_loaf_n_catting_v2/Loaf n Catting Web App v2.dc.html`. The reference HTML and its `README.md` are the visual source of truth. The existing menu implementation remains, while the shared header and the reservation, cats, cat detail, location, notifications, chat, and profile screens are replaced with faithful React implementations. The customer header must omit only the `QUẢN TRỊ` control.

## Visual Contract

- Use the reference tokens: `#F6EEE4` background, `#FFFFFF` surfaces, `#5F2B15` ink and borders, `#FF6B35` accent, `#FF8C5A` accent light, `#FFC18A` dark-surface eyebrow, `#FFE3C7` placeholders, `#8A6B5B` muted text, and `#6B4632` body copy.
- Use Playfair Display 500 for display text, Space Grotesk for body and the wordmark, and Space Mono for uppercase navigation, labels, and buttons.
- Preserve the reference geometry: square cards with one-pixel brown borders, pill controls with `999px` radius, a `1180px` customer content width, and a sticky `70px` header.
- The header order is wordmark; `THỰC ĐƠN`, `ĐẶT BÀN`, `MÈO`, `VỊ TRÍ`, `THÔNG BÁO`, `TRÒ CHUYỆN`; flexible spacer; cart; customer avatar. Active navigation is orange and bold. There is no admin pill or separate logout icon.
- Responsive behavior may reflow navigation and grids, but desktop layout, typography, spacing, color, and styling must match the reference.

## Routes and Screens

- `/menu`: retain the accepted menu V2 screen and cart behavior.
- `/reservations`: dark reservation hero, date/time/guest pill fields, four-column table choices, selected orange treatment, and confirmation CTA.
- `/cats`: reference heading and pill search with a four-column square cat grid.
- `/cats/:catId`: reference back link, two-column image/info layout, status, description, and three rating metrics.
- `/notifications`: centered single-column notification list with circular icons and hairline dividers.
- `/location`: reference map illustration, dark cafe panel, contact rows, and a directions CTA opening Google Maps.
- `/chat`: reference full-height message surface, left/right message bubbles, pill composer, and local simulated reply behavior.
- `/profile`: supporting reference profile screen reached from the header avatar; it displays authenticated user information and owns the logout action.

## Component and Data Boundaries

`CustomerLayout` owns only the customer shell, navigation, cart trigger, avatar link, and responsive menu state. Shared customer primitives cover the page frame, feedback states, image fallback, pills, and icon treatments. Each screen remains a focused page component. API request and DTO mapping logic lives in feature repositories rather than page components.

Real backend APIs are used wherever available:

- Categories and products: existing `/api/categories` and `/api/products` integration.
- Cats: `/api/cats`, mapped to card and detail models.
- Reservation availability and creation: `GET /api/reservations/availability` and authenticated `POST /api/reservations`.
- Notifications: authenticated `GET /api/notifications`, `PATCH /api/notifications/{id}/read`, and `PATCH /api/notifications/read-all`.
- Authentication/profile: the current auth session supplies the customer name, email, phone, and token; the existing logout flow remains unchanged internally.

The backend currently exposes no location or conversation controller. Location therefore uses the exact prototype cafe information, and chat uses isolated client-side state with the prototype's simulated reply. These boundaries allow future backend adapters to replace local data without changing page markup or styling.

## Interaction and Error Handling

Reservation fields drive availability requests, the suggested table drives the selected state, and confirmation posts the required guest data. Invalid input and API failures appear inline without destroying the form state. Cats and notifications have reference-aligned loading, empty, and retry states. Notification rows can be marked read, with a mark-all action when unread items exist. Missing or failed images fall back to the reference icon placeholder. Auth failures continue through the existing protected-route/session behavior.

## Verification

- Component tests cover the exact navigation labels and absence of admin/logout header controls, route availability, cat search/detail navigation, reservation availability/creation payloads, notification read actions, location CTA, chat simulation, and profile logout.
- Repository tests cover DTO mapping and authenticated request construction.
- Run the complete frontend test suite, production build, and `git diff --check`.
- Perform browser visual QA at desktop and mobile widths against the reference HTML, including header geometry, typography, colors, screen layout, loading/error states, and API-backed interactions.

## Out of Scope

No backend controller, database schema, admin UI, or staff routing changes are included. Real-time chat, persisted message history, and a store/location management API remain future backend work.
