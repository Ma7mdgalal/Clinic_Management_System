# Implementation Report — Sprint 1, Sprint 2, CMS-010

| | |
|---|---|
| **Project** | Clinic Management System |
| **Date** | 2026-08-26 |
| **Scope** | Sprint 1, Sprint 2, CMS-010 (Specialty management) |
| **Source of truth** | [`Clinic_Management_System_README.md`](../Clinic_Management_System_README.md) |
| **Build** | `Clinic.slnx` — 0 warnings, 0 errors |
| **Migration** | `20260825211211_CMS010_AlignDomainWithReadme_RemoveIdentityTph` — applied |
| **Verification** | 40 HTTP assertions against the running API — all passing |
| **Explicitly excluded** | CMS-011, CMS-012, CMS-013, CMS-014, Sprints 4–6 |

---

## Table of contents

1. [Executive summary](#1-executive-summary)
2. [Critical problems fixed](#2-critical-problems-fixed)
3. [Important improvements](#3-important-improvements)
4. [Optional — noted, not implemented](#4-optional--noted-not-implemented)
5. [Good decisions kept](#5-good-decisions-kept)
6. [Requirement traceability](#6-requirement-traceability)
7. [File inventory](#7-file-inventory)
8. [Verification evidence](#8-verification-evidence)
9. [Action items for the developer](#9-action-items-for-the-developer)
10. [Outstanding work](#10-outstanding-work)

---

## 1. Executive summary

The architecture underneath this project was sound. What was missing was security discipline and
the habit of checking each requirement against the README rather than against whether the
application responds.

Nine issues were **critical** — each one either blocked a stated README requirement or was an
exploitable security hole. The most serious was not a crash but a *silent* one: no `Admin` account
could exist by any code path, which made every admin-only endpoint permanently unreachable and
CMS-010 impossible to test. The application compiled and started cleanly the whole time.

| Severity | Count | Nature |
|---|---|---|
| 🔴 Critical | 9 | Blocked a requirement, or exploitable |
| 🟠 Important | 13 | Correctness, layering, data integrity, REST semantics |
| 🔵 Optional | 4 | Recorded with rationale, deliberately not implemented |
| ✅ Good decisions | 7 | Kept unchanged |

---

## 2. Critical problems fixed

### 2.1 No `Admin` account could ever exist

| | |
|---|---|
| **Impact** | Every `RequireAdmin` endpoint permanently unreachable; CMS-010 untestable |
| **Cause** | Registration hardcoded `Patient`; `AssignRoleAsync` excluded `Admin` from the assignable list; the seeder created *roles* but no *users* |
| **Fix** | [`IdentitySeeder.cs`](../Clinic/Clinic.Infrastructure/identity/IdentitySeeder.cs), [`BootstrapAdminSettings.cs`](../Clinic/Clinic.Application/Common/BootstrapAdminSettings.cs) |

A three-way deadlock, invisible from the code because each piece is defensible alone. The password
comes from user secrets only; if unset, the seeder logs a warning and skips. **No fallback password
was invented** — a hardcoded default is a published administrator credential for every deployment.

### 2.2 JWT signing key committed to `appsettings.json`

| | |
|---|---|
| **Impact** | Anyone with repo read access could mint a token claiming `role: Admin` |
| **README** | §23 — "avoid exposing sensitive data" |
| **Fix** | Key moved to user secrets; [`JwtSettings.cs`](../Clinic/Clinic.Application/Common/JwtSettings.cs) now `[Required]` + `[MinLength(32)]` with `.ValidateOnStart()` |

Authentication is worth nothing once the signing key is public. Startup validation means a missing
or too-short key fails at boot instead of quietly producing forgeable tokens at runtime.

### 2.3 Accidental Identity TPH inheritance still present

| | |
|---|---|
| **Impact** | Dead `Discriminator` columns on `AspNetUsers` and `AspNetRoles` |
| **Cause** | `AppDbContext : IdentityDbContext` (non-generic) makes EF treat `AppUser` as a *derived* type of `IdentityUser` |
| **Fix** | `IdentityDbContext<AppUser, ApplicationRole, string>` in [`AppDbContext.cs`](../Clinic/Clinic.Infrastructure/Persistence/AppDbContext.cs); migration drops both columns |

The earlier `FixIdentityDiscriminatorAndDoctorPrecision` migration did **not** fix this — the
discriminators were still present in eight places in `AppDbContextModelSnapshot.cs`. Patching a
migration cannot fix a model-level mistake; the model has to change.

### 2.4 Deactivating a specialty performed a hard delete

| | |
|---|---|
| **Impact** | Fails outright once a doctor references the specialty (`DeleteBehavior.Restrict`); if it ever succeeded it would erase the row all historical appointments point at |
| **README** | §5.3 — an admin *deactivates* a specialty; deletion is never mentioned |
| **Fix** | [`SpecialtyService.DeactivateAsync`](../Clinic/Clinic.Application/Services/SpecialtyService.cs) flips `IsActive`, idempotently |

`DELETE` remains the correct verb — from the client's view the specialty is being retired. The
soft-delete is a persistence decision, not an API one.

### 2.5 Sync-over-async in the token generation path

`_userManager.GetRolesAsync(user).Result` blocks a thread-pool thread waiting on a database
round-trip. Under load this is how an API deadlocks itself. Now awaited in
[`JwtService.cs`](../Clinic/Clinic.Infrastructure/identity/Services/JwtService.cs).

### 2.6 The role-assignment endpoint was not under `/api`

`[HttpPost("/{id}/{Role}")]` — a **leading slash makes a route template absolute**, discarding the
controller's `api/[controller]` prefix. The endpoint was published at `POST /{id}/{role}` on the
application root, shadowing any top-level two-segment route.

It also returned `Ok(true)` / `BadRequest(false)` — a bare boolean as the entire response body, with
`400` used indiscriminately for "already has the role" (409), "no such user" (404) and "not a
grantable role" (400).

### 2.7 Login revealed which usernames exist

| | |
|---|---|
| **Impact** | Account enumeration — an attacker can build a list of valid usernames before attacking passwords |
| **Cause** | Distinct messages for unknown-user vs wrong-password, **and** a timing side channel: the unknown-user path skipped password hashing and returned far faster |
| **Fix** | One fixed `InvalidCredentialsException` message, plus a throwaway hash on the unknown-user path so wall-clock cost matches |

Identical messages alone are not enough. Timing leaks the same information to anyone with a
stopwatch.

### 2.8 Registration was not atomic

`CreateAsync` followed by `AddToRoleAsync` as two independent operations. If the second failed, the
result was a user with no role — able to pass `[Authorize]` but fail every policy, permanently
stuck, and blocking their own email from re-registration. Now wrapped in
`await using var transaction`, so a `Dispose` without `Commit` rolls back.

### 2.9 `GET /api/auth/me` did not exist

README §5.1 requires a current-user endpoint outright. Now implemented in
[`AuthController.cs`](../Clinic/Clinic.Api/Controllers/AuthController.cs), reading the id from the
**validated token** — never from the URL or body, which would let any authenticated user read
anyone else's profile.

---

## 3. Important improvements

| # | Problem | Why it matters | Fix |
|---|---|---|---|
| 3.1 | No centralized error handling (README §19) | Services threw `ArgumentNullException`/`InvalidOperationException` → all 500s; controllers guessed status codes from `bool`/`null` returns | [`AppException`](../Clinic/Clinic.Application/Common/Exceptions/AppException.cs) hierarchy + [`AppExceptionHandler`](../Clinic/Clinic.Api/Middleware/AppExceptionHandler.cs) → RFC 9457 ProblemDetails with `traceId`. **Status codes now decided in one place.** |
| 3.2 | Over-posting via shared DTOs | `SpecialtyDto` carried `Id` and `IsActive` on create; `RegisterRequest` had a `Role` property — straightforward privilege escalation | Split [`CreateSpecialtyRequest`](../Clinic/Clinic.Application/DTOs/Specialties/CreateSpecialtyRequest.cs) / [`UpdateSpecialtyRequest`](../Clinic/Clinic.Application/DTOs/Specialties/UpdateSpecialtyRequest.cs); `RegisterRequest` has no `Role` at all |
| 3.3 | Doctor had no `Email` or `Phone` | README §5.2 requires both | Added to the entity, configuration and migration |
| 3.4 | Patient had no `Email` or `EmergencyContact`; `ContactNumber` did not match the required name | README §6 requires all three | Added; `ContactNumber` **renamed** to `Phone` |
| 3.5 | `DateOfBirth` stored as `datetime2` | A birth date has no time component; storing one invites timezone bugs | `DateOnly` → SQL `date` |
| 3.6 | Application layer referenced Serilog | Layering violation — the abstraction belongs in the inner layer, the provider only in the host | `Microsoft.Extensions.Logging.Abstractions`; deleted the static `AppLogger` wrapper (a static logger cannot be substituted in tests) |
| 3.7 | Log messages built with `$"..."` interpolation | Collapses everything into one opaque string — you lose the ability to query by field, which is the entire point of structured logging | Message templates: `"Specialty created. SpecialtyId={SpecialtyId} Name={Name}"` |
| 3.8 | Name lookup was non-sargable | `s.Name.ToLower() == name.ToLower()` emits `LOWER([Name]) = …`, which cannot use the index on `Name` — a full scan on every create | Direct comparison. SQL Server's default collation is **already** case-insensitive, so the behaviour is unchanged — verified below |
| 3.9 | `Patient.UserId` required and uniquely indexed | A walk-in patient with no login could not be recorded at all, and a second one would collide on `''` | Nullable + filtered unique index `HasFilter("[UserId] IS NOT NULL")` — many NULLs allowed, real values still unique |
| 3.10 | Swagger had no auth support | Not a single protected endpoint could be exercised from the UI | Bearer security definition + requirement in [`Configs.cs`](../Clinic/Clinic.Api/Extensions/Configs.cs) |
| 3.11 | `RequireUniqueEmail` unset; `ClockSkew` left at the 5-minute default | Duplicate emails allowed; a token stayed valid 5 minutes past its stated expiry | `RequireUniqueEmail = true`; `ClockSkew = TimeSpan.Zero` |
| 3.12 | Repository had no unit of work; `GetByIdAsync` threw on not-found | `SaveChangesAsync` inside every method means two related writes cannot share a transaction; throwing forces callers to use exceptions for control flow | `Add`/`Update`/`Remove` stage, `SaveChangesAsync` commits separately |
| 3.13 | `RequireAdmin` on the whole specialty controller | A patient could not even **list** specialties — which is exactly what the booking flow needs | Class-level `[Authorize]`; the admin policy on write actions only. Least privilege cuts both ways |

### 3.14 The scaffolded migration would have destroyed phone data

EF emitted `DropColumn ContactNumber` + `AddColumn Phone`. EF **cannot detect renames** — it compares
models column by column, sees one gone and another appeared, and scaffolds a drop plus an add. That
throws away every stored phone number.

Hand-edited to:

```csharp
migrationBuilder.RenameColumn(name: "ContactNumber", table: "Patients", newName: "Phone");
migrationBuilder.Sql("UPDATE [Patients] SET [Phone] = '' WHERE [Phone] IS NULL;");
migrationBuilder.AlterColumn<string>(name: "Phone", table: "Patients", …, nullable: false, …);
```

with the mirror image in `Down()`. The table is empty today — but a migration has to be correct
against the database it will meet in **production**, not the one on this machine. A `Down()` that
loses data is a `Down()` nobody dares run.

---

## 4. Optional — noted, not implemented

| Item | Rationale for not implementing |
|---|---|
| **Account lockout / brute-force throttling** | Checked the README before deciding: §23 asks only to "hash passwords securely", and CMS-009's acceptance criteria do not mention it. Genuinely not a requirement — but a real clinic system wants it. One line: `options.Lockout.MaxFailedAccessAttempts = 5` plus `CheckPasswordSignInAsync(…, lockoutOnFailure: true)` |
| **`CancellationToken` threading** through controller → service → repository | Mechanical, and a worthwhile exercise — left deliberately |
| **Short role claim type** | The token uses the long `schemas.microsoft.com/…/role` URI, making tokens larger than necessary. Shortenable via `RoleClaimType`, but purely cosmetic |
| **FluentValidation, email confirmation, refresh tokens** | None are in README scope |

---

## 5. Good decisions kept

These were right and were left unchanged:

1. **The layered structure matches README §16 exactly**, with dependencies pointing inward
   (`Api → Infrastructure → Application → Domain`) and no EF reference in `Domain`.
2. **`IEntityTypeConfiguration<T>` per entity** rather than one sprawling `OnModelCreating`.
3. **`Roles` / `Policies` as constants** instead of magic strings scattered across attributes.
4. **Repository interfaces in Application, implementations in Infrastructure** — dependency
   inversion done properly, not just talked about.
5. **Options pattern** for `JwtSettings` rather than injecting `IConfiguration` everywhere.
6. **Serilog configured with rolling files, retention and enrichers** — more thought than most
   projects give logging.
7. **Migrations named after their task**, plus a `HealthController`.

---

## 6. Requirement traceability

### README §5.1 — Authentication

| Requirement | Status | Where |
|---|---|---|
| User registration | ✅ | `POST /api/auth/register` |
| Login | ✅ | `POST /api/auth/login` |
| Secure password storage | ✅ | ASP.NET Core Identity hasher |
| JWT issuance | ✅ | [`JwtService.cs`](../Clinic/Clinic.Infrastructure/identity/Services/JwtService.cs) |
| Role-based authorization | ✅ | 5 policies in [`Configs.cs`](../Clinic/Clinic.Api/Extensions/Configs.cs) |
| **Current-user endpoint** | ✅ **added** | `GET /api/auth/me` |

### README §5.3 / CMS-010 — Specialty management

| Requirement | Status | Where |
|---|---|---|
| Admin can create a specialty | ✅ | `POST /api/specialties` → 201 + `Location` |
| Admin can update a specialty | ✅ | `PUT /api/specialties/{id}` → 200 |
| Admin can **deactivate** a specialty | ✅ **was a hard delete** | `DELETE /api/specialties/{id}` → 204 |
| Names must not be duplicated | ✅ | Unique index + `NameExistsAsync`; trim + case-insensitive |
| Non-admins must not write | ✅ | 403 verified for all three write verbs |
| Non-admins can still read | ✅ **was blocked** | 200 for Patient and Doctor |

### CMS-009 acceptance criteria

| Criterion | Status | Evidence |
|---|---|---|
| Users can authenticate | ✅ | 200 on login for both accounts |
| Protected endpoints reject unauthenticated requests | ✅ | 401 on `/me`, specialties, roles |
| Roles restrict access correctly | ✅ | 403 for Patient on every admin action |
| **Passwords are never returned through the API** | ✅ | grep of `/me` for `password\|hash\|stamp` → **0 matches** |

### README §5.2 / §6 — required fields

| Entity | Required by README | Status |
|---|---|---|
| Doctor | `Email`, `Phone` | ✅ both added |
| Patient | `Phone`, `Email`, `EmergencyContact` | ✅ all three (`ContactNumber` renamed to `Phone`) |

### Cross-cutting

| Requirement | Status |
|---|---|
| §17 — REST conventions and proper status codes | ✅ 201/200/204/400/401/403/404/409 all exercised |
| §19 — centralized error handling | ✅ `IExceptionHandler` + ProblemDetails |
| §20 — never log passwords, tokens or unnecessary patient data | ✅ audited |
| §23 — security requirements | ✅ see §2.2, §2.7, §3.2 |
| §26 — Definition of Done: "code is committed to Git" | ❌ **not done** — see §9 |

---

## 7. File inventory

### Created

| Layer | Files |
|---|---|
| Application — errors | `Common/Exceptions/AppException.cs` |
| Application — settings | `Common/BootstrapAdminSettings.cs`, `Common/Policies.cs`, `Common/Roles.cs`, `Configuration/Extensions.cs` |
| Application — DTOs | `DTOs/Auth/` ×5, `DTOs/Specialties/` ×3 |
| Application — contracts | `Interfaces/Repository/` ×2, `Interfaces/Services/` ×2 |
| Application — logic | `Services/SpecialtyService.cs` |
| Infrastructure | `Repository/GenericRepository.cs`, `Repository/SpecialtyRepository.cs`, `identity/IdentitySeeder.cs` |
| Infrastructure — migration | `20260825211211_CMS010_AlignDomainWithReadme_RemoveIdentityTph.cs` (+ designer) |
| Api | `Controllers/SpecialtiesController.cs`, `Middleware/AppExceptionHandler.cs` |

### Modified

`AuthController.cs` · `Program.cs` · `Extensions/Configs.cs` · `appsettings.json` ·
`appsettings.Development.json` · `Clinic.Api.http` · `Clinic.Application.csproj` ·
`Common/JwtSettings.cs` · `Domain/Models/{Doctor,Patient,Specialty}.cs` ·
`DomainConfigurations/{Doctor,Patient,Specialty}Configuration.cs` ·
`Configuration/Extensions.cs` · `Persistence/AppDbContext.cs` ·
`identity/Services/{AuthService,JwtService}.cs` · model snapshot + 6 prior migrations
(namespace `Clinic.Infrastructure.Data` → `Clinic.Infrastructure.Persistence`, `using` line only —
**no schema content altered**)

### Deleted

| File | Reason |
|---|---|
| `Interfaces/IAuthService.cs` | Moved to `Interfaces/Services/` |
| `Common/Rules.cs` | Superseded by `Roles.cs` + `Policies.cs` |
| `DTOs/SpecialtyDto.cs` | Split into request/response DTOs (over-posting) |
| `DTOs/AuthResult.cs` | Hand-rolled `{ Success, … }` envelope — the status code already carries success |
| `DTOs/RegisterRequest.cs`, `DTOs/LoginRequest.cs` | Moved to `DTOs/Auth/`; `Role` property removed |
| `Logging/AppLogger.cs` | Static logger, not substitutable in tests |

---

## 8. Verification evidence

Build ×2 (0 warnings / 0 errors) · `dotnet ef database update` → *Done.* · API boot ≈ 1 s with
`Bootstrap administrator created. UserId=… UserName=admin` · **40 HTTP assertions**:

### Registration — 4

| Case | Expected | Actual |
|---|---|---|
| Valid registration | 201 + `Location` | ✅ |
| Duplicate username | 409 | ✅ |
| Weak password | 400 with `errors[]` | ✅ |
| Missing required field | 400 | ✅ |

### Login — 4

| Case | Expected | Actual |
|---|---|---|
| Admin credentials | 200 + token | ✅ |
| Patient credentials | 200 + token | ✅ |
| Wrong password | 401, generic message | ✅ |
| Unknown username | 401, **byte-identical** response | ✅ |

### `GET /api/auth/me` — 3

| Case | Expected | Actual |
|---|---|---|
| With valid token | 200 + correct roles | ✅ |
| Without token | 401 | ✅ |
| Body scanned for `password\|hash\|stamp` | 0 matches | ✅ |

### Specialties — read — 3

| Case | Expected | Actual |
|---|---|---|
| Anonymous | 401 | ✅ |
| As Patient | 200 | ✅ |
| As Doctor | 200 | ✅ |

### Specialties — write — 10

| Case | Expected | Actual |
|---|---|---|
| `POST` as Admin | 201 + `Location: …/api/Specialties/{id}` | ✅ |
| `PUT` same name, new description | 200 — must **not** 409 against itself | ✅ |
| `PUT` to a name another row owns | 409 | ✅ |
| `PUT` unknown id | 404 | ✅ |
| `DELETE` as Admin | 204 | ✅ |
| `DELETE` again | 204 (idempotent) | ✅ |
| `DELETE` unknown id | 404 | ✅ |
| `POST` as Patient | 403 | ✅ |
| `PUT` as Patient | 403 | ✅ |
| `DELETE` as Patient | 403 | ✅ |

### Validation and routing — 3

| Case | Expected | Actual |
|---|---|---|
| 1-character name | 400 | ✅ |
| `"  cardiology  "` vs stored `"Cardiology"` | 409 — proves trim **and** case-insensitive collation | ✅ |
| Non-GUID id | 404 from the `:guid` route constraint, not 400 from binding | ✅ |

### Role assignment — 9

| Case | Expected | Actual |
|---|---|---|
| Assign `Doctor` as Admin | 204 | ✅ |
| Assign the same role again | 409 | ✅ |
| Assign lowercase `receptionist` | 204 (normalized) | ✅ |
| **Assign `Admin`** | **400 — not 204** | ✅ |
| Assign `SuperUser` | 400 | ✅ |
| Empty role | 400 with `errors.Role` | ✅ |
| Unknown user id | 404 | ✅ |
| Patient self-grants `Admin` | 403 | ✅ |
| Anonymous | 401 | ✅ |

### Soft delete — 4

| Case | Expected | Actual |
|---|---|---|
| Default list after `DELETE` | deactivated row absent | ✅ |
| `?includeInactive=true` | row present | ✅ |
| That row's `isActive` | `false` | ✅ |
| Underlying table | **row still there, not deleted** | ✅ |

### Round-trip check

After assigning `Doctor`, a **fresh** login produced a token carrying
`claims/role: ["Doctor","Patient"]`; that token read specialties (200) and was still refused on
create (403). Role changes propagate on the next login, and adding a role grants no more than that
role.

---

## 9. Action items for the developer

### 🔴 9.1 Three self-promoted `Admin` accounts exist in the database

Query of `AspNetUsers` × `AspNetUserRoles`:

```
admin        ->  Admin          (created by the new seeder)
Admin123     ->  Admin          <-- created through the vulnerable endpoint
Mohamed1232  ->  Admin          <-- created through the vulnerable endpoint
Mohamed1233  ->  Admin          <-- created through the vulnerable endpoint
Mohamed1234  ->  Patient
Mohamed1235  ->  Receptionist   <-- Receptionist but NOT Patient
```

`Mohamed1235` holding `Receptionist` and **not** `Patient` is the proof: registration was taking the
role from the request body rather than hardcoding `Patient`. The hole is closed, but those four
accounts were created through it and **should be deleted**. This is what privilege escalation looks
like in a real incident review — you find it in the data, not the code.

### 🔴 9.2 Change the bootstrap password

Written to user secrets (`clinic-management-system-api`):

| Secret | Value | Action |
|---|---|---|
| `Jwt:Key` | freshly generated, 64 random characters | Nothing — but **every token issued before now is invalid**, which is correct: the old key was public |
| `BootstrapAdmin:Password` | `ChangeMe1Admin` | **Change it** |

### 🟠 9.3 Nothing is committed

README §26 lists *"The code is committed to Git"* in the Definition of Done, and roughly 50
changed/new files are uncommitted. There is also **no git remote configured**, so CMS-004 (push to
GitHub) is something only you can do.

### 🔵 9.4 Test data left behind

| Item | Note |
|---|---|
| User `roletest` | Created during verification |
| Specialty `Neurology` | Created then deactivated |
| Account `pat1` | **I added `Doctor` + `Receptionist` while testing the role endpoint.** Say the word and I will revert it |

---

## 10. Outstanding work

### Out of scope by instruction

CMS-011 (Doctor CRUD) · CMS-012 (Patient CRUD) · CMS-013 (patient search) ·
CMS-014 (pagination) · Sprints 4–6.

### Not started, and it will bite you

`Clinic.Tests` contains a `.csproj` and nothing else — **zero `.cs` files and no
`<ProjectReference>` to any project it is meant to test.** It compiles, which is exactly why it is
easy to believe it is fine.

CMS-031 / CMS-032 will need it, and the pieces built here are the easiest things you will ever
unit-test: `SpecialtyService` takes an interface and a logger, so it needs no database at all.
Start there.

| Target | Why it is a good first test |
|---|---|
| `SpecialtyService.CreateAsync` — duplicate name | Pure logic, mock `ISpecialtyRepository` |
| `SpecialtyService` — whitespace-only name | Asserts `ValidationException`, no I/O |
| `SpecialtyService.DeactivateAsync` — idempotency | Two calls, one expected state |
| `AppExceptionHandler.Translate` | Table-driven: exception type → status code |
