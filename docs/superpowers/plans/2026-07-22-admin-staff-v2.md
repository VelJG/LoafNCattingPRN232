# Admin and Staff Web App v2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild all Admin and Staff screens from the Loaf'N Catting v2 handoff with role-safe routing, reusable React UI, and live API integration for every backend-supported operation.

**Architecture:** A role-aware `AdminLayout` owns navigation and page metadata. Focused route pages consume a typed `adminApi` module; shared admin components standardize panels, chips, feedback, dialogs, and toasts. Unsupported backend mutations remain visibly disabled and never update client state as if they succeeded.

**Tech Stack:** React 19, TypeScript 6, React Router 7, React Icons 5, CSS, Vitest 4, Testing Library, ASP.NET Core REST API.

## Global Constraints

- Source of truth: `C:\FPTUniversity\PRN232\Nya\Replicate all screens\Replicate all screens\design_handoff_loaf_n_catting_v2\Loaf n Catting Web App v2.dc.html`.
- Admin sees all eight screens; Staff cannot see or enter Users and Store Location.
- Remove `Trang khách hàng` from the sidebar and retain `Đăng xuất`.
- Use `#F6EEE4`, `#5F2B15`, `#FF6B35`, Playfair Display, Space Grotesk, Space Mono, square 1px-bordered panels, and pill controls exactly as specified.
- Use live APIs where they exist; never simulate a successful unsupported mutation.
- Add no new runtime dependency and do not modify backend controllers or database schema.
- Preserve existing customer, auth, and landing behavior.

---

### Task 1: Typed admin API foundation

**Files:**
- Modify: `frontend/src/api/httpClient.ts`
- Modify: `frontend/src/api/httpClient.test.ts`
- Create: `frontend/src/features/admin/adminTypes.ts`
- Create: `frontend/src/features/admin/adminApi.ts`
- Create: `frontend/src/features/admin/adminApi.test.ts`

**Interfaces:**
- Consumes: `requestJson<T>(path, options)` and authenticated session tokens.
- Produces: `listOrders`, `updateOrderStatus`, `listStoreReservations`, `transitionReservation`, `listAdminProducts`, `createAdminProduct`, `updateAdminProduct`, `deleteAdminProduct`, and `createStaff`.

- [ ] **Step 1: Write failing HTTP-header and endpoint tests**

Add a `headers` expectation to `httpClient.test.ts` and create endpoint tests that assert exact method, body, token, role header, and URL:

```ts
it('merges caller headers with authentication headers', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }),
  ))

  await requestJson('/orders/1/status', {
    method: 'PATCH',
    token: 'staff-token',
    headers: { 'X-Role': 'Staff' },
    body: { orderStatusId: 2 },
  })

  expect(fetch).toHaveBeenCalledWith('/api/orders/1/status', expect.objectContaining({
    headers: expect.objectContaining({
      Authorization: 'Bearer staff-token',
      'Content-Type': 'application/json',
      'X-Role': 'Staff',
    }),
  }))
})
```

```ts
it('updates an order with the role header required by the backend', async () => {
  const request = vi.spyOn(http, 'requestJson').mockResolvedValue(orderDto)
  await updateOrderStatus('token', 'Staff', 1042, 2)
  expect(request).toHaveBeenCalledWith('/orders/1042/status', {
    method: 'PATCH',
    token: 'token',
    headers: { 'X-Role': 'Staff' },
    body: { orderStatusId: 2 },
  })
})
```

- [ ] **Step 2: Run tests and verify RED**

Run: `npm.cmd test -- src/api/httpClient.test.ts src/features/admin/adminApi.test.ts`

Expected: FAIL because `RequestOptions.headers` and `adminApi` do not exist.

- [ ] **Step 3: Add request headers and admin DTOs**

Extend `RequestOptions` and the fetch headers:

```ts
export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  token?: string
  signal?: AbortSignal
  headers?: Record<string, string>
}

headers: {
  Accept: 'application/json',
  ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' }),
  ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
  ...options.headers,
},
```

Define API-shaped types in `adminTypes.ts`, including nested order items/payments and reservation table data:

```ts
export type OperatorRole = 'Admin' | 'Staff'
export type ReservationTransition = 'confirm' | 'cancel' | 'check-in' | 'complete'

export interface AdminOrder {
  orderId: number
  customerUserId: number | null
  customerName: string | null
  orderDate: string
  totalPrice: number
  orderType: string | null
  note: string | null
  orderStatusId: number
  orderStatusName: string
  items: Array<{ orderDetailId: number; productId: number; productName: string; quantity: number; unitPrice: number; subtotal: number }>
  payments: Array<{ paymentId: number; paymentAmount: number; methodId: number; methodName: string; paymentStatus: string; transactionCode: string | null; paymentDate: string; paidAt: string | null }>
}

export interface StoreReservation {
  reservationId: number
  customerUserId: number | null
  customerName: string | null
  customerEmail: string | null
  date: string
  time: string
  numberOfGuests: number
  guestName: string
  guestPhoneNumber: string
  note: string | null
  status: string
  durationMinutes: number
  startAt: string
  endAt: string
  table: { tableId: number; tableName: string; capacity: number; area: string | null; description: string | null }
  tableStatus: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface AdminProduct {
  productId: number
  name: string
  description: string | null
  price: number
  discountPrice: number | null
  unitInStock: number
  picture: string | null
  categoryId: number
  categoryName: string
  isAvailable: boolean
  createdAt: string
  updatedAt: string | null
}

export type AdminProductInput = Pick<AdminProduct, 'name' | 'description' | 'price' | 'discountPrice' | 'unitInStock' | 'picture' | 'categoryId' | 'isAvailable'>

export interface CreateStaffInput {
  name: string
  email: string
  password: string
  phoneNumber: string
  address: string | null
}
```

- [ ] **Step 4: Implement exact API functions**

```ts
export const listOrders = (token: string, signal?: AbortSignal) =>
  requestJson<AdminOrder[]>('/orders', { token, signal })

export const updateOrderStatus = (token: string, role: OperatorRole, orderId: number, orderStatusId: number) =>
  requestJson<AdminOrder>(`/orders/${orderId}/status`, {
    method: 'PATCH', token, headers: { 'X-Role': role }, body: { orderStatusId },
  })

export const listStoreReservations = (token: string, signal?: AbortSignal) =>
  requestJson<StoreReservation[]>('/store/reservations', { token, signal })

export const transitionReservation = (token: string, reservationId: number, transition: ReservationTransition) =>
  requestJson<StoreReservation>(`/store/reservations/${reservationId}/${transition}`, { method: 'PATCH', token })

export const listAdminProducts = (token: string, signal?: AbortSignal) =>
  requestJson<AdminProduct[]>('/admin/products', { token, signal })

export const createAdminProduct = (token: string, input: AdminProductInput) =>
  requestJson<AdminProduct>('/admin/products', { method: 'POST', token, body: input })

export const updateAdminProduct = (token: string, id: number, input: AdminProductInput) =>
  requestJson<AdminProduct>(`/admin/products/${id}`, { method: 'PUT', token, body: input })

export const deleteAdminProduct = (token: string, id: number) =>
  requestJson<void>(`/admin/products/${id}`, { method: 'DELETE', token })

export const createStaff = (token: string, input: CreateStaffInput) =>
  requestJson<User>('/admin/users/staff', { method: 'POST', token, body: input })
```

- [ ] **Step 5: Run tests and commit**

Run: `npm.cmd test -- src/api/httpClient.test.ts src/features/admin/adminApi.test.ts`

Expected: PASS.

Commit: `git commit -m "feat: add typed admin API client"`

---

### Task 2: Role-safe routes and reference-faithful admin shell

**Files:**
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/App.test.tsx`
- Rewrite: `frontend/src/layouts/AdminLayout.tsx`
- Create: `frontend/src/layouts/AdminLayout.test.tsx`
- Create: `frontend/src/features/admin/adminNavigation.ts`
- Create: `frontend/src/features/admin/AdminOnlyRoute.tsx`
- Modify: `frontend/src/styles/v2-admin.css`

**Interfaces:**
- Consumes: `useAuth()`, `ProtectedRoute`, and React Router nested routes.
- Produces: `adminNavigation`, `AdminOnlyRoute`, a route-aware `AdminLayout`, and eight nested route slots.

- [ ] **Step 1: Write failing role and layout tests**

Tests must assert Admin sees all labels, Staff cannot see Users/Store, the removed customer-page link is absent, active links use `aria-current="page"`, and Staff entering `/admin/users` lands at `/admin`.

```tsx
expect(screen.queryByRole('link', { name: /trang khách hàng/i })).not.toBeInTheDocument()
expect(screen.queryByRole('link', { name: /người dùng/i })).not.toBeInTheDocument()
expect(screen.getByRole('link', { name: /đơn hàng/i })).toHaveAttribute('href', '/admin/orders')
```

- [ ] **Step 2: Run tests and verify RED**

Run: `npm.cmd test -- src/layouts/AdminLayout.test.tsx src/App.test.tsx`

Expected: FAIL because navigation is made of disabled buttons and child routes do not exist.

- [ ] **Step 3: Define route metadata and Admin-only guard**

```ts
export const adminNavigation = [
  { key: 'dashboard', to: '/admin', label: 'Tổng quan', subtitle: 'THỨ SÁU, 10/07/2026 · CA SÁNG', roles: ['Admin', 'Staff'] },
  { key: 'orders', to: '/admin/orders', label: 'Đơn hàng', subtitle: 'THEO DÕI VÀ CẬP NHẬT ĐƠN', roles: ['Admin', 'Staff'], badge: '6' },
  { key: 'reservations', to: '/admin/reservations', label: 'Đặt bàn', subtitle: 'LỊCH HẸN VÀ KHÁCH ĐẾN QUÁN', roles: ['Admin', 'Staff'], badge: '3' },
  { key: 'catalog', to: '/admin/catalog', label: 'Thực đơn', subtitle: 'SẢN PHẨM, GIÁ VÀ TỒN KHO', roles: ['Admin', 'Staff'] },
  { key: 'cats', to: '/admin/cats', label: 'Các bé mèo', subtitle: 'HỒ SƠ VÀ TRẠNG THÁI CÁC BÉ', roles: ['Admin', 'Staff'] },
  { key: 'tables', to: '/admin/tables', label: 'Quản lý bàn', subtitle: 'SƠ ĐỒ VÀ TRẠNG THÁI BÀN', roles: ['Admin', 'Staff'] },
  { key: 'users', to: '/admin/users', label: 'Người dùng', subtitle: 'TÀI KHOẢN VÀ PHÂN QUYỀN', roles: ['Admin'] },
  { key: 'store', to: '/admin/store', label: 'Vị trí cửa hàng', subtitle: 'ĐỊA CHỈ, GIỜ MỞ CỬA VÀ TOẠ ĐỘ', roles: ['Admin'] },
] as const
```

Implement `AdminOnlyRoute` with `useAuth()` and `<Navigate to="/admin" replace />` for non-Admin sessions.

- [ ] **Step 4: Build the shell and routes**

Use `<NavLink end>` for Dashboard, route metadata for top-bar title/subtitle, a pill search field, notification circle, authenticated avatar/name/role, and logout. Keep `/admin` wired to the existing dashboard in this task; each remaining child route is added by the task that delivers its complete page. Test `AdminOnlyRoute` in an isolated test router before Admin-only application routes are added in Task 8.

- [ ] **Step 5: Apply shell CSS from the handoff**

Set the desktop sidebar to exactly `256px`, topbar to `76px`, page padding to `26px 28px 40px`, active nav rows to orange pills, and remove all inherited legacy shadows/radii from square panels. Add the 1100px compact-sidebar and 720px horizontal-header behavior without hiding route content.

- [ ] **Step 6: Run tests and commit**

Run: `npm.cmd test -- src/layouts/AdminLayout.test.tsx src/App.test.tsx`

Expected: PASS.

Commit: `git commit -m "feat: add role-safe admin shell and routes"`

---

### Task 3: Shared admin feedback, status, dialog, and toast UI

**Files:**
- Create: `frontend/src/features/admin/components/AdminFeedback.tsx`
- Create: `frontend/src/features/admin/components/AdminStatusChip.tsx`
- Create: `frontend/src/features/admin/components/AdminDialog.tsx`
- Create: `frontend/src/features/admin/components/AdminToast.tsx`
- Create: `frontend/src/features/admin/components/AdminComponents.test.tsx`
- Modify: `frontend/src/styles/v2-admin.css`

**Interfaces:**
- Produces: `AdminFeedback({ state, title, message, onRetry })`, `AdminStatusChip({ value })`, `AdminDialog({ open, title, children, onClose })`, and `AdminToast({ message, tone, onDismiss })`.

- [ ] **Step 1: Write failing accessibility and behavior tests**

```tsx
render(<AdminFeedback state="error" title="Không thể tải" message="Thử lại sau" onRetry={retry} />)
await user.click(screen.getByRole('button', { name: /thử lại/i }))
expect(retry).toHaveBeenCalledOnce()

render(<AdminDialog open title="Thêm sản phẩm" onClose={close}><button>Lưu</button></AdminDialog>)
expect(screen.getByRole('dialog', { name: 'Thêm sản phẩm' })).toBeInTheDocument()
```

- [ ] **Step 2: Run tests and verify RED**

Run: `npm.cmd test -- src/features/admin/components/AdminComponents.test.tsx`

Expected: FAIL because components do not exist.

- [ ] **Step 3: Implement components**

Use semantic `role="status"`, `role="alert"`, `role="dialog"`, `aria-modal="true"`, Escape-to-close, a labeled close button, and status normalization that maps pending/waiting to warning, confirmed/processing to info, complete/paid/available to success, and cancel/expired/out-of-stock to danger.

- [ ] **Step 4: Add exact shared styling**

Feedback and dialog bodies are square white panels with 1px ink borders. Only chips/buttons/inputs/toast are `999px`. Toast is fixed bottom-center, ink background, cream text, and dismisses after 2200ms.

- [ ] **Step 5: Run tests and commit**

Run: `npm.cmd test -- src/features/admin/components/AdminComponents.test.tsx`

Expected: PASS.

Commit: `git commit -m "feat: add reusable admin UI states"`

---

### Task 4: Live dashboard matching the reference

**Files:**
- Rewrite: `frontend/src/pages/admin/AdminDashboardPage.tsx`
- Rewrite: `frontend/src/pages/admin/AdminExperience.test.tsx`
- Modify: `frontend/src/styles/v2-admin.css`

**Interfaces:**
- Consumes: `listOrders`, `listStoreReservations`, `listAdminProducts`, `useAuth`, `AdminFeedback`, and `AdminStatusChip`.

- [ ] **Step 1: Write failing dashboard tests**

Mock the three API calls and assert four metrics, five recent orders, three today's reservations, and low-stock products. Add a partial-failure case where products fail but orders/reservations remain visible.

```tsx
expect(await screen.findByText('6')).toBeInTheDocument()
expect(screen.getByRole('heading', { name: 'Đơn hàng gần đây' })).toBeInTheDocument()
expect(screen.getByRole('heading', { name: 'Đặt bàn hôm nay' })).toBeInTheDocument()
expect(screen.getByRole('heading', { name: 'Sắp hết hàng' })).toBeInTheDocument()
```

- [ ] **Step 2: Run tests and verify RED**

Run: `npm.cmd test -- src/pages/admin/AdminExperience.test.tsx`

Expected: FAIL because the dashboard still uses `catalogRepository` and mock summaries.

- [ ] **Step 3: Implement independent data loading**

Use one `AbortController`, call all three endpoints with `Promise.allSettled`, derive pending orders, today's reservations, stock under 10, and today's revenue. Render per-panel retry/error states so one rejected request does not erase successful data.

- [ ] **Step 4: Recreate reference markup and styling**

Implement the four stat cards; the `1.65fr / 1fr` lower grid; recent-order headers and rows; reservation rows; stock thumbnail/icon, remaining count, and progress bar. Match the handoff sizes, gaps, and borders exactly.

- [ ] **Step 5: Run tests and commit**

Run: `npm.cmd test -- src/pages/admin/AdminExperience.test.tsx`

Expected: PASS.

Commit: `git commit -m "feat: rebuild admin dashboard with live data"`

---

### Task 5: Orders and reservations operations

**Files:**
- Create: `frontend/src/pages/admin/AdminOrdersPage.tsx`
- Create: `frontend/src/pages/admin/AdminOrdersPage.test.tsx`
- Create: `frontend/src/pages/admin/AdminReservationsPage.tsx`
- Create: `frontend/src/pages/admin/AdminReservationsPage.test.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/styles/v2-admin.css`

**Interfaces:**
- Consumes: order and reservation API functions plus shared chips/feedback/toast.

- [ ] **Step 1: Write failing order-page tests**

Assert API load, status filters, the two-column card content, pending mutation state, `X-Role` mutation through `updateOrderStatus`, success toast, and API-error retention.

- [ ] **Step 2: Write failing reservation-page tests**

Assert filters and exact transitions: `Đang chờ -> confirm`, `Đã xác nhận -> check-in`, `Đã đến -> complete`, with cancel available only for active reservations.

- [ ] **Step 3: Run tests and verify RED**

Run: `npm.cmd test -- src/pages/admin/AdminOrdersPage.test.tsx src/pages/admin/AdminReservationsPage.test.tsx`

Expected: FAIL because pages do not exist.

- [ ] **Step 4: Implement Orders**

Use filters `Tất cả`, `Chờ xử lý`, `Đang pha chế`, `Hoàn thành`, `Đã hủy`. Sort newest first. Map the supported next order status IDs as `{ 1: 2, 2: 3, 3: 4 }`; hide the update button when no next transition exists. Render reference cards with order code, total, customer, timestamp, status/payment chips, and the outline pill action.

- [ ] **Step 5: Implement Reservations**

Use filters `Tất cả`, `Chờ xác nhận`, `Đã xác nhận`, `Đã đến`, `Hoàn thành`. Render date/time, guest, guest count, table, status chip, and role-safe transition buttons. Refresh only the mutated record with the API response.

- [ ] **Step 6: Add responsive styling and routes**

Use two columns above 900px and one column below. Keep square white cards and pill controls. Wire `/admin/orders` and `/admin/reservations` to these pages.

- [ ] **Step 7: Run tests and commit**

Run: `npm.cmd test -- src/pages/admin/AdminOrdersPage.test.tsx src/pages/admin/AdminReservationsPage.test.tsx src/App.test.tsx`

Expected: PASS.

Commit: `git commit -m "feat: add admin order and reservation operations"`

---

### Task 6: Catalog CRUD with shared product form

**Files:**
- Create: `frontend/src/features/admin/components/AdminProductForm.tsx`
- Create: `frontend/src/features/admin/components/AdminProductForm.test.tsx`
- Create: `frontend/src/pages/admin/AdminCatalogPage.tsx`
- Create: `frontend/src/pages/admin/AdminCatalogPage.test.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/styles/v2-admin.css`

**Interfaces:**
- Consumes: Admin product APIs and `catalogRepository.listCategories()`.
- Produces: validated `AdminProductInput` submissions for create/update.

- [ ] **Step 1: Write failing form tests**

Assert required name/category, non-negative price/stock, discount not above price, trimmed values, and edit prefill.

- [ ] **Step 2: Write failing catalog tests**

Assert product count/table columns, create/update/delete endpoint calls, delete confirmation, retained error state, and correct table update after success.

- [ ] **Step 3: Run tests and verify RED**

Run: `npm.cmd test -- src/features/admin/components/AdminProductForm.test.tsx src/pages/admin/AdminCatalogPage.test.tsx`

Expected: FAIL because form and page do not exist.

- [ ] **Step 4: Implement the product form**

The form contains name, description, category, price, discount price, stock, picture URL, and availability. Inputs use labeled pill fields; actions use outline cancel and orange save pills. Disable all controls during submission and expose backend `ApiError.detail`.

- [ ] **Step 5: Implement the catalog table**

Load products and categories together. Render exact reference columns and square thumbnails. Open the same dialog for create/edit; confirm deletion in `AdminDialog`; update local rows only after successful API responses.

- [ ] **Step 6: Run tests and commit**

Run: `npm.cmd test -- src/features/admin/components/AdminProductForm.test.tsx src/pages/admin/AdminCatalogPage.test.tsx src/App.test.tsx`

Expected: PASS.

Commit: `git commit -m "feat: add admin catalog CRUD"`

---

### Task 7: Live read-only cats and table management screens

**Files:**
- Create: `frontend/src/pages/admin/AdminCatsPage.tsx`
- Create: `frontend/src/pages/admin/AdminCatsPage.test.tsx`
- Create: `frontend/src/pages/admin/AdminTablesPage.tsx`
- Create: `frontend/src/pages/admin/AdminTablesPage.test.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/styles/v2-admin.css`

**Interfaces:**
- Consumes: `catalogRepository.listCats()` and reservation availability table data.

- [ ] **Step 1: Write failing cats and tables tests**

Assert live cat loading, three-column cards, status chips, table cards, and disabled add/edit/delete controls with the explanation `Backend chưa hỗ trợ thao tác này.`

- [ ] **Step 2: Run tests and verify RED**

Run: `npm.cmd test -- src/pages/admin/AdminCatsPage.test.tsx src/pages/admin/AdminTablesPage.test.tsx`

Expected: FAIL because pages do not exist.

- [ ] **Step 3: Implement Cats**

Reuse `catalogRepository.listCats()` and render the reference circular thumbnail, Playfair name, mono breed, status chip, and disabled action icons. Add retry and empty states.

- [ ] **Step 4: Implement Tables**

Load the current availability-backed table list through `catalogRepository.listAvailableTables(1)`. Render four-column cards with icon, status, Playfair name, uppercase area/capacity, and a read-only notice. Add retry and empty states.

- [ ] **Step 5: Run tests and commit**

Run: `npm.cmd test -- src/pages/admin/AdminCatsPage.test.tsx src/pages/admin/AdminTablesPage.test.tsx src/App.test.tsx`

Expected: PASS.

Commit: `git commit -m "feat: add admin cats and tables screens"`

---

### Task 8: Admin-only Users and Store Location screens

**Files:**
- Create: `frontend/src/features/admin/components/CreateStaffForm.tsx`
- Create: `frontend/src/pages/admin/AdminUsersPage.tsx`
- Create: `frontend/src/pages/admin/AdminUsersPage.test.tsx`
- Create: `frontend/src/pages/admin/AdminStorePage.tsx`
- Create: `frontend/src/pages/admin/AdminStorePage.test.tsx`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/styles/v2-admin.css`

**Interfaces:**
- Consumes: `createStaff`, current authenticated Admin, `AdminDialog`, and `AdminToast`.

- [ ] **Step 1: Write failing Users tests**

Assert Admin access, Staff redirect from Task 2, search presentation, exact reference table shell, create dialog validation, live `createStaff` request, success toast, and disabled role/lock actions.

- [ ] **Step 2: Write failing Store tests**

Assert the six labeled pill fields (name, address, phone, hours, latitude, longitude), reference values, read-only state, disabled save button, and missing-backend explanation.

- [ ] **Step 3: Run tests and verify RED**

Run: `npm.cmd test -- src/pages/admin/AdminUsersPage.test.tsx src/pages/admin/AdminStorePage.test.tsx`

Expected: FAIL because pages do not exist.

- [ ] **Step 4: Implement Users**

Render the reference toolbar and table header. Since no list endpoint exists, show a truthful empty/data-unavailable row rather than mock accounts. The create form validates name, email, phone, and password length of at least eight characters; append the newly created Staff returned by the API to the visible session list.

- [ ] **Step 5: Implement Store Location**

Render the six exact handoff values: `Loaf’N Catting Cafe`, `128 Nguyễn Huệ, Quận 1, TP.HCM`, `028 3822 1188`, `08:00 - 22:00 mỗi ngày`, latitude `10.774300`, and longitude `106.703600`. Keep the save CTA disabled and associate its explanation with `aria-describedby`.

- [ ] **Step 6: Run tests and commit**

Run: `npm.cmd test -- src/pages/admin/AdminUsersPage.test.tsx src/pages/admin/AdminStorePage.test.tsx src/App.test.tsx`

Expected: PASS.

Commit: `git commit -m "feat: add admin-only users and store screens"`

---

### Task 9: Full fidelity, regression, and visual verification

**Files:**
- Modify: `frontend/src/styles/v2-admin.css`
- Modify: `frontend/src/pages/admin/*.tsx` only where visual comparison finds a measurable mismatch.
- Modify: `frontend/src/pages/admin/*.test.tsx` only for discovered regressions.

**Interfaces:**
- Consumes: all completed Admin and Staff routes.
- Produces: a verified final implementation with no known reference mismatch or regression.

- [ ] **Step 1: Run the complete automated suite**

Run: `npm.cmd test`

Expected: every test file and test passes with zero unhandled errors.

- [ ] **Step 2: Run the production build and diff validation**

Run: `npm.cmd run build`

Expected: TypeScript and Vite exit with code 0.

Run from repository root: `git diff --check`

Expected: exit code 0 with no whitespace errors.

- [ ] **Step 3: Start the frontend and backend for browser QA**

Run the backend on its configured development URL and the frontend with `npm.cmd run dev -- --host 127.0.0.1`. Sign in once as Staff and once as Admin.

- [ ] **Step 4: Compare all screens at desktop width**

At `1916x917`, compare each route against the `.dc.html` reference. Verify the exact 256px sidebar, 76px topbar, background/border colors, font roles, square panels, pill controls, grid counts, gaps, table columns, disabled-operation messaging, and absence of `Trang khách hàng`.

- [ ] **Step 5: Verify role and responsive behavior**

At widths 1100px, 720px, and 390px, confirm content remains reachable and controls do not overlap. Confirm Staff navigation and direct URLs exclude Users/Store, while Admin can reach them.

- [ ] **Step 6: Fix only evidence-backed mismatches and rerun verification**

For each mismatch, add or adjust the closest component test, reproduce failure, apply the smallest CSS/markup fix, rerun the focused test, then rerun `npm.cmd test`, `npm.cmd run build`, and `git diff --check`.

- [ ] **Step 7: Commit final fidelity fixes**

Commit: `git commit -m "fix: match admin v2 reference across roles"`
