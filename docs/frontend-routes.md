# Frontend Routes Plan

Page routes for the Bulkivore Nuxt frontend, phased by page in dependency order.
Each phase is independently shippable and leaves no new dead links in the nav.

Backend reference: `POST /api/imports/initialize` → `POST /api/ingestion/inspect-headers`
→ `GET /api/imports/{id}/inspect` → `POST /api/imports/{id}/mappings`
→ `POST /api/imports/{id}/commit`, plus `GET /api/imports` (paged list),
`GET /api/imports/{id}` (detail), `GET /api/imports/{id}/errors` + `/errors/export`,
`GET /api/schema/{tableName}`.

## Conventions

- Shell: `UDashboardGroup` + collapsible sidebar + `UDashboardPanel` in
  `app/layouts/default.vue`. Navbar title is per-page; mobile uses
  `UDashboardSidebarToggle`.
- Page shape: `UPageHeader` (title, description, action links) + `UCard` content.
- Width rule: list/table pages are full-bleed (tables need width); form/wizard/detail
  pages constrain to `max-w-2xl/3xl` centered.
- Data fetching lives in the feature component (`ImportTable` pattern), not the page —
  pages stay as header + card + component. Re-extract presentational parts only when a
  second consumer appears.
- Tables: sticky header + viewport-capped scroll body (`max-h-[calc(100dvh-25rem)]`)
  so lists stay on one screen; pagination + page-size selector in the component footer.

## Phase 1 — Imports list + `/` redirect ✅ Done

| Route | File | Content |
|---|---|---|
| `/` | `app/pages/index.vue` | `definePageMeta({ redirect: '/imports' })`, no UI |
| `/imports` | `app/pages/imports/index.vue` | `UPageHeader` + disabled "New import" button + `UCard` + `<ImportTable />` |

- `app/components/imports/ImportTable.vue`: self-contained — `listImports` query,
  `page`/`pageSize` state (selector: 10/20/50/100, resets to page 1 on change),
  status badges, formatted counts/dates, empty state, `UPagination` footer.
- `app/layouts/default.vue`: `Home` nav item removed.
- The "New import" button stays **disabled** until Phase 2 lands.

## Phase 2 — New import wizard (`/imports/new`)

Single page + `UStepper`, session id held in component state. Refresh mid-wizard
restarts (accepted v1 behavior; backend sessions are cheap).

| Step | Backend |
|---|---|
| Upload | `POST /imports/initialize` + S3 PUT to `uploadUrl` + `POST /ingestion/inspect-headers` |
| Inspect | `GET /imports/{id}/inspect` (headers, suggested mappings, preview rows) |
| Map | `POST /imports/{id}/mappings` |
| Review & commit | `POST /imports/{id}/commit` → navigate to `/imports/[id]` |

New files: `app/pages/imports/new.vue`, `components/imports/ImportWizard.vue`
(stepper shell), `UploadStep.vue`, `MappingEditor.vue`, `ReviewStep.vue`.
Enable the Phase 1 "New import" button (`to: '/imports/new'`) in this phase.

## Phase 3 — Import detail (`/imports/[id]`)

- `app/pages/imports/[id].vue` + `components/imports/ImportErrorsTable.vue`.
- Header from `GET /imports/{id}` (file, status badge, counts); tabs **Overview** /
  **Errors** (`GET /imports/{id}/errors` table + export button → `/errors/export`).
- Wire `ImportTable` row-click → detail route (only once this page exists).
- Depends on Phase 1; pairs with Phase 2 (wizard lands here).

## Phase 4 — Schema (`/schema`)

- `app/pages/schema.vue`: table picker (`USelectMenu`) driving
  `GET /api/schema/{tableName}`; column-metadata table.
- Independent — can run parallel to Phase 2/3. No `[table]` deep links (picker state only).

## Phase 5 — Settings (`/settings`)

- `app/pages/settings.vue`: static only (appearance, preference placeholders), no API.
- Independent, smallest. Completes the nav with zero dead links.
