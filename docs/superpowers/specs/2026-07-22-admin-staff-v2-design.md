# Admin and Staff Web App v2 Design

## Goal

Recreate the admin experience from `Loaf n Catting Web App v2.dc.html` with high visual fidelity inside the existing React frontend. The implementation covers Dashboard, Orders, Reservations, Catalog, Cats, Tables, Users, and Store Location. It uses live backend APIs wherever they already exist and clearly disables unsupported mutations instead of simulating successful server work.

## Reference and visual rules

The source of truth is:

`C:\FPTUniversity\PRN232\Nya\Replicate all screens\Replicate all screens\design_handoff_loaf_n_catting_v2\Loaf n Catting Web App v2.dc.html`

The implementation follows the handoff tokens and composition:

- `#F6EEE4` cream application background.
- `#5F2B15` ink, borders, and sidebar background.
- `#FF6B35` active navigation, CTA, icons, and prices.
- Playfair Display for page and card headings, Space Grotesk for body text, and Space Mono for labels, statuses, and buttons.
- Flat white panels with square corners and 1px ink borders.
- Pill-shaped controls, filters, status chips, inputs, and avatars.
- A fixed 256px dark sidebar and sticky 76px top bar on desktop.
- Responsive layouts retain all functionality on smaller screens.

The sidebar's `Trang khách hàng` action is removed. `Đăng xuất` remains.

## Roles and routes

Admin and Staff share the same layout and reusable page components.

| Route | Screen | Admin | Staff |
| --- | --- | --- | --- |
| `/admin` | Dashboard | Yes | Yes |
| `/admin/orders` | Orders | Yes | Yes |
| `/admin/reservations` | Reservations | Yes | Yes |
| `/admin/catalog` | Catalog | Yes | Yes |
| `/admin/cats` | Cats | Yes | Yes |
| `/admin/tables` | Tables | Yes | Yes |
| `/admin/users` | Users | Yes | No |
| `/admin/store` | Store Location | Yes | No |

Admin-only pages are omitted from Staff navigation and protected at the route level. A Staff user who enters an admin-only URL is redirected to `/admin`.

## Frontend architecture

### Layout

`AdminLayout` owns the shared sidebar, route-aware active state, page title/subtitle metadata, top-bar search presentation, notification button, authenticated profile, and logout action. Navigation entries are filtered by the authenticated role.

### Reusable UI

Admin-specific reusable components cover:

- stat cards and trend labels;
- filter pills and status chips;
- page toolbars and primary actions;
- flat data panels and responsive tables;
- entity cards for orders, reservations, cats, and tables;
- loading skeletons, empty states, API errors, and retry controls;
- confirmation dialogs and toast feedback;
- pill form fields and disabled-action explanations.

Each route remains a focused page component. Data access and DTO mapping stay outside the page markup in an admin service/repository layer.

## Screens

### Dashboard

Match the reference four-card metric row, recent-orders panel, today's-reservations panel, and low-stock panel. Metrics are derived from live orders, store reservations, and admin products. Independent API failures remain visible in the relevant panel without blanking the whole dashboard.

### Orders

Render reference-style two-column order cards with status filters, total, customer, timestamp, order/payment status chips, and a status-transition action. Use the live orders endpoints. Status mutations update the affected card and dashboard data after success.

### Reservations

Render two-column reservation cards with filters, date/time, customer, guest count, assigned table, status, and the appropriate next transition. Use the store-reservation list and transition endpoints for confirm, check-in, complete, and cancel.

### Catalog

Render the flat product table from the reference with image/placeholder, availability, category, price, stock, and edit/delete controls. Use `/api/admin/products` for list, create, update, and delete. Create and edit use a shared validated form dialog.

### Cats

Render the reference three-column management cards using live `/api/cats` data. Because the backend exposes read-only cat endpoints, add/edit/delete controls are visibly disabled with an explanation rather than performing mock mutations.

### Tables

Render the reference four-column table cards with table name, capacity/area, icon, and status. The current backend exposes availability through the reservation flow but no management CRUD API. The management actions are therefore read-only and clearly marked unavailable.

### Users

This Admin-only screen matches the searchable reference table and `Tạo nhân viên` action. The existing backend only supports staff creation, so the creation form uses `/api/admin/users/staff`. A user list or role/lock action is not fabricated; unavailable actions are disabled and explained.

### Store location

This Admin-only screen matches the single bordered reference form with pill fields. Since no backend store-settings endpoint exists, the fields are presented read-only and saving is disabled with an explanation.

## API and state behavior

- Authenticated requests use the existing session token and HTTP client conventions.
- Order status requests also supply the role header currently required by `OrdersController`.
- Query filters are client-side where the backend does not accept an equivalent filter.
- Mutations show pending state, prevent duplicate submissions, surface backend validation messages, show a success toast, and refresh or update the relevant cached data.
- A `401` follows the existing authentication-expiry behavior. A `403` becomes a permission message or role-safe redirect.
- Unsupported operations never display a false success state.

## Verification

- Route tests verify all eight Admin routes and Staff restrictions.
- Layout tests verify active navigation, role filtering, removed customer-page link, and logout.
- Repository tests verify endpoint paths, authorization headers, DTO mapping, and mutation payloads.
- Page tests cover loading, success, empty, error/retry, filters, and supported mutations.
- The full test suite, TypeScript production build, and `git diff --check` must pass.
- Each desktop page is visually compared with the interactive `.dc.html` reference at the same viewport; responsive states are checked separately.

## Out of scope

- Adding new backend controllers or database migrations.
- Simulated successful CRUD for backend capabilities that do not exist.
- The removed `Trang khách hàng` sidebar switch.
- Customer-facing UI changes unrelated to shared design tokens.
