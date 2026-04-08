# Phase 4 — Polish & Testing

> **Parent document:** [`mainspring-port-plan.md`](mainspring-port-plan.md)

---

## 4.1 Responsive Table / Card Layouts

**Goal:** Ensure all list views work well on mobile.

For each table view (`ScheduledJob/Index`, `Plugin/Index`, `ApplicationConfiguration/Index`, `JobHistory/Index`):

- On `lg+`: standard Tailwind table with `table-auto`, proper `th`/`td` styling, dark mode variants
- On `sm`/`md`: either horizontal scroll (`overflow-x-auto`) or collapse to a stacked card layout using Tailwind `hidden lg:table-cell` toggling

**Acceptance criteria:**

- [ ] All list views are usable on a 375px-wide viewport

---

## 4.2 Toast Notifications

**Goal:** Polish the toast system from 3.1.

- Success toasts (green): job created, job updated, run now queued, configuration saved
- Error toasts (red): validation errors, failed saves
- Auto-dismiss after 4 seconds with fade-out transition
- Dismissible via click

**Acceptance criteria:**

- [ ] Toasts appear for all CRUD operations and Run Now
- [ ] Toasts auto-dismiss and are manually dismissible

---

## 4.3 Schedule Timing Tests

**Goal:** Verify that `ScheduleHelper.ShouldRun` produces correct results.

**Files to create:**

- A test project (`MainSpringTwo.Tests`) with a single test class `ScheduleHelperTests`.

**Test cases (ported from legacy `ShouldRun` logic):**

| Test | Setup | Expected |
|---|---|---|
| Minutely job at exact interval | ScheduleType.Minute, Interval 5, StartTime 00:00, Now 00:05 | `true` |
| Minutely job off interval | Same, Now 00:03 | `false` |
| Hourly job at interval | ScheduleType.Hour, Interval 2, StartTime 00:00, Now 02:00 | `true` |
| Hourly job off interval | Same, Now 01:00 | `false` |
| Daily job at interval | ScheduleType.Day, Interval 1, StartTime 08:00 Day 1, Now 08:00 Day 2 | `true` |
| Weekly job | ScheduleType.Week, Interval 1, StartTime Mon 09:00, Now next Mon 09:00 | `true` |
| Monthly job on matching day | ScheduleType.Month, StartTime Jan 15 10:00, Now Feb 15 10:00 | `true` |
| Monthly job end-of-month fallback | StartTime Jan 31, Now Feb 28 (non-leap) | `true` |
| Monthly job wrong day | StartTime Jan 15, Now Feb 16 | `false` |
| Inactive job never runs | IsActive = false | `false` (checked at caller level, not in ShouldRun) |

**Acceptance criteria:**

- [ ] All test cases pass
- [ ] Tests run via `dotnet test`

---

## 4.4 Hangfire Dashboard Access

**Goal:** Ensure the Hangfire dashboard is only accessible to authenticated users.

- Verify `HangfireAuthorizationFilter` checks `IsAuthenticated`
- Add a "Hangfire" nav link in the sidebar (visible to authenticated users)
- Optionally restrict to a specific role/claim in the future

**Acceptance criteria:**

- [ ] Anonymous access to `/hangfire` is denied
- [ ] Authenticated users can access the dashboard

---

## 4.5 Final Dark Mode Pass

**Goal:** Ensure all views look correct in both light and dark mode.

Audit checklist:
- [ ] Layout shell (sidebar, top bar, footer)
- [ ] All form inputs, selects, textareas
- [ ] All tables (headers, rows, alternating row colors)
- [ ] Toast notifications
- [ ] Identity pages (login, register) — may need custom CSS overrides
- [ ] Hangfire dashboard (external UI — limited control, but can style the container)
- [ ] Error/empty states
- [ ] Pagination controls
