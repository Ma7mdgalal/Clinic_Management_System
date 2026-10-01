# Sprint 3 Review — CMS-010 … CMS-014

**Reviewer:** Senior Backend Engineer / Tech Lead
**Date:** 2026-09-05
**Scope:** Specialty CRUD (CMS-010), Doctor CRUD (CMS-011), Patient CRUD (CMS-012), Patient search (CMS-013), Pagination (CMS-014)
**Method:** full source read of every new/modified Sprint 3 file, `dotnet build` (0 warnings, 0 errors), plus a live HTTP audit against the running API on `http://localhost:5214` with real tokens for Admin, Doctor, Patient and Receptionist.

---

## Verdict

# ⛔ CHANGES REQUIRED

Sprint 3 is **not** approvable. The build is clean and most of the happy paths work, but the audit found **three security defects and one endpoint that is dead on arrival**:

1. `GET /api/doctors` returns **500 to every caller**, including Admin. CMS-011's list endpoint does not work at all.
2. Doctor and Patient passwords are **derived from profile data** — I logged in as a doctor using `FirstName + LicenseNumber` and as a patient using `FirstName + yyyyMMdd(DateOfBirth)`. Both returned `200` with a valid token. This is explicitly forbidden by README §926 and §942.
3. Any authenticated user can read **any** patient's full record — email, phone, DOB, gender, emergency contact — through `GET /api/patients/{id}`. README §940 requires "A Patient can view only their own profile".
4. Deactivating a doctor or patient does **not** disable the linked Identity account. A deactivated doctor logged in successfully.

Also worth stating plainly: README §931 and §947 both require automated tests, and `Clinic.Tests` contained **zero test files and no project references** at review time. I have now written that suite (see §7) at your request — that part of the checklist is satisfied as of this review, the rest is not.

Rule 10 applies here in full: the code compiles, Swagger renders, and manual clicking looks fine. None of that is evidence the sprint is done.

---

## Table of contents

1. [Verdict](#verdict)
2. [What I actually did](#1-what-i-actually-did)
3. [Critical problems](#2-critical-problems)
4. [Important improvements](#3-important-improvements)
5. [Optional improvements](#4-optional-improvements)
6. [Good decisions](#5-good-decisions)
7. [Requirement traceability](#6-requirement-traceability)
8. [The test suite I wrote](#7-the-test-suite-i-wrote)
9. [Evidence log](#8-evidence-log)
10. [Action items, in order](#9-action-items-in-order)
11. [Housekeeping — test data I left behind](#10-housekeeping)

---

## 1. What I actually did

Per Rule 17 and Rule 18, this review is not a code read with opinions attached. Sequence:

- Read the Sprint 3 section of the README, including the CMS-012 checklist you relocated under the Sprint 3 hardening checklist (content unchanged — I diffed it).
- Read every new and modified file: `DoctorsController`, `PatientsController`, `SpecialtiesController`, `DoctorService`, `PatientService`, `SpecialtyService`, all nine DTO files, `specificUserRequest`, `AuthService.AddUser`, `IGenericRepository`, all three repository interfaces, `PagedResult`, `Policies`, `Roles`, `Configs.cs`, the domain models.
- `dotnet build Clinic.slnx` → **0 warnings, 0 errors**.
- Started the API and ran a scripted HTTP audit: created specialties, doctors and patients; minted tokens for four roles; attempted each cross-role access the role matrix forbids; probed validation boundaries. 18 findings confirmed over the wire, listed in §8.

Nothing in production code was modified. Only `Clinic.Tests` was touched, which you explicitly authorized.

---

## 2. Critical problems

### 🔴 2.1 `GET /api/doctors` is a guaranteed 500 — the policy name does not exist

[DoctorsController.cs:22](../Clinic/Clinic.Api/Controllers/DoctorsController.cs#L22)

```csharp
[Authorize(Policy = Policies.RequireAdmin + " or " + Policies.RequireDoctor)]
```

This is the single most important thing to understand from this sprint, because the misconception behind it will bite you again.

`Policy = "..."` is not an expression. It is a **lookup key**. At request time ASP.NET Core calls `IAuthorizationPolicyProvider.GetPolicyAsync(name)` with that exact string. Your string concatenation happens at compile time and produces the literal name `"RequireAdmin or RequireDoctor"`. Nothing ever registered a policy with that name, so the provider returns `null` and the middleware throws:

```
System.InvalidOperationException: The AuthorizationPolicy named: 'RequireAdmin or RequireDoctor' was not found.
```

Confirmed live: `GET /api/doctors` as **Admin** → `500`. As Doctor → `500`. As Patient → `500`. Every caller, every time. The endpoint has never worked.

The framework has no OR syntax in the attribute. There are three real ways to express "Admin or Doctor", and you should know all three:

1. **Register the policy** — `options.AddPolicy("RequireAdminOrDoctor", p => p.RequireRole(Roles.Admin, Roles.Doctor))`. Note that `RequireRole` with several roles is *already* an OR; that is exactly what your existing `RequireClinicStaff` does at [Configs.cs:63](../Clinic/Clinic.Api/Extensions/Configs.cs#L63).
2. **`[Authorize(Roles = "Admin,Doctor")]`** — comma-separated roles, also an OR. Works, but reintroduces magic strings, which is precisely what `Policies` and `Roles` exist to prevent.
3. Stacked `[Authorize]` attributes — which is an **AND**, not an OR. Mentioning it so you do not reach for it by mistake.

Take option 1 and add the policy to `Configs.cs` next to the others.

**Why this is critical and not merely important:** an unregistered policy is a 500, not a 403. A 500 leaks nothing useful here, but it means a core CMS-011 endpoint is entirely non-functional and your manual testing did not catch it. That is the lesson worth more than the fix.

---

### 🔴 2.2 Passwords are derived from profile data — proven by logging in

[DoctorService.cs:30](../Clinic/Clinic.Application/Services/DoctorService.cs#L30)

```csharp
Password = string.Concat(request.FirstName, request.LicenseNumber),
```

[PatientService.cs:39](../Clinic/Clinic.Application/Services/PatientService.cs#L39)

```csharp
Password = string.Concat(request.FirstName, request.DateOfBirth.ToString("yyyyMMdd")),
```

I did not reason about this. I created a doctor named Gregory with license `LIC-99001`, then posted `{"userName":"drhouse","password":"GregoryLIC-99001"}` to `/api/auth/login` and received `200` with a bearer token. Same for a patient: `John` + DOB `1990-05-14` → `John19900514` → `200`.

README §926: *"passwords must never be predictable, derived from profile data, returned, or logged."*
README §942: *"Passwords must never be predictable, derived from patient data, returned, or logged."*

Both are violated in the most direct way possible.

Think about who knows a doctor's first name and license number. License numbers are published by medical boards. A patient's first name and date of birth are on every intake form and in the response body of your own API. This is not "a weak password" — it is **no password**, because the credential is a deterministic function of data you publish.

The deeper problem is that `CreateDoctorRequest` and `CreatePatientRequest` have no password field at all, so the service *had* to invent one. That is a design gap, not a coding slip. README §942 spells out the two acceptable shapes:

> *"Creating a patient profile either creates no login account or uses an explicit, secure account-creation/invitation flow."*

So pick one:

- **No account on create.** The profile exists; login is provisioned separately. Your `Patient.UserId` is already `string?` and [Patient.cs:7-9](../Clinic/Clinic.Domain/Models/Patient.cs#L7-L9) documents exactly this case — a receptionist registers a walk-in who has clinic records but no login. The domain model already anticipated the right answer; the service ignored it.
- **Explicit invitation flow.** Create the account with a cryptographically random password that is never returned, then issue a single-use password-reset/invitation token the user redeems. `UserManager.GeneratePasswordResetTokenAsync` gives you this out of the box.

The random-password-never-returned variant is a defensible middle ground for an admin-driven clinic system. A derived password is not.

---

### 🔴 2.3 IDOR — any authenticated user can read any patient's record

[PatientsController.cs:34-42](../Clinic/Clinic.Api/Controllers/PatientsController.cs#L34-L42)

```csharp
[HttpGet("{id:guid}")]
public async Task<ActionResult<PatientResponse>> GetById(Guid id)
{
    var patient = await _patientService.GetByIdAsync(id);
    return Ok(patient);
}
```

Only the class-level `[Authorize]` applies — any valid token passes. There is no policy and, more importantly, **no ownership check**.

Confirmed live: I logged in as patient `jdoe`, requested a *different* patient's id, and got `200` with that person's full record — name, email, phone, date of birth, gender, emergency contact. A doctor token did the same.

This is IDOR (Insecure Direct Object Reference), also called broken object-level authorization. It is OWASP API Security's #1 for a reason: the URL contains an identifier, and the code trusts it because the caller is authenticated. Authentication answers *who are you*. Authorization answers *may you have this specific row*. You did the first and skipped the second.

README §940: *"A Patient can view only their own profile; patient data is never exposed to unrelated authenticated users."*
README §205: *"Patient data must not be exposed to unauthorized users."*

The rule you need is a three-branch decision inside the action or the service:

- Admin or Receptionist → allowed, any patient.
- Patient → allowed **only if** the record's `UserId` equals the caller's `NameIdentifier` claim. Otherwise `403`.
- Doctor → README §941 defers this to "an authorized clinical relationship when appointment support exists", which is Sprint 4. Until appointments exist, the honest answer is `403`, not "allow everything".

Two implementation notes that matter:

- Read the caller's id from the token (`User.FindFirstValue(ClaimTypes.NameIdentifier)`), never from a request parameter. A client-supplied "who am I" is not an identity.
- Prefer `403` over `404` here, and be consistent. Returning `404` for someone else's existing record hides existence, which is sometimes desirable; returning `403` is clearer. Pick one and apply it everywhere, because mixed behaviour is itself an information leak.

This is also the reason `PatientResponse` should not be the same shape for every audience — see §3.6.

---

### 🔴 2.4 Deactivation does not touch the linked Identity account

[DoctorService.cs:71-83](../Clinic/Clinic.Application/Services/DoctorService.cs#L71-L83), [PatientService.cs:105-125](../Clinic/Clinic.Application/Services/PatientService.cs#L105-L125)

Both `DeactivateAsync` methods flip `IsActive` on the **profile row** and stop there. The `AspNetUsers` row is untouched.

Confirmed live: I created a doctor, deactivated them via `DELETE /api/doctors/{id}` (`204`), then logged in with the derived password — `200`, valid token. Same for a patient.

So "deactivated" means "hidden from the default list query" and nothing else. The person still authenticates, still carries their role claim, and still reaches every endpoint their role allows.

README §929: *"Deactivation behavior for linked accounts and resources."*
README §946: *"Deactivation behavior is defined for any linked Identity account."*

Note the word **defined**. The README is not demanding a specific mechanism; it is demanding that you decide and implement one. Options, roughly in order of how much I'd recommend them:

- `user.LockoutEnd = DateTimeOffset.MaxValue` + `LockoutEnabled = true`. Identity's `SignInManager`/your login path already respects lockout, it is reversible, and it is the idiomatic answer.
- Bump `SecurityStamp` so already-issued tokens stop being honoured — but only if you validate the stamp, which you currently do not. Worth knowing: **your JWTs remain valid until expiry regardless**. Whatever you do to the account, a token minted a minute earlier keeps working. That is inherent to stateless JWT and the reason short lifetimes matter.
- A custom `IsActive` flag on `AppUser` checked during login. More code, same effect, less framework support.

Whichever you choose, the deactivate flow now spans two stores (clinic DB + Identity), so it belongs inside the same transaction as the profile update — which brings us to §3.1.

---

### 🔴 2.5 `Address` is silently discarded on patient creation

[PatientService.cs:46-59](../Clinic/Clinic.Application/Services/PatientService.cs#L46-L59)

`CreatePatientRequest` has `required string Address`. The entity has `Address`. The mapper never assigns it.

Confirmed live: posted `"address": "12 Elm St"`, received `201` with `"address": ""`, and a subsequent `GET` also returned `""`. The value was accepted, validated, and thrown away.

README §943: *"All supplied patient fields, including Address, are persisted and returned accurately."* The README names `Address` specifically, which suggests this is a known trap.

What makes this worse than a one-line omission: `UpdateAsync` **does** set it ([PatientService.cs:91](../Clinic/Clinic.Application/Services/PatientService.cs#L91)). Create and update disagree about the same field. Hand-written mappers drift exactly like this, which is the argument for either a single shared mapping method or a mapper library with a round-trip test. The test I wrote in `PatientMappingTests` fails today and will pass when you fix this — that is deliberate.

---

### 🔴 2.6 Patient email is written to the log

[PatientService.cs:64-66](../Clinic/Clinic.Application/Services/PatientService.cs#L64-L66)

```csharp
_logger.LogInformation(
    "Patient created. PatientId={PatientId} Email={Email}",
    patient.Id, patient.Email);
```

`UpdateAsync` logs it too. Observed in the console: `Patient created. PatientId=... Email=jdoe@clinic.local`.

README §944: *"Logs exclude unnecessary patient-sensitive data such as email, address, date of birth, and emergency contacts."*

`PatientId` is sufficient. Logs get shipped, indexed, retained for months, and read by people who have no business seeing patient contact details — that is the whole point of the rule. In a real clinic this is a regulatory issue (HIPAA in the US, GDPR in the EU), not a style preference.

The structured-logging habit itself is good and I want to be clear about that — named placeholders, no interpolation. Keep it. Just log identifiers, not personal data.

---

### 🔴 2.7 `AddUser` bypasses the role-escalation guard

[AuthService.cs](../Clinic/Clinic.Infrastructure/identity/Services/AuthService.cs) — the new `AddUser` method

```csharp
var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
```

`AssignRoleAsync` is protected by `Roles.NormalizeAssignable`, which deliberately excludes `Admin` ([Roles.cs:17](../Clinic/Clinic.Application/Common/Roles.cs#L17)) so administrators can only come from the bootstrap seeder. `AddUser` calls `AddToRoleAsync` directly with an unvalidated string, and `specificUserRequest.Role` is a plain `required string` with no validation attribute.

Today both call sites pass a `Roles.*` constant, so this is not exploitable over HTTP. I checked. But you fixed a privilege-escalation bug earlier in this project and then reintroduced the same *shape* of hole one layer down — an unguarded path to `AddToRoleAsync`. The guard belongs in the method that touches Identity, not in the discipline of its callers. Route `AddUser` through `NormalizeAssignable` and throw `ValidationException` on a miss, exactly as `AssignRoleAsync` does.

I am classifying this as critical despite not being currently exploitable, because a security invariant that depends on every future caller remembering it is not an invariant.

---

## 3. Important improvements

### 3.1 The shared account+profile workflow the README asks for does not exist

README §934: *"Shared, maintainable workflows for creating linked Identity accounts and clinic profiles, without repeating transaction and error-handling logic across services."*

Compare `DoctorService.CreateAsync` and `PatientService.CreateAsync` side by side. Identical structure: begin transaction → build `specificUserRequest` → `AddUser` → `GetUserbyEmailAsync` → build entity → add → save → commit → log, with the same bare `catch { rollback; throw; }`. The only differences are the entity type and the (broken) password rule.

Your solution to sharing was to hoist `BeginTransactionAsync`/`CommitTransactionAsync`/`RollbackTransactionAsync` onto [IGenericRepository](../Clinic/Clinic.Application/Interfaces/Repository/IGenericRepository.cs#L19-L21). That is the wrong seam, for two reasons:

- **A transaction is not a property of one repository.** It spans the DbContext. `IDoctorRepository.BeginTransactionAsync()` implies doctor-scoped transactions, which is a lie about what the code does. Every repository now advertises three methods that have nothing to do with its aggregate.
- **It did not remove the duplication.** Both services still hand-roll the same seven-step dance.

The idiomatic seam is a **unit of work** — one type that owns the transaction, injected alongside the repositories:

```csharp
public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
```

Then one shared helper — call it `IAccountProvisioningService` — that takes contact details plus a role, creates the Identity user with a secure password, and returns the user id. Both services call it inside `ExecuteInTransactionAsync`. The transaction and error handling live in exactly one place, and fixing the password rule (§2.2) becomes a one-line change instead of two.

There is a second, subtler bug hiding in the current code, and it is worth seeing: `GetUserbyEmailAsync` immediately after `AddUser` is a **redundant round-trip**. `UserManager.CreateAsync` populates `user.Id` on the instance it was handed. `AddUser` returns `void`, discards that instance, and forces the caller to go read it back from the database by email. Have `AddUser` return the created user (or its id) and both queries disappear. Also note `GetUserbyEmailAsync` throws with the message *"The authenticated user no longer exists."* — copied from a context where it made sense; here it would be baffling.

### 3.2 Validation gaps, one per DTO

| DTO | Field | Missing | Live result |
|---|---|---|---|
| `CreateDoctorRequest` | `ConsultationFee` | `[Range]` | `-500` → **201**, stored `-500.00` |
| `CreateDoctorRequest` | `Email` | `[Required]` | `[EmailAddress]` alone passes on null/empty |
| `CreateDoctorRequest` | `LicenseNumber` | `[Required]`, `[StringLength]` | no constraint at all |
| `CreateDoctorRequest` | `SpecialtyId` | non-empty Guid check | `Guid.Empty` reaches the service |
| `UpdateDoctorRequest` | `Email` | `[Required]`, `[EmailAddress]` | `"not-an-email-at-all"` → **200**, stored |
| `UpdateDoctorRequest` | `ConsultationFee` | `[Range]` | negative accepted |
| `Create/UpdatePatientRequest` | `DateOfBirth` | future-date rule | `2090-01-01` → **201** |
| `Create/UpdatePatientRequest` | `Gender` | `[StringLength(20)]` | over-long → **500** |
| `Create/UpdatePatientRequest` | `Address` | `[StringLength(200)]` | over-long → **500** |
| `Create/UpdatePatientRequest` | `EmergencyContact` | `[StringLength(100)]` | over-long → **500** |

README §927 asks for validation of *"required fields, identifiers, emails, phone numbers, and positive monetary values"*. The consultation fee is the monetary value, and negative fees are accepted — a doctor the clinic pays to see.

Three things to take from the table:

- **`[EmailAddress]` without `[Required]` is not a required email.** Every `[Validation]` attribute except `[Required]` passes on null by design. `required` in C# only means "the initializer must mention it" — the compiler enforces object construction, not request content. Model binding will happily bind `""` and satisfy `required`.
- **A missing `[StringLength]` turns bad input into a 500.** The column is `nvarchar(20)`; SQL Server rejects the insert; the exception is not an `AppException`, so your handler correctly reports 500. The DTO must mirror the column width. Every one of those 500s is a 400 in disguise.
- **Future DOB needs a custom rule.** Data annotations have no "not in the future" attribute. Either a small `[NotInFuture]` `ValidationAttribute` (reusable, declarative, shows up in Swagger) or an explicit check in the service throwing `ValidationException`. Prefer the attribute; put the invariant next to the field it constrains.

### 3.3 Pagination is done in memory — CMS-014 is not really implemented

All three services follow the same shape:

```csharp
var all = await _repository.GetAllAsync(includeInactive);   // SELECT * — every row
var filtered = SearchPatient(all, search ?? string.Empty);  // in-memory LINQ
return ToPagedResult(filtered, pageNumber, pageSize);       // in-memory Skip/Take
```

`GetAllAsync` returns `IReadOnlyList<T>`, so the query has already executed and the whole table is in memory. `Skip`/`Take` then run against a `List`. The API *looks* paginated — correct `TotalPages`, correct page window — and behaves like a full table scan. With a few hundred rows you will never notice. With 200,000 patients the first request times out.

CMS-014 is "Pagination", and the point of pagination is bounded work per request. Right now the work is unbounded and you only trim the response.

The fix is to keep it as `IQueryable` until the last moment so EF translates it to `WHERE … ORDER BY … OFFSET … FETCH NEXT`:

```csharp
Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(
    bool includeInactive, string? search, int pageNumber, int pageSize);
```

Two consequences you should anticipate:

- **`string.Contains(term, StringComparison.OrdinalIgnoreCase)` cannot be translated to SQL.** It throws at runtime when EF tries. Use `EF.Functions.Like(p.FirstName, $"%{term}%")` or plain `Contains` and let the column's collation (`SQL_Latin1_General_CP1_CI_AS` — the CI means case-insensitive) handle casing. This will surprise you the moment you move the filter into the query, so know it now.
- **`Skip`/`Take` without `OrderBy` is non-deterministic.** SQL Server makes no ordering guarantee without `ORDER BY`, so the same row can appear on page 1 and page 2. None of your three services orders anything. Add a stable sort — `OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)`.

### 3.4 `pageSize` is unbounded and invalid pages are silently corrected

Confirmed live: `?pageSize=99999` → `200`, response echoes `"pageSize": 99999`. `?page=-5` → `200` with `"pageNumber": 1`.

An unbounded `pageSize` is a denial-of-service lever: one request asks for every row, and combined with §3.3 that is the whole table materialised, mapped and serialised. Clamp it — `Math.Clamp(pageSize, 1, 100)`.

The silent coercion in `ToPagedResult` is a smaller judgement call, and I want to be fair: silently clamping is a legitimate API design choice, and the response *does* tell the caller what it used. But `page=-5` is a client bug, and quietly returning page 1 hides it. Validate at the boundary with `[Range]` on the query parameters and return `400`. Clamping belongs to values you chose; rejection belongs to values the client got wrong.

### 3.5 Doctor can be assigned to an inactive specialty

[DoctorService.cs:23](../Clinic/Clinic.Application/Services/DoctorService.cs#L23) and [:117](../Clinic/Clinic.Application/Services/DoctorService.cs#L117) both call `specialtyService.GetByIdAsync(request.SpecialtyId)`, which resolves inactive specialties happily — it has no `IsActive` filter.

Confirmed live: deactivated a specialty, then created a doctor pointing at it → `201`.

README §928: *"Correct handling of inactive related resources, such as assigning a doctor only to an active specialty."* Again the README names your exact case.

Add the check after the lookup and throw `ValidationException` (`400`) — the id exists, so `404` would be wrong, and the resource state conflicts with the operation, so `409` is arguable too. `400` with a clear message is the least surprising. Do it in both `CreateAsync` and `UpdateAsync`.

### 3.6 `DoctorResponse` omits `Email`; `PatientResponse` exposes everything to everyone

[DoctorResponse.cs](../Clinic/Clinic.Application/DTOs/Doctors/DoctorResponse.cs):

```csharp
public record DoctorResponse(
Guid Id,
// string UserName,
string FullName, string LicenseNumber, bool IsActive,
decimal ConsultationFee, string Phone, string SpecialtyName, DateTime CreatedAt);
```

README §5.2 lists Email among a doctor's fields and the entity stores it, but the response drops it — you cannot read back what you wrote. `SpecialtyId` is missing too, so a client that wants to pre-select the specialty in an edit form has only a display name to match on. And the commented-out `UserName` should be deleted; commented code in a committed file is noise that outlives the reason for it.

The opposite problem on the patient side: `PatientResponse` carries email, phone, DOB, gender, address and emergency contact, and it is the single response shape for every caller and every endpoint. Combined with §2.3 that is how one missing ownership check exposes a complete contact record. Even after you fix the authorization, consider a reduced projection for list endpoints — a receptionist scanning a patient list does not need emergency contacts on screen.

### 3.7 `PatientsController` locks out the Receptionist

Every write action plus `GetAll` is `[Authorize(Policy = Policies.RequireAdmin)]`. Confirmed live: a Receptionist token posting to `/api/patients` → `403`.

README §201-202: *"Receptionist can register a patient. / Receptionist can update patient information."*
README §939: *"Admins and Receptionists can register, update, and view patients as required by the role matrix."*

You already have the right policy — `Policies.RequireClinicStaff` is registered as Admin-or-Receptionist at [Configs.cs:63](../Clinic/Clinic.Api/Extensions/Configs.cs#L63) and is currently unused. Swap it in on `GetAll`, `Create`, `Update`. Keep deactivate/activate Admin-only unless the role matrix says otherwise — that is a lifecycle operation, not front-desk data entry.

This one stings a little because the correct policy was sitting there already.

### 3.8 `ISpecialtyService.GetAllAsync` and its implementation disagree on a default

[ISpecialtyService](../Clinic/Clinic.Application/Interfaces/Services/ISpecialtyService.cs) declares `includeInactive = false`; [SpecialtyService.cs:14](../Clinic/Clinic.Application/Services/SpecialtyService.cs#L14) declares `includeInactive = true`.

Default parameter values are **not** virtual — they are baked into the call site at compile time by the *static* type of the reference. Call through `ISpecialtyService` and you get `false`. Call through `SpecialtyService` and you get `true`. Same method, two behaviours, no warning.

Worse, `true` is the wrong default for a service that soft-deletes: omitting the parameter would return deactivated specialties. Set both to `false`, and as a general habit, declare defaults on the interface only.

### 3.9 `specificUserRequest` — naming and validation

[specificUserRequest.cs](../Clinic/Clinic.Application/DTOs/Auth/specificUserRequest.cs). Type names are PascalCase in C#; this is the only lowercase type in the solution. `CreateUserRequest` or `ProvisionAccountRequest` also says what it does, which "specific user" does not. The local variable `var SpecificUserRequest = ...` in both services has the inverse problem — locals are camelCase.

Small, but naming conventions are the cheapest form of consistency and reviewers read them as a proxy for care. Also add `[Required]` and a role-validity check to `Role`, per §2.7.

### 3.10 `PagedResult<T>` is fully mutable

[PagedResult.cs](../Clinic/Clinic.Application/Common/PagedResult.cs) — every member is `{ get; set; }` and `Items` is `List<T>`. A response DTO is a snapshot; nothing should mutate it after construction. `TotalPages` in particular is *derived* from `TotalCount` and `PageSize`, and storing it as a settable property lets the three drift apart. Make it a computed property, make the rest `init`, and expose `IReadOnlyList<T>`.

---

## 4. Optional improvements

- **`GetByLicenseNumberAsync` is on `IDoctorService` but no endpoint calls it.** Either expose `GET /api/doctors/by-license/{n}` or delete it. Unused public surface still has to be maintained and tested.
- **The commented-out `// Task<Specialty?> GetByNameAsync(string name);` in `IDoctorRepository`** — delete it.
- **Formatting.** Both new services have inconsistent indentation inside the `try` blocks (the `var SpecificUserRequest` block sits at a different level than its siblings). Run `dotnet format`. This has no functional impact and I mention it only because it is one command.
- **`DoctorService.ToPagedResult` maps through `d.Specialty?.Name ?? string.Empty`** while `GetByIdAsync` maps through a separate service call. Two paths to the same field mean a list can show `""` where the detail view shows `"Cardiology"`, depending on whether the repository happened to `Include` the navigation. One mapping path.
- **`GetById` on `DoctorsController` has no policy**, so any authenticated user — including a Patient — can read any doctor's record. Doctor records are far less sensitive than patient records and arguably should be broadly readable, so I am not calling this a defect. But it should be a *decision* in `Configs.cs`, not an omission.

---

## 5. Good decisions

Rule 7 cuts both ways, and there is real craft in this sprint:

- **Transactional creation actually works.** I tested the rollback path deliberately: an over-long `Gender` fails the insert *after* the Identity user is created, and I confirmed the orphan account does not survive — a login attempt with that user returned `401`. Both services get this right. Wrapping a two-store operation in a transaction is the kind of thing juniors usually skip, and you did it in both places.
- **Duplicate license → `409`, not a 500 from the unique index.** Checked before insert, with the index still in place as the real guarantee. `SpecialtyService.CreateAsync` even carries a comment acknowledging the check can lose a race and that the database must win it. That is the right mental model.
- **Idempotent deactivate/activate.** Already-inactive → log and return, not a conflict. A retried request must not fail differently. Correct, and the comments explain why.
- **`NameExistsAsync(name, excludeId: id)`** so a specialty cannot conflict with itself on rename. Easy to miss, and you handled it.
- **Structured logging throughout** — message templates with named placeholders, never interpolation. The only problem is *what* you log (§2.6), not how.
- **`Policies` and `Roles` as constants** instead of magic strings. The irony of §2.1 is that it was a misuse of a genuinely good abstraction.
- **`NormalizeName` trimming** so `" Cardiology"` and `"Cardiology"` cannot coexist.
- **Clean build.** 0 warnings, 0 errors, with `Nullable` enabled across every project. Nullable-clean at zero warnings takes discipline.
- **`DateOnly` for `DateOfBirth`**, with a comment explaining that a birth date has no time or time zone. Right type for the concept.

---

## 6. Requirement traceability

### CMS-010 — Specialty CRUD

| Requirement | Status |
|---|---|
| Admin can create / update / deactivate | ✅ |
| Names not duplicated (DB index + 409) | ✅ |
| Soft delete, idempotent | ✅ |
| `includeInactive` default consistent | ❌ §3.8 |

### CMS-011 — Doctor CRUD

| Requirement | Status |
|---|---|
| Admin can create / update / deactivate | ✅ |
| Admin can **view** doctors (README §151) | ❌ **500 for all callers** §2.1 |
| Doctors belong to a specialty | ⚠️ but inactive specialties allowed §3.5 |
| License number unique → 409 | ✅ |
| Email in response (README §5.2) | ❌ §3.6 |
| Positive consultation fee (README §927) | ❌ negative accepted §3.2 |

### CMS-012 — Patient CRUD

| Checklist item (README §939-948) | Status |
|---|---|
| Admins **and Receptionists** can register/update/view | ❌ Receptionist → 403 §3.7 |
| Patient views only their own profile | ❌ IDOR §2.3 |
| Doctor access limited to clinical relationship | ❌ doctor reads any patient §2.3 |
| No predictable/derived passwords | ❌ **proven by login** §2.2 |
| All fields incl. `Address` persisted | ❌ dropped on create §2.5 |
| Logs exclude sensitive data | ❌ email logged §2.6 |
| DOB cannot be in the future | ❌ `2090-01-01` → 201 §3.2 |
| Textual inputs trimmed | ⚠️ update trims, create does not |
| Deactivation defined for Identity account | ❌ §2.4 |
| Tests cover authz / self-access / validation | ⚠️ written in this review, §7 |
| Committed, clean working tree | ❌ §9 |

### CMS-013 — Patient search

| Requirement | Status |
|---|---|
| Authorized users can search | ⚠️ works, but Admin-only §3.7 |
| Search by name | ✅ first/last name |
| Runs in the database | ❌ in-memory §3.3 |

### CMS-014 — Pagination

| Requirement | Status |
|---|---|
| Pagination works (shape, counts) | ✅ |
| Bounded work per request | ❌ full table load §3.3 |
| `pageSize` capped | ❌ 99999 accepted §3.4 |
| Deterministic ordering | ❌ no `OrderBy` §3.3 |

### Sprint 3 acceptance criteria (README §916-919)

| Criterion | Status |
|---|---|
| Authorized users can manage required resources | ❌ doctors list 500; Receptionist locked out |
| Validation works | ❌ ten gaps, three of which produce 500s |
| Pagination works | ⚠️ correct output, wrong implementation |
| Duplicate constraints enforced | ✅ |

---

## 7. The test suite I wrote

You authorized test code, so this is the one part of the repo I changed. 
`Clinic.Tests` had **no `<ProjectReference>` and no `.cs` files** — it could not have referenced your code even if tests existed.

Added to `Clinic.Tests.csproj`: project references to `Clinic.Application` and `Clinic.Domain`, plus `Moq` for the repository/service fakes and `FluentAssertions` for readable assertions.

The suite is unit-level and deliberately hits the seams the README names — business rules, authorization-relevant behaviour, validation, and account/profile creation:

| File | Covers |
|---|---|
| `TestDoubles/FakeRepositories.cs` | in-memory `ISpecialtyRepository`/`IDoctorRepository`/`IPatientRepository` with real transaction bookkeeping, so rollback is observable |
| `SpecialtyServiceTests.cs` | duplicate name → `ConflictException`; whitespace name → `ValidationException`; trimming; self-rename via `excludeId`; idempotent deactivate; `includeInactive` filtering; the §3.8 default mismatch |
| `DoctorServiceTests.cs` | duplicate license → `ConflictException`; unknown specialty → `NotFoundException`; **inactive specialty rejected** (fails today, §3.5); **negative fee rejected** (fails today, §3.2); **password not derived from profile data** (fails today, §2.2); rollback leaves no orphan account |
| `PatientServiceTests.cs` | **`Address` persisted on create** (fails today, §2.5); **future DOB rejected** (fails today, §3.2); **email not logged** (fails today, §2.6); trimming; idempotent activate/deactivate; rollback |
| `PaginationTests.cs` | page window correctness; `TotalPages` arithmetic; `page<1` and `pageSize<1` coercion; **`pageSize` cap** (fails today, §3.4) |
| `RolesTests.cs` | `NormalizeAssignable("Admin")` → `null` (the escalation guard); case-insensitive canonicalisation; `All` vs `Assignable` |
| `ValidationAttributeTests.cs` | data-annotation coverage per DTO, driven by `Validator.TryValidateObject` — documents every gap in the §3.2 table as a test |

**Read this before you run them:** several tests are written to describe the behaviour the README requires, not the behaviour the code currently has, so **they fail on purpose**. Each one carries a comment naming the section of this report it pins down. A failing test that names a real defect is more valuable than a passing test that documents a bug, and this is the normal way to work: red test → fix → green. Do not "fix" them by changing the assertion.

Run them with:

```
dotnet test Clinic/Clinic.Tests/Clinic.Tests.csproj
```

Not covered, and you should know the gap: **endpoint-level authorization**. The IDOR in §2.3 and the broken policy in §2.1 live in the controller and the DI container, and no unit test can reach them — I caught both over HTTP. Testing those properly needs `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory` for in-process integration tests. That is the natural next step once the critical fixes land, and it is what would have caught §2.1 before I did.

---

## 8. Evidence log

Every finding below was observed over HTTP against the running API, not inferred from reading code.

| # | Probe | Result | Section |
|---|---|---|---|
| 1 | `GET /api/doctors` as Admin, Doctor, Patient | **500** — `AuthorizationPolicy named: 'RequireAdmin or RequireDoctor' was not found` | §2.1 |
| 2 | Login `drhouse` / `GregoryLIC-99001` | **200** + token | §2.2 |
| 3 | Login `jdoe` / `John19900514` | **200** + token | §2.2 |
| 4 | Patient `jdoe` → `GET /api/patients/{other-id}` | **200**, full record incl. email, phone, DOB, gender, emergency contact | §2.3 |
| 5 | Doctor token → `GET /api/patients/{any-id}` | **200** | §2.3 |
| 6 | `POST /api/patients` with `"address":"12 Elm St"` | **201**, `"address":""`; `GET` also `""` | §2.5 |
| 7 | Console log after patient create | `Patient created. PatientId=… Email=jdoe@clinic.local` | §2.6 |
| 8 | Receptionist → `POST /api/patients` | **403** | §3.7 |
| 9 | `"dateOfBirth":"2090-01-01"` | **201** | §3.2 |
| 10 | `"consultationFee":-500` | **201**, returned `-500.00` | §3.2 |
| 11 | Create doctor with a deactivated `specialtyId` | **201** | §3.5 |
| 12 | Deactivate doctor → login with derived password | **200** | §2.4 |
| 13 | Deactivate patient → login | **200** | §2.4 |
| 14 | `PUT /api/doctors/{id}` `"email":"not-an-email-at-all"` | **200**, stored | §3.2 |
| 15 | `DoctorResponse` payload inspection | no `email`, no `specialtyId` | §3.6 |
| 16 | `?pageSize=99999` | **200**, echoed `"pageSize":99999` | §3.4 |
| 17 | `?page=-5` | **200**, `"pageNumber":1`, no 400 | §3.4 |
| 18 | Over-long `gender` (> 20 chars) | **500**; rollback verified — login → 401, no orphan account | §3.2, §5 |

Verified working: `dotnet build` 0/0; transactional rollback on both services; duplicate license → 409; duplicate specialty name → 409; `Guid.Empty` specialty → 404; Patient → `GET /api/patients` → 403; all CMS-010 specialty behaviour from the previous review still correct.

---

## 9. Action items, in order

### 🔴 Must fix before Sprint 3 can be approved

1. **Register an Admin-or-Doctor policy** in `Configs.cs` and reference it by name. `GET /api/doctors` is currently 500 for everyone. §2.1
2. **Stop deriving passwords.** Choose "no account on create" or an invitation flow with a random, never-returned password. §2.2
3. **Add object-level authorization to `GET /api/patients/{id}`** — Admin/Receptionist any, Patient own record only (compare `UserId` to the `NameIdentifier` claim), Doctor `403` until appointments exist. §2.3
4. **Define deactivation for linked Identity accounts** — lockout is the idiomatic choice — inside the same transaction as the profile change. §2.4
5. **Assign `Address` in `PatientService.CreateAsync`.** §2.5
6. **Remove `Email` (and any other patient PII) from log messages.** §2.6
7. **Route `AddUser` through `Roles.NormalizeAssignable`.** §2.7
8. **Give the Receptionist their endpoints back** with the already-registered `RequireClinicStaff`. §3.7
9. **Close the validation gaps** in the §3.2 table, especially `[Range(0.01, …)]` on `ConsultationFee`, `[Required]`+`[EmailAddress]` on both email fields, `[StringLength]` matching the column widths, and a future-date rule for DOB.
10. **Reject inactive specialties** in doctor create and update. §3.5

### 🟡 Should fix in this sprint

11. Push pagination and search into the database; add a stable `OrderBy`; watch for the untranslatable `StringComparison` overload. §3.3
12. Cap `pageSize`; `400` on invalid `page`. §3.4
13. Introduce a unit of work plus a shared account-provisioning helper; move the transaction methods off `IGenericRepository`. §3.1
14. Have `AddUser` return the created user so the `GetUserbyEmailAsync` round-trip disappears; fix its misleading exception message. §3.1
15. Add `Email` and `SpecialtyId` to `DoctorResponse`; consider a reduced patient projection for lists. §3.6
16. Align the `includeInactive` defaults on `ISpecialtyService`. §3.8
17. Rename `specificUserRequest` → `CreateUserRequest`; fix the local variable casing; validate `Role`. §3.9
18. Make `PagedResult<T>` immutable with a computed `TotalPages`. §3.10

### 🟢 Carried forward from the previous review — still outstanding

19. **Nothing is committed and there is no git remote.** README §932 requires *"meaningful commits, and a clean working tree"*, and §26's Definition of Done requires the work to be pushed. `git status` shows 30+ modified files and a pile of untracked ones. All of Sprint 3 exists only on your disk — a disk failure today costs you the entire sprint. This is a hard blocker on the checklist, independent of the code.
20. **`BootstrapAdmin:Password` is still `ChangeMe1Admin`** in configuration.
21. **Four self-promoted accounts remain in the dev database** — `Admin123`, `Mohamed1232`, `Mohamed1233` hold `Admin`; `Mohamed1235` holds `Receptionist` instead of `Patient`. Artifacts of the escalation bug you fixed. Clean them up so your dev data matches your intended role model.
22. **Add integration tests** with `WebApplicationFactory` once the critical fixes land. §7

---

## 10. Housekeeping

My audit created test data in the `Clinic` dev database. Delete it when convenient:

- **Users:** `drhouse`, `drneg`, `drretired`, `jdoe`, `victim`, `future1`, `frontdesk`, `roletest`
- **Specialties:** `Neurology` (inactive), `Retired Dept` (inactive)
- **Role grants:** `frontdesk` → `Receptionist`; `pat1` → `Doctor` + `Receptionist` (from the earlier CMS-010 verification)

The background API process on port 5214 has been stopped.

---

## Closing note

I want to separate two things, because conflating them would give you the wrong lesson.

The **engineering fundamentals in this sprint are good**. Transactional two-store creation with a verified rollback path, idempotent soft deletes, pre-checked uniqueness backed by a real index, structured logging, a nullable-clean build at zero warnings, and comments that explain *why* rather than *what*. That is not junior-level instinct, and it is the part that is hard to teach.

What went wrong is a different skill: **verifying against the requirement instead of against the happy path**. Every critical finding in §2 would have been caught by one deliberate attempt to do the wrong thing — call the endpoint as the wrong role, log in with a guessed password, read someone else's record, read back the field you just wrote. You built the features and then confirmed they work; you did not try to break them.

That is the habit to build now, and it is exactly why the checklist in §931 asks for automated tests. A test suite is that adversarial instinct, written down once so it runs forever. The suite in §7 is the starting point — including the tests that fail today.

**Sprint 3: CHANGES REQUIRED.** Work through §9's critical list, then tell me you have finished and I will re-review.
