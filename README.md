# MySets

An app for tracking your own LEGO set collection — search the Rebrickable catalog, add sets to your collection, document builds with photos, write reviews, and follow what other people are building.

## Project status

Currently working through **Phase 0 (Foundation)** — see [Roadmap](#roadmap) below.

Full feature checklist across the roadmap:

**Phase 0 — Foundation**

- [x] backend and frontend project skeletons + Docker Compose (PostgreSQL)
- [ ] user registration and login
- [x] domain model (`User`, `Set`, `Collection`, `Review`) + EF Core migrations, incl. `pg_trgm` search index on `Set.Name`
- [x] import of `.csv` data to db

**Phase 1 — Collection and gallery (core MVP)**

- [ ] search and add sets from the catalog to your own collection
- [ ] photo gallery of the build process for each set
- [ ] reviews and ratings
- [ ] public or private user profile

**Phase 2 — Social & Feed**

- [ ] following other users
- [ ] live "recently built by others" feed
- [ ] comments and likes
- [ ] email notifications (new comment, followed user built a set)

**Phase 3 — Gamification and recommendations**

- [ ] points, ranks and badges for built sets
- [ ] leaderboard (global and among followed users)
- [ ] simple set recommendations based on history/themes

**Phase 4 — DevOps and quality**

- [ ] CI/CD pipeline with automated tests and Docker image builds
- [ ] unit, integration, and e2e tests
- [ ] logging and metrics (observability)
- [ ] full-text search for sets
- [ ] cloud deployment

**Phase 5 — Extensions (stretch goals)**

- [ ] wishlist
- [ ] tracking minifigs separately from sets
- [ ] gallery of own creations (MOC)
- [ ] new-set release calendar
- [ ] content moderation and reporting
- [ ] internationalization (i18n)
- [ ] GDPR compliance tooling

## Tech stack

| Layer           | Technology                                                                                                 |
| --------------- | ---------------------------------------------------------------------------------------------------------- |
| Backend         | ASP.NET Core Web API (.NET 10)                                                                             |
| Database        | PostgreSQL, EF Core + Npgsql                                                                               |
| Auth            | ASP.NET Core Identity (user management only) + a custom BFF-style session (cookie, refresh token rotation) |
| Frontend        | Angular (standalone components, signals)                                                                   |
| Data import     | CsvHelper, Rebrickable set catalog (CSV)                                                                   |
| Dev environment | Docker Compose (PostgreSQL)                                                                                |

## Repository layout

```
MySets/
├── backend/
│   ├── MySets.slnx
│   ├── src/
│   │   └── MySets.Api/            — the actual API (controllers, DbContext, entities, migrations)
│   │       └── Data/
│   │           ├── Entities/      — EF Core entities
│   │           ├── Configurations/ — Fluent API (keys, relationships, indexes) per entity
│   │           └── Migrations/
│   ├── tests/
│   │   └── MySets.Api.Tests/      — backend tests (xUnit)
│   └── tools/
│       └── MySets.Import/         — one-off importer for the Rebrickable set catalog CSV
├── frontend/                      — Angular SPA(CSV), source for the importer
├── docker-compose.yml             — PostgreSQL for local development
```

## Data model (Phase 0)

| Entity           | Key                                       | Notes                                                                                                                 |
| ---------------- | ----------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `User`           | `Guid` (`IdentityUser<Guid>`)             | Identity Core for password/user management; `DisplayName`, `IsPublicProfile`, `Points`, `CreatedAt` are custom fields |
| `RefreshToken`   | `Guid`                                    | server-side refresh token rotation, `TokenHash` is unique                                                             |
| `Set`            | `RebrickableSetNum` (string, natural key) | read-mostly catalog data from the API's perspective, kept up to date by the importer                                  |
| `CollectionItem` | `Guid`                                    | unique `(UserId, SetId)` pair — one collection entry per set per user                                                 |
| `Review`         | `Guid`                                    | unique `(UserId, SetId)` pair — one review per set per user                                                           |

Database column naming convention: snake_case (`EFCore.NamingConventions`). Deleting a `User` cascades to their `CollectionItem`/`Review`/`RefreshToken` rows; deleting a `Set` is blocked (`Restrict`) while related records exist. Searching sets by name is backed by a GIN index with the `pg_trgm` extension (for `GET /api/sets?query=`).

Scope is deliberately narrower than the full Rebrickable export — only `sets.csv` and `themes.csv` are imported (theme flattened to a single name). Parts, colors, minifigs, and inventories stay untouched until a feature actually needs them.

## Running locally

```bash
docker compose up -d
```

Backend (from `backend/src/MySets.Api`):

```bash
dotnet watch run
```

Frontend (from `frontend/`):

```bash
npm start
```

## EF Core migrations

From `backend/src/MySets.Api` (this project is both `--project` and `--startup-project`, so no extra flags needed):

```bash
dotnet ef migrations add <Name>
dotnet ef database update
```

## Roadmap

### Phase 0 — Foundation

Working skeleton: Angular SPA + .NET Web API + PostgreSQL under Docker Compose, user registration and login, `User`/`Set`/`Collection`/`Review` domain model, one-time import of the set catalog from Rebrickable.

### Phase 1 — Collection and gallery (core MVP)

Search and add sets to your own collection, build-process photo gallery, reviews and ratings, public/private profile. Additional tech: MinIO (photo storage), ImageSharp (thumbnails), FluentValidation, Angular Reactive Forms + ngx-dropzone.

### Phase 2 — Social & Feed

Following other users, a live "recently built by others" feed, comments and likes, email notifications. Additional tech: SignalR, Redis, Hangfire/Quartz.NET, MailKit + SendGrid/Postmark.

### Phase 3 — Gamification and recommendations

Points for built sets, ranks, badges, global and followed-users leaderboard, simple set recommendations. Additional tech: Redis (sorted sets), a custom scoring rules engine, topic-similarity heuristics.

### Phase 4 — DevOps and quality

CI/CD pipeline, unit/integration/e2e tests, observability, full-text search, cloud deployment. Additional tech: GitHub Actions, xUnit + NSubstitute + Testcontainers, Playwright, Serilog+Seq or OpenTelemetry, Meilisearch/Typesense, Terraform/Bicep + Azure Container Apps.

### Phase 5 — Extensions (may or may not come)

Wishlist, tracking minifigs separately from sets, MOC (custom build) gallery, new-set release calendar, content moderation, i18n, GDPR compliance tooling.
