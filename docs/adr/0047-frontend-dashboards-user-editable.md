# Dashboards: user-editable

## Context

Users want to arrange the figures they watch on dashboards of their own. A dashboard that could name any query would get around every endpoint's authorization, and a saved dashboard is state that needs a home following the same module, tenancy and retry rules as everything else.

## Decision

- **A `dashboards` module stores them** (CRUD, logical CQRS). A dashboard row holds its owner (the token's `sub`, through `ICurrentUser`), its name and its widgets as one JSON column; each widget names a declared widget, a visual (`bar`, `line`, `pie` or `table`) and its place on a 12-column grid. Every read filters by owner, and by tenant through the persistence layer.
- **Writes are idempotent.** The client chooses the id; `PUT /dashboards/{id}` creates or replaces and `DELETE /dashboards/{id}` removes, so a retried request applies once. Another user's id is not found for the caller, and inserting it fails on the primary key (409): no one replaces someone else's dashboard. `GET /dashboards` is paged; `GET /dashboards/{id}` reads one.
- **Widgets are declared, typed, per module.** A module's `IModuleWidgets` (Application project) lists each widget with its key, title and how to build its query; a chart's query answers `ChartSeries`, a table's `PagedResult<TRow>`, and the compiler checks it. Its id is `<module>.<key>`. The query takes nothing but the page, so no caller chooses its input.
- **Nothing else is reachable.** `GET /dashboards/widgets` lists the declared widgets whose policy the caller holds, `GET /dashboards/widget-declarations/{id}` describes one of those, `GET /dashboards/widgets/{id}` runs one through the dispatcher as the caller, and saving a dashboard naming any other is refused with `dashboards.unknown-widget`. A widget carries its module's policy because it bypasses the endpoint that would otherwise check it.
- **One definition of a valid widget**, `WidgetCatalog.Scan`: a query of the declaring module's Application project with exactly one handler, ids unique, a named policy registered by that module. The host refuses to start on a problem; the architecture rule `EveryWidgetIsADeclaredModuleQuery` fails the build on the same ones and on an `IModuleWidgets` outside an Application project.

## Consequences

- A module offers a widget by changing its own Application project; no shared file lists widgets.
- The rule sees that a widget is declared, not that its query reads only what its viewers may see; that stays the query's business, as for an endpoint.
- **The page** (`/dashboards`) lists the user's dashboards, opens one or starts a new one with a client-made v7 UUID, and edits it on a react-grid-layout 2.3.0 grid of 12 columns: add a declared widget as a bar, line or pie chart or a table, move, resize and remove widgets, rename, and save with the PUT above. Each widget draws `GET /dashboards/widgets/{id}` with the project's `Chart` or `DataTable`, under the title its declaration gives (`GET /dashboards/widget-declarations/{id}`), whatever page the widget picker shows. It uses only the generated API client.
