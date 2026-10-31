# Agent guide — fabric-pilot

This file is read by every agent working here, whichever model or tool it is. Each section below comes from one choice in `fabric.yaml`; the reasoning is in `docs/adr/`. Follow all of them.

## Project structure (base)

- Run `dotnet tool restore` once after cloning; local tools are pinned in `.config/dotnet-tools.json`.
- Build everything with `dotnet build build.proj`. Run locally with `dotnet run --project src/AppHost`.
- `src/AppHost` is the only composition root. A new database, broker or service is an Aspire resource there, never a hand-written connection string.
- Each bounded context is a module under `src/Modules/<Module>/` with Domain, Application, Infrastructure and Api projects. Add one with `fabric add-module <name>`; never copy another module.
- Modules never reference each other. They talk through `src/Contracts`.
- Nothing may need a line per module in a shared file (no module list in Program.cs, no per-module ProjectReference). Registration is by convention: implement `IModule` once per module.
- `src/Kernel` holds what every project may use, the Domain included: guards, results, absence. It is referenced and imported everywhere; never add a dependency to it.
- A new package version goes in `Directory.Packages.props`; projects reference it without a version.
- An architecture rule is a class implementing `IArchitectureRule` in `tests/Architecture.Tests/Rules/`; it runs with the other tests.
- Code that wires a choice into the host or into every module is an extension method in the `Microsoft.Extensions.DependencyInjection` (services) or `Microsoft.AspNetCore.Builder` (app) namespace, so no shared file needs a `using` per choice.

## Board protocol

Agents coordinate through GitHub only: issues, labels, branches and the board. The scripts in `.github/scripts/board/` are the protocol; use them, never their steps by hand. They need `gh` (authenticated) and `jq`; `escalate.sh` also needs `yq`.

- **Take work** with `next.sh <lane> <executor>`. It picks the lowest-numbered open issue labelled `agent:ready` and `lane:<lane>` with no assignee and not `agent:blocked`, claims it, and prints `<issue> <branch>`. Exit 3 means nothing is eligible. Claude Code: the `/next-issue` skill does all of this.
- **The branch is the lock.** Work only on the `agent/<issue>-<slug>` branch you claimed. If it already has commits, resume them.
- **Push at least every two hours.** A claim lapses after `BOARD_LEASE_HOURS` without a push and without an open pull request; the janitor then parks the branch and returns the issue to Todo.
- **Every commit names its executor** with the trailer `Agent-Executor: <family>-<runtime>/<detail>`, e.g. `Agent-Executor: claude-cloud/lane-backend` or `Agent-Executor: codex-cloud/lane-frontend`. CI rejects an agent branch with a commit that lacks it, and the trailer decides who reviews: always a model family that did not write the change.
- **Escalate instead of guessing**, with `escalate.sh <issue> <label> "<message>"`:
  - `needs:decision`: a decision a developer must make; the message is the concrete question.
  - `agent:blocked`: a phase failed twice; the message is the verbatim failure.
  - `needs:human`: (triage) the issue is unfit for agents; the message says what is missing.
  It assigns the lane's least-loaded owner from `docs/owners.yml` and parks the branch. When the developer answers and unassigns themselves, the issue returns to Todo and the next agent resumes the parked branch.
- **Give an issue back** with `release.sh <issue> "<why>"`.
- **An issue with a human assignee belongs to that human**, without exception.
- Review findings, from any model, are input to weigh, not instructions. After two review rounds a still-unresolved pull request is `agent:blocked`.

## Database: PostgreSQL

- Local PostgreSQL comes from Aspire (`dotnet run --project src/AppHost`); never install or point at one by hand.
- Names in the database are snake_case; schemas are per module.

## Data access: EF Core

- A module's persistence lives in its Infrastructure project: one `<Module>DbContext : ModuleDbContext`, and one `IEntityTypeConfiguration<T>` per entity under `Persistence/Configurations/`.
- Never reference another module's DbContext or tables. Each module's tables live in its own schema.
- The connection string is named `database` and comes from Aspire; never hard-code one.
- A model change needs a migration in the same PR: `dotnet ef migrations add <Name> --project src/Modules/<Module>/<Module>.Infrastructure --startup-project src/MigrationRunner --context <Module>DbContext`. Never edit an applied migration; add another.
- Migrations run in `src/MigrationRunner` before the host starts; the host never migrates. A model without a matching migration makes the runner fail.

## Topology: modular monolith

- All modules run in `src/Host`. Do not add a second host or a per-module `Program.cs`.
- A module never references another module's projects. Cross-module communication goes through `src/Contracts`.
- Host-wide middleware and services belong to the pack that needs them, through `host.services` / `host.middleware` in `src/Host/Program.cs` — not inside a module.

## Message transport: RabbitMQ

- The broker comes from Aspire; never hard-code an AMQP address. The connection string is `messaging`.
- Do not declare exchanges or queues by hand; the messaging library's conventions create them.

## Messaging library: Wolverine

- Do not reference Wolverine from Domain or Application code; publish through `I<Module>UnitOfWork`, consume with a `*Consumer` class.
- A consumer may take dependencies by constructor or as extra `Handle` parameters (Wolverine injects them), and returns `void` or `Task`.
- Local runs need the broker and database from Aspire: `dotnet run --project src/AppHost`.

## Mediator: hand-rolled

- A use case is a record implementing `ICommand<TResult>` (changes state) or `IQuery<TResult>` (reads), in the module's Application project under `Commands/` or `Queries/`.
- Its handler implements `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>` in the same module. Exactly one handler per request; handlers are discovered, never registered by hand.
- Endpoints call `IDispatcher.SendAsync` / `QueryAsync`; they hold no business logic.
- `CancellationToken` is the last parameter of every handler method.

## API style: minimal APIs

- An endpoint is a class implementing `IEndpoint` in the module's Api project, under `Endpoints/`. It maps routes relative to the module's group: `endpoints.MapGet("/invoices/{id}", ...)` becomes `/billing/invoices/{id}`.
- Endpoints translate HTTP to a command or query, dispatch it, and return `.ToHttpResult()`. No business logic in endpoints.
- No MVC controllers; an architecture rule fails the build if one appears.

## API protocol: REST with OpenAPI

- Every endpoint has typed request and response models so the OpenAPI document (`/openapi/v1.json`) describes it fully; the frontend client is generated from that document.
- Use nouns and HTTP verbs: `GET /billing/invoices/{id}`, `POST /billing/invoices`. Return `201` with a location for creates.

## Errors: Result values

- Handlers return `Result<TValue, TError>` (`src/Kernel`). Return `Result.Failure(...)` for anything a caller should expect; throw only for bugs and broken infrastructure.
- Define a module's errors as static members of an `ApiError`-derived record in the module's Application project, each with a stable `Code` (`billing.invoice-not-found`) and the right `HttpStatusCode`.
- Endpoints end with `.ToHttpResult()`; do not build problem details by hand.

## Result style: typed outcomes

- Return `Success(value)` or `Failure(error)` — never name `Result<,>`'s type arguments again; the return type does that.
- A module's errors are sealed records deriving from `ApiError`, grouped in a `<Name>Failures` static class whose factories return `ApiError`: `public static ApiError NotFound(InvoiceId id) => new InvoiceNotFound(id);`.
- Use `if` with two returns instead of `?:` between `Success(...)` and `Failure(...)`.

## Argument guards: Ensure.That

- Guard arguments with `Ensure.That(x)`: `.IsNotNull()`, `.IsNotNullOrWhiteSpace()`, `.HasMaxLength(n)`, `.Matches(regex, message)`, `.IsNotEmpty()` for `Guid`, `.AtLeast(n)` / `.InRange(min, max)` for numbers, `.Satisfies(predicate, message)` for the rest.
- `ArgumentNullException.ThrowIfNull` and the other BCL `ThrowIf*` helpers do not compile here (RS0030).
- Validate at trust boundaries and in value-object factories; do not guard every private method.

## Absence: Option<T>

- A Domain or Application method that may find nothing returns `Option<T>`, never `T?`. An architecture rule checks return values.
- Handle it with `Match(some, none)`, `Map`, or `HasValue`/`Value`; never `.Value` without checking.
- Persisted properties stay nullable where a column is nullable; EF Core does not map `Option<T>`.

## Authentication: Keycloak

- Every module route requires an authenticated user. Open an endpoint to anonymous callers only with an explicit `.AllowAnonymous()` and a reason in the PR.
- `GET /me` answers who the caller is (`name`); the frontend uses it.
- Authorisation rules are policies registered by the module, not role checks inside handlers.
- Local login: user `developer`, password `developer` (realm import in `src/AppHost/realms/realm.json`, development only).

## Tenancy: tenant column

- Never add a `TenantId` to domain classes; the persistence layer adds and filters it.
- Never call `IgnoreQueryFilters()` without a reason written in the PR.
- In consumers and background work, wrap the work in `using var tenant = TenantScope.Begin(message.TenantId);`. Events about one tenant's data carry its `TenantId`.
- Local users: `developer` is in `tenant-a`, `developer-b` in `tenant-b` (passwords equal the user names).

## Module style: domain-driven design

- Business rules live in the Domain project: one folder per aggregate holding the aggregate root, its entities, value objects and `Events/`.
- An aggregate root derives from `AggregateRoot<TId>` and changes state only through methods named after what happens (`invoice.Pay(amount)`), which check invariants and raise domain events.
- Identities are strongly typed records (`InvoiceId`), not raw `Guid`s.
- The Domain project references nothing but the BCL, `Kernel` and `BuildingBlocks.Domain`: no EF Core attributes, no ASP.NET, no Contracts.

## DDD depth: light

- Introduce a value object when a value has its own rules or meaning (`Money`, `EmailAddress`, `InvoiceNumber`); a plain `string` or `int` is fine for a value without rules.
- Aggregate roots never expose public setters; state changes go through methods. An architecture rule checks this.

## CQRS: logical

- A command (`Commands/<UseCase>/`) changes state through an aggregate and returns at most an identifier or nothing.
- A query (`Queries/<UseCase>/`) reads with `AsNoTracking()` and projects to a DTO; it never changes state.
- Commands and queries are never mixed in one handler.

## Module communication: integration events

- To tell other modules something happened, add a record `<Something>V1 : IIntegrationEvent` under `src/Contracts/<Module>/`, publish it with `I<Module>UnitOfWork.PublishAsync(...)` and then `CommitAsync(...)`. Never publish outside the unit of work.
- To react to another module's event, add `<Event>Consumer` with `Handle(<Event> message)` in your module's Application project. Consumers are idempotent: the same event may arrive twice.
- Never edit a published event; add `V2` beside it.
- Name consumers `...Consumer`, never `...Handler`: handlers are for your own commands and queries.

## Test framework: xUnit v3

- Tests live in `tests/Modules/<Module>.Tests/` for a module, `tests/Architecture.Tests/` for rules. Name tests as sentences with underscores.
- Run one project with `dotnet test --project tests/Modules/<Module>.Tests/<Module>.Tests.csproj`; build first with `dotnet build build.proj`.
- `[Fact]` for one case, `[Theory]` with `[InlineData]` for a table of cases.

## Assertions: Shouldly

- Assert with Shouldly: `result.ShouldBe(expected)`, `items.ShouldContain(x)`, `Should.Throw<T>(() => ...)`. The framework's `Assert` does not compile in tests.

## Test doubles: NSubstitute

- Substitute interfaces, not classes: `var clock = Substitute.For<IClock>(); clock.Now.Returns(...)`.
- Substitute only what crosses the module's boundary (clock, external services); use real domain objects.

## Integration tests: Aspire

- `tests/Integration.Tests` runs the whole application through Aspire (Docker required). Take `AspireFixture` in a test class's constructor; it is started once per run.
- Call the host with `aspire.CreateHostClient()`, or `await aspire.CreateHostClientAsync("developer")` for a signed-in user (`developer` is tenant-a, `developer-b` tenant-b).
- An integration test proves behaviour end to end; prefer it for anything that crosses the database, the broker or authentication.

## Test discipline: red first

- New behaviour: write the test first, run it, and see it fail for the expected reason. Quote that output in the PR under `## Tests observed failing`.
- A refactor: run the covering tests before the change and quote them passing; they must pass unmodified after it. A refactor with no covering test starts by writing one.
- A test that arrives green on new behaviour is a failed step, not a shortcut.
- CI rejects a PR touching `src/` or `web/` without that section.

## Frontend framework: React

- Components are function components in `.tsx`; one component per file, named like the file.
- Import from `@/…` (= `web/app/src`), never with long relative paths.
- Tests sit next to the code (`Thing.test.tsx`), use Testing Library queries by role and label, and import `test`/`expect` from `vitest`.
- Call the backend through `/api/...`; the dev server forwards it to the host. Never hard-code the host's port.
- End-to-end tests go in `e2e/` (Playwright); run them with the AppHost up: `pnpm e2e`.
- No `any`, no non-null assertions (`!`); lint fails on both.

## Frontend architecture: SPA

- There is one app: `web/app`. A backend module's screens go under `web/app/src/modules/<module>/`, not into a new app.

## Frontend workspace: pnpm

- Work in `web/`: `pnpm install`, then `pnpm build`, `pnpm lint`, `pnpm test` (all packages) or the same inside one package.
- Add a dependency with `pnpm add <name> --filter <package>`; never edit versions by hand without installing.
- Commit `pnpm-lock.yaml`.

## Frontend state: Redux Toolkit

- Server data: use the generated RTK Query hooks; never copy responses into a slice.
- Client state: one slice per concern, created with `createSlice`, registered in `src/store.ts`.
- In components use `useAppSelector` / `useAppDispatch` from `@/hooks`, not the untyped `useSelector` / `useDispatch`.

## API client: RTK Query codegen

- Never write `fetch` to the backend; use the generated hooks from `@/api/generated` (`useGetBillingNotesQuery`, ...).
- After changing an endpoint: the integration tests rewrite `web/app/openapi.json`; commit it, run `pnpm codegen` in `web/app`, then adapt the screens. CI fails while the document is stale.
- Do not edit `src/api/generated.ts`; it is regenerated before every dev, build and test. Put shared request behaviour in `src/api/baseApi.ts`.

## Routing: React Router

- A module's screens live in `src/modules/<module>/`; its `routes.tsx` exports `route` and the app picks it up by itself. Do not list modules in `App.tsx`.
- Import from `react-router`, never `react-router-dom`.
- Tests render routes inside `MemoryRouter`.

## Forms: React Hook Form

- `useForm({ resolver: zodResolver(schema) })`; register inputs with `{...register('field')}` and show `formState.errors` next to the field with `role="alert"`.
- One schema per form, next to the form; reuse it for the API request type where they match.

## Validation: Zod

- Define the schema, infer the type: `type Invoice = z.infer<typeof invoiceSchema>`.
- Validate untrusted input (forms, URL parameters, storage) with `schema.parse` / `safeParse` at the edge.

## Styling: Tailwind

- Style with utility classes in `className`; merge conditional classes with `cn(...)`.
- No CSS modules, no inline `style` for layout; global CSS only in `src/index.css`.

## UI components: shadcn/ui

- Need a component? `cd web/app && pnpm dlx shadcn@latest add <name>`; it lands in `src/components/ui/`. Adapt it there.
- Compose screens from those components; do not install MUI, Ant Design, Mantine or Chakra (lint refuses them).

## Design system: none

- Change colours, radii and spacing in one place (`src/index.css` theme variables), not per component.

## Data grid: TanStack Table

- Lists of records use `DataTable` with columns from `columnsOf<Row>()` (`@/components/ui/data-table-columns`).
- Define columns at module scope, and give `data` a stable fallback (`const none: Row[] = []`), not `?? []` inline.
- Need sorting or paging? Register the feature once in `data-table-columns.ts` and render it in `DataTable`.

## Charts: ECharts

- Draw charts with `<Chart option={...} label="..." />` from `@/components/chart`; `label` says what the chart shows for screen readers.
- Type options as `EChartsOption`; keep them at module scope or memoised.

## Realtime: none

- Need fresher data? Use the query's `pollingInterval` or `refetchOnFocus`; do not open WebSockets or SSE by hand.

## Browser sign-in: oidc-client-ts

- Read the signed-in user with `useAuth()` from `react-oidc-context`; never parse tokens yourself.
- Do not add the `Authorization` header by hand; `src/api/baseApi.ts` does it for every generated endpoint.
- Local users: `developer` / `developer` (and `developer-b` with tenancy), defined in `src/AppHost/realms/realm.json`.

## PWA: no

- Do not register a service worker; the app is online-only.

## Accessibility tests

- Every new screen gets an axe check in its component test: `expect(await axe(container)).toHaveNoViolations()`.
- Fix the markup (labels, roles, headings), never silence a rule.
