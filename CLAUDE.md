# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

OrderHub — an internal order-management web app used as the practice project for a junior AI-agent coding course. The repo has two parts:

- `documents/` — the course material (Chinese): activity guidelines, agent-configuration guides, a `PROCESS.md` journal template. Read `documents/README.md` and `documents/activities/activity-guideline.md` for the exercises.
- `training-repo/` — the actual .NET solution (`OrderHub.sln`). **All `dotnet` commands run from inside `training-repo/`, not the repo root.**

Some bugs in the code are intentional training exercises (see activity 2 in `activity-guideline.md`). Do not "fix" order-list paging, Gold-tier pricing, or stock-restore-on-cancel unless the task explicitly asks for it.

## Commands (run from `training-repo/`)

- `dotnet build` — build the solution
- `dotnet test` — run all xUnit tests (uses EF Core InMemory; no SQL Server needed, does not touch the dev database)
- `dotnet test --filter "FullyQualifiedName~OrderServiceCreateTests"` — run one test class
- `dotnet run --project src/OrderHub.Web` — start the site; first run auto-applies EF migrations and seeds data (20 customers, 50 products, 200 orders, fixed random seed)
- Reset the dev database: `dotnet ef database drop -f -p src/OrderHub.Infrastructure -s src/OrderHub.Web` then `dotnet run --project src/OrderHub.Web`

Runtime needs a local SQL Server (any edition) for `dotnet run`; connection string is in `src/OrderHub.Web/appsettings.Development.json`. Tests do not.

## Architecture

Three-layer, dependencies point inward toward `OrderHub.Core`:

- **`OrderHub.Web`** — ASP.NET Core MVC (.NET 8, Razor Views, Bootstrap 5, all front-end assets local/no CDN). Controllers, ViewModels, Views. Wiring and display only.
- **`OrderHub.Core`** — domain models, service interfaces, and all business logic (discounts, stock, status transitions). No EF Core dependency.
- **`OrderHub.Infrastructure`** — EF Core `OrderHubDbContext`, repositories, migrations, `DbSeeder`.

DI is registered in `src/OrderHub.Web/Program.cs` (repositories and services, all scoped). Migration + seed run at startup there.

Domain flow: `Order` has many `OrderItem`; each item snapshots `UnitPriceSnapshot` at creation time. `Customer.Tier` (Standard/Silver/Gold) drives discount. Order status: Pending → Confirmed → ... / Cancelled.

## Conventions (follow these when adding features)

- Controllers stay thin — only relay service results. All business logic lives in a `Core` service, injected via its interface.
- **Only repositories touch `DbContext`.** Never use EF Core directly in a controller or service.
- Services return `ServiceResult<T>` (`Ok` / `Fail`) for expected failures instead of throwing. See `OrderHub.Core/Common/ServiceResult.cs`.
- Views bind to a ViewModel (hand-written mapping), never to a domain model directly.
- Validate user input with DataAnnotations + `ModelState`; bad input must render a form error, never a 500.
- Money is always `decimal`. Discount logic is centralized in `OrderService.CalculateTotal` / `GetDiscountRate` — do not recompute discounts elsewhere.
- Operation feedback uses `TempData["Success"]` / `TempData["Error"]` (shared alert block in `Views/Shared/_Layout.cshtml`).
- Reference files to copy style from: `ProductsController.cs`, `ProductService.cs` / `IProductService.cs`, `Views/Products/Index.cshtml`.
- C# style is enforced by `training-repo/.editorconfig`: file-scoped namespaces, `var` when type is apparent, Allman braces, 4-space indent, system usings sorted first.

## Danger / do-not-touch

- `src/OrderHub.Infrastructure/Migrations/**` — migrations are history; do not hand-edit.
- `appsettings*.json` / connection strings / any secret file (`*.pfx`, `appsettings.Production.json`, user-secrets) — ask before changing or reading.
- Do not add NuGet packages without asking.
