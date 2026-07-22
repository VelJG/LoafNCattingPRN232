# Landing, authentication, and role-based UI design

## Goal

Implement the approved Loaf'N Catting v2 visual direction in the existing React frontend, connect the frontend to the backend authentication API, and route authenticated users to the correct experience:

- `Customer` users enter the customer menu at `/menu`.
- `Staff` and `Admin` users enter the operations dashboard at `/admin`.
- Unauthenticated visitors start at the public landing page at `/` and can open login or customer registration.

The work covers the public landing page, authentication flow, customer menu landing screen, and staff/admin dashboard. Existing cat and reservation pages remain available, but recreating every screen listed in the design ZIP is outside this change.

## Source of truth

The implementation uses these references in order:

1. `Replicate all screens.zip/design_handoff_loaf_n_catting_v2/README.md` for the v2 handoff, screen behavior, and exact design tokens.
2. The two `.dc.html` prototypes for landing and authenticated application structure.
3. `frontend/design/VISUAL_DESIGN.md` for existing project conventions and accessibility requirements.
4. Existing React architecture and backend contracts, so the redesign does not break current cart, repository, reservation, or dashboard behavior.

Where the documents differ, the ZIP's v2 warm-editorial direction wins for the screens in this scope.

## Backend contract

The frontend connects to the existing ASP.NET API at `VITE_API_BASE_URL`, defaulting in local development to `http://localhost:5053`.

### Register

`POST /api/auth/register`

Request:

```ts
interface RegisterRequest {
  name: string
  email: string
  password: string
  phoneNumber: string
  address: string | null
}
```

The backend creates only a `Customer` account and returns `201` with a `User` object. Registration does not return a token, so successful registration returns the user to login with a clear success message and the email prefilled.

### Login

`POST /api/auth/login`

Request:

```ts
interface LoginRequest {
  email: string
  password: string
}
```

Response:

```ts
interface LoginResponse {
  accessToken: string
  tokenType: 'Bearer'
  expiresAtUtc: string
  user: User
}
```

The response's semantic `user.role` string is the only routing authority. The frontend never uses numeric role IDs or user-entered role values.

### Session verification and logout

- `GET /api/auth/verify` with `Authorization: Bearer <token>` restores a stored session and returns the current `User` plus expiration time.
- `POST /api/auth/logout` is called during logout. The frontend clears local session state even if the request cannot reach the server because the current backend has no refresh-token or server-side token-revocation mechanism.
- A `401` during verification or an authenticated request clears the stored session and sends the user to login.
- A `403` means the token is valid but the role cannot perform the action; the UI shows an authorization message without pretending the session expired.

Backend `ProblemDetails` responses are normalized from `status`, `title`, and `detail` into one typed frontend error object.

## Frontend architecture

The existing React 19, TypeScript, Vite, React Router, and `react-icons/md` stack stays in place. No framework migration is required.

The frontend separates transport, authentication logic, reusable UI, layouts, and page composition:

```text
src/
  api/
    httpClient.ts              shared JSON request/error handling
  components/
    brand/BrandWordmark.tsx    reusable text wordmark for v2 screens
    ui/Button.tsx              shared primary/secondary/text actions
    ui/Field.tsx               labeled input and validation rendering
    ui/StatusChip.tsx          semantic status presentation
    ui/LoadingScreen.tsx       session/bootstrap loading state
  features/
    auth/
      authApi.ts               login/register/verify/logout requests
      authModels.ts            API and session types
      authRouting.ts           role-to-home and authorization rules
      AuthProvider.tsx         session lifecycle and actions
      useAuth.ts               feature hook
      ProtectedRoute.tsx       authentication and role guard
  layouts/
    PublicLayout.tsx
    CustomerLayout.tsx
    AdminLayout.tsx
  pages/
    public/LandingPage.tsx
    auth/LoginPage.tsx
    auth/RegisterPage.tsx
    customer/MenuPage.tsx
    admin/AdminDashboardPage.tsx
```

Existing components and pages are reused where their responsibilities still fit. Large visual blocks specific to the landing page may live beside that page; truly reusable controls stay in `components/ui`.

## Authentication state and routing

`AuthProvider` owns a small state machine:

```text
initializing -> authenticated
initializing -> unauthenticated
unauthenticated -> authenticating -> authenticated
authenticated -> unauthenticated
```

On application startup, the provider reads the stored token and expiry. If a token exists, it verifies it with the backend before exposing protected routes. The loading screen prevents a flash of the wrong layout while verification runs.

Role mapping is case-insensitive at the boundary and returns one of three known frontend roles: `Customer`, `Staff`, or `Admin`. Unknown roles are treated as unauthorized and never receive an admin route.

Route behavior:

| Route | Access | Behavior |
| --- | --- | --- |
| `/` | Public | Marketing landing page; authenticated users may still view it |
| `/login` | Public-only | Authenticated users are redirected to their role home |
| `/register` | Public-only | Creates Customer accounts only |
| `/menu`, `/cats`, `/reservations` | Customer | Unauthenticated users go to login; Staff/Admin go to `/admin` |
| `/admin` | Staff, Admin | Unauthenticated users go to login; Customer goes to `/menu` |

After successful login, the frontend immediately routes from the response role without decoding or trusting the JWT payload. A preserved `redirect` destination is honored only when it is valid for that user's role; otherwise the role home wins.

## Visual design

### Public landing page

The landing page follows the ZIP's red-brick editorial palette:

- Background `#F3ECDD`, ink `#201812`, primary brick `#C6422A`.
- Playfair Display for display headings, Space Grotesk for body/wordmark, and Space Mono for labels and actions, with system fallbacks when remote fonts are unavailable.
- Sticky 70px header, large editorial hero, restrained hairline borders, square content frames, and fully rounded pill buttons.
- Main sections: hero, moving cafe highlights, menu preview, cats preview, about, reservation CTA, contact/footer.
- Existing logo/hero and realistic cafe/cat/product assets are reused where they improve the HTML prototype's placeholders.
- Primary header actions are `ĐĂNG NHẬP` and `ĐẶT BÀN`; registration is available from the authentication flow.

### Login and registration

Authentication pages bridge the landing palette and application palette:

- A two-panel desktop layout with editorial brand/story content and a focused white form panel.
- One-column mobile layout with the form first and no decorative content that pushes actions below the fold.
- Visible labels, inline validation, password visibility control, pending button state, API error summary, and keyboard focus treatment.
- Registration fields match the backend contract exactly: name, email, phone number, password, and optional address.

### Customer menu

The existing menu behavior remains intact while adopting the v2 authenticated application style:

- Cream `#F6EEE4` background, brown `#5F2B15` ink/borders, orange `#FF6B35` accent.
- Sticky customer header with text wordmark, mono navigation, cart count, user action, and logout.
- Dark-brown greeting hero with serif heading and orange reservation CTA.
- Pill search and category filters.
- Responsive three-column flat product cards with square borders, product imagery, availability text, mono pricing, and reusable add action.
- Existing loading, error, empty, add-to-cart, and cart drawer behavior remains functional.

### Staff and admin dashboard

The existing dashboard data flow remains intact while matching the v2 admin reference:

- Fixed dark-brown desktop sidebar with orange active item and a responsive compact navigation treatment on narrow screens.
- Top bar displays the signed-in user's real name and role instead of hardcoded profile data.
- Four flat statistic cards, recent-orders table, today's reservation summary, and low-stock summary using existing mock repository data until their backend APIs are implemented.
- Staff and Admin share this dashboard route. Backend authorization remains the final authority for future admin-only actions.

## Reusable component rules

- Page components compose reusable controls and do not issue raw `fetch` calls.
- Authentication pages call only `AuthProvider` actions; `AuthProvider` calls `authApi`; `authApi` calls `httpClient`.
- Button, field, loading, and status components expose behavioral props and variants rather than accepting arbitrary page-specific class strings.
- Layouts own navigation and account/logout controls. Pages own screen content.
- CSS uses shared tokens and focused section classes. No inline styles are introduced.
- Existing cart state and catalog repository interfaces remain unchanged unless a tested adapter is required.

## Responsive and accessibility requirements

- No horizontal page scroll at 390px.
- Desktop content width is capped at approximately 1180–1240px.
- Header navigation collapses without hiding login, cart, or logout access.
- Buttons and icon controls meet a 44px minimum target size.
- Form labels remain visible; validation is associated with its input.
- All interactive controls have keyboard focus states and meaningful accessible names.
- Status is communicated with text as well as color.
- Semantic headings, navigation, main regions, tables, and buttons are preserved.
- Motion respects `prefers-reduced-motion`.

## Error, loading, and empty states

- Session verification uses a branded full-page loading state.
- Login and registration show field validation before submission and normalized backend errors beside the form.
- Form submission disables repeat submission while preserving entered values.
- Menu and dashboard keep stable skeleton geometry while data loads.
- Data errors provide a retry action.
- Empty menu and dashboard lists provide a useful next step rather than a blank surface.

## Testing strategy

The frontend gains Vitest, React Testing Library, and jsdom configuration because it currently has no test runner.

Tests cover:

- HTTP success, `ProblemDetails`, unauthorized, and network-error normalization.
- Role normalization and role-home selection.
- Login sends the expected payload and routes Customer versus Staff/Admin correctly.
- Registration sends the exact backend contract and returns to login after success.
- Session verification restores valid sessions and clears invalid sessions.
- Protected routes prevent cross-role access.
- Landing, login, menu, and admin primary landmarks render accessibly.
- Existing cart/menu behavior continues to pass its focused tests.

Completion requires a clean `npm test` run, TypeScript/Vite production build, and responsive visual checks at desktop and 390px widths.

## Non-goals

- Recreating every customer and admin screen listed in the ZIP.
- Adding OAuth, refresh tokens, password reset, or email verification flows that the backend does not currently expose.
- Changing backend authentication contracts or role authorization.
- Replacing the existing catalog/cart repository with unimplemented backend endpoints.
- Adding product options, promotions, reviews, or other domain features absent from the backend and approved scope.
