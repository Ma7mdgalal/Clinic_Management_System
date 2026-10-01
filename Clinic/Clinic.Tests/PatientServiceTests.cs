using Clinic.Application.Common;
using Clinic.Application.Common.Exceptions;
using Clinic.Application.Services;
using Clinic.Tests.TestDoubles;

namespace Clinic.Tests;

/// <summary>
/// CMS-012 and CMS-013. The checklist at README 937-948 is the specification here:
/// every field persisted, no patient PII in logs, no future date of birth, textual inputs
/// trimmed, and no predictable password.
/// </summary>
public class PatientServiceTests
{
    private readonly FakeTransaction _transaction = new();
    private readonly FakePatientRepository _patients;
    private readonly FakeAuthService _auth;
    private readonly RecordingLogger<PatientService> _logger = new();
    private readonly PatientService _sut;

    public PatientServiceTests()
    {
        _patients = new FakePatientRepository(_transaction);
        _auth = new FakeAuthService(_transaction);
        _sut = new PatientService(_patients, _logger, _auth);
    }

    // ---------------------------------------------------------------- create

    [Fact]
    public async Task CreateAsync_StoresPatientAndCreatesOneAccount()
    {
        var response = await _sut.CreateAsync(Build.CreatePatient());

        Assert.Single(_patients.Rows);
        Assert.Single(_auth.Accounts);
        Assert.Equal("John", response.FirstName);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task CreateAsync_GrantsThePatientRole_NeverAdmin()
    {
        // Self-service and staff-created accounts alike must never produce a privileged
        // account. Roles.Admin is deliberately outside Roles.Assignable.
        await _sut.CreateAsync(Build.CreatePatient());

        Assert.Equal(Roles.Patient, _auth.Accounts[0].Role);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 2.5.
    ///
    /// README 943: "All supplied patient fields, including Address, are persisted and
    /// returned accurately." CreatePatientRequest has a required Address and the entity has
    /// the column, but PatientService.CreateAsync never assigns it. Verified live: posted
    /// "12 Elm St", got back "" and a subsequent GET also returned "".
    ///
    /// UpdateAsync does set Address, so create and update disagree about the same field —
    /// the classic failure mode of hand-written mappers.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PersistsAddress()
    {
        var response = await _sut.CreateAsync(Build.CreatePatient(address: "12 Elm St"));

        Assert.Equal("12 Elm St", response.Address);
        Assert.Equal("12 Elm St", _patients.Rows[0].Address);
    }

    /// <summary>
    /// A round-trip check over every field, so the next missing assignment is caught by a
    /// test rather than by a patient noticing their address is blank. Fails today for the
    /// same reason as above.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PersistsEverySuppliedField()
    {
        var request = Build.CreatePatient(
            firstName: "Alice",
            lastName: "Brown",
            email: "alice@clinic.local",
            dateOfBirth: new DateOnly(1985, 3, 2),
            gender: "Female",
            address: "5 Nile Ave",
            emergencyContact: "+201222222222");

        var response = await _sut.CreateAsync(request);

        Assert.Equal(request.FirstName, response.FirstName);
        Assert.Equal(request.LastName, response.LastName);
        Assert.Equal(request.Email, response.Email);
        Assert.Equal(request.Phone, response.Phone);
        Assert.Equal(request.DateOfBirth, response.DateOfBirth);
        Assert.Equal(request.Gender, response.Gender);
        Assert.Equal(request.Address, response.Address);
        Assert.Equal(request.EmergencyContact, response.EmergencyContact);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.2.
    ///
    /// README 945: "Date of birth is valid and cannot be in the future." Neither DTO has a
    /// future-date rule and the service does not check, so a birth date in 2090 is accepted.
    /// Verified live: 201 Created.
    ///
    /// Data annotations have no built-in "not in the future" attribute. Write a small
    /// [NotInFuture] ValidationAttribute — declarative, reusable, and visible in Swagger —
    /// and keep a service-level check for non-HTTP callers.
    /// </summary>
    [Fact]
    public async Task CreateAsync_FutureDateOfBirth_IsRejected()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(Build.CreatePatient(dateOfBirth: tomorrow)));

        Assert.Empty(_patients.Rows);
    }

    [Fact]
    public async Task CreateAsync_TodayAsDateOfBirth_IsAccepted()
    {
        // A newborn registered on the day of birth is legitimate. The rule is "not in the
        // future", not "must be in the past" — an off-by-one here rejects real patients.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = await _sut.CreateAsync(Build.CreatePatient(dateOfBirth: today));

        Assert.Equal(today, response.DateOfBirth);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 2.5 / 945.
    ///
    /// UpdateAsync trims every textual field; CreateAsync trims none. README 945 asks for
    /// trimmed textual inputs, and untrimmed names break search and produce visually
    /// duplicate records.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TrimsTextualInputs()
    {
        var response = await _sut.CreateAsync(Build.CreatePatient(
            firstName: "  John  ",
            lastName: "  Doe  ",
            address: "  12 Elm St  ",
            emergencyContact: "  +201111111111  "));

        Assert.Equal("John", response.FirstName);
        Assert.Equal("Doe", response.LastName);
        Assert.Equal("12 Elm St", response.Address);
        Assert.Equal("+201111111111", response.EmergencyContact);
    }

    // ------------------------------------------------- account creation security

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 2.2.
    ///
    /// README 942: "Passwords must never be predictable, derived from patient data,
    /// returned, or logged."
    ///
    /// PatientService builds the password as FirstName + DateOfBirth in yyyyMMdd. Verified
    /// live: logged in as the created patient with "John19900514" and received 200 with a
    /// valid token. A first name and a date of birth are on every intake form — and in the
    /// response body of this very API.
    ///
    /// Note Patient.UserId is nullable by design, documented for the walk-in case: the
    /// domain model already allows "profile with no login", which is the simplest fix.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PasswordIsNotDerivedFromPatientData()
    {
        var request = Build.CreatePatient(
            firstName: "John", lastName: "Doe", dateOfBirth: new DateOnly(1990, 5, 14));

        await _sut.CreateAsync(request);

        var password = _auth.Accounts[0].Password;

        Assert.DoesNotContain(request.FirstName, password, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(request.LastName, password, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(request.UserName, password, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("19900514", password, StringComparison.Ordinal);
        Assert.DoesNotContain("1990", password, StringComparison.Ordinal);
    }

    /// <summary>FAILS TODAY — Sprint 3 review 2.2.</summary>
    [Fact]
    public async Task CreateAsync_TwoPatients_GetUnrelatedPasswords()
    {
        await _sut.CreateAsync(Build.CreatePatient(userName: "jdoe1", email: "j1@clinic.local"));
        await _sut.CreateAsync(Build.CreatePatient(userName: "jdoe2", email: "j2@clinic.local"));

        Assert.NotEqual(_auth.Accounts[0].Password, _auth.Accounts[1].Password);
    }

    [Fact]
    public async Task CreateAsync_PasswordIsNeverReturned()
    {
        var response = await _sut.CreateAsync(Build.CreatePatient());

        Assert.DoesNotContain("password", response.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- logging / PII

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 2.6.
    ///
    /// README 944: "Logs exclude unnecessary patient-sensitive data such as email, address,
    /// date of birth, and emergency contacts." PatientService logs Email on both create and
    /// update. Observed in the console: "Patient created. PatientId=... Email=jdoe@clinic.local".
    ///
    /// PatientId alone identifies the record for support purposes. Logs are shipped,
    /// indexed and retained for months, and read by people with no business seeing patient
    /// contact details — which is a regulatory matter, not a style preference.
    /// </summary>
    [Fact]
    public async Task CreateAsync_DoesNotLogPatientSensitiveData()
    {
        var request = Build.CreatePatient(
            email: "jdoe@clinic.local",
            address: "12 Elm St",
            emergencyContact: "+201111111111",
            dateOfBirth: new DateOnly(1990, 5, 14));

        await _sut.CreateAsync(request);

        var logged = _logger.AllText;

        Assert.DoesNotContain(request.Email, logged, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(request.Address, logged, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(request.EmergencyContact, logged, StringComparison.Ordinal);
        Assert.DoesNotContain("1990-05-14", logged, StringComparison.Ordinal);
    }

    /// <summary>FAILS TODAY — Sprint 3 review 2.6. UpdateAsync logs Email as well.</summary>
    [Fact]
    public async Task UpdateAsync_DoesNotLogPatientSensitiveData()
    {
        var patient = Build.Patient();
        _patients.Rows.Add(patient);

        await _sut.UpdateAsync(patient.Id, Build.UpdatePatient(email: "jdoe@clinic.local"));

        Assert.DoesNotContain("jdoe@clinic.local", _logger.AllText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_StillLogsThePatientId()
    {
        // The fix for the two tests above must not be "log nothing". An identifier is
        // needed for support and audit; only the personal data has to go.
        var response = await _sut.CreateAsync(Build.CreatePatient());

        Assert.Contains(response.Id.ToString(), _logger.AllText, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- transaction

    [Fact]
    public async Task CreateAsync_WhenTheProfileInsertFails_RollsBackTheAccount()
    {
        _patients.SaveChangesThrows = new InvalidOperationException("constraint violation");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(Build.CreatePatient()));

        Assert.Equal(1, _transaction.RollbackCount);
        Assert.Equal(0, _transaction.CommitCount);
        Assert.Empty(_auth.Accounts);
        Assert.Empty(_patients.Rows);
    }

    [Fact]
    public async Task CreateAsync_WhenAccountCreationFails_RollsBackAndAddsNoPatient()
    {
        _auth.AddUserThrows = new ConflictException("Email already taken.");

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(Build.CreatePatient()));

        Assert.Equal(1, _transaction.RollbackCount);
        Assert.Empty(_patients.Rows);
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_CommitsExactlyOnce()
    {
        await _sut.CreateAsync(Build.CreatePatient());

        Assert.Equal(1, _transaction.BeginCount);
        Assert.Equal(1, _transaction.CommitCount);
        Assert.Equal(0, _transaction.RollbackCount);
    }

    // ---------------------------------------------------------------- read

    [Fact]
    public async Task GetByIdAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTheStoredRecord()
    {
        var patient = Build.Patient();
        _patients.Rows.Add(patient);

        var response = await _sut.GetByIdAsync(patient.Id);

        Assert.Equal(patient.Id, response.Id);
        Assert.Equal(patient.FirstName, response.FirstName);
    }

    /// <summary>
    /// Documents the IDOR in Sprint 3 review 2.3 — and why a unit test cannot catch it.
    ///
    /// README 940: "A Patient can view only their own profile; patient data is never
    /// exposed to unrelated authenticated users." Verified live: patient jdoe requested a
    /// different patient's id and received 200 with that person's email, phone, date of
    /// birth, gender and emergency contact.
    ///
    /// The service takes only a Guid — it has no idea who is asking, so it cannot enforce
    /// ownership and this test cannot assert it. That is the design point: object-level
    /// authorization needs the caller's identity, which lives in the controller (the
    /// NameIdentifier claim) or in a caller-context abstraction passed into the service.
    ///
    /// This test therefore records the current behaviour and stands as a marker. Replace it
    /// with a WebApplicationFactory integration test — or add a caller-context parameter
    /// and assert a real 403 here.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_HasNoOwnershipCheck_AuthorizationMustLiveInTheApiLayer()
    {
        var victim = Build.Patient("Victim", "Person");
        _patients.Rows.Add(victim);

        // Any caller reaching the service gets the full record, including contact data.
        var response = await _sut.GetByIdAsync(victim.Id);

        Assert.Equal(victim.Email, response.Email);
        Assert.Equal(victim.EmergencyContact, response.EmergencyContact);

        // The signature is the evidence: no caller identity, so no ownership decision.
        var parameters = typeof(PatientService)
            .GetMethod(nameof(PatientService.GetByIdAsync))!
            .GetParameters();

        Assert.Single(parameters);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesInactiveByDefault()
    {
        _patients.Rows.Add(Build.Patient("John", "Doe"));
        _patients.Rows.Add(Build.Patient("Gone", "Away", isActive: false));

        var result = await _sut.GetAllAsync();

        Assert.Single(result.Items);
        Assert.Equal("John", result.Items[0].FirstName);
    }

    [Fact]
    public async Task GetAllAsync_IncludesInactive_WhenAsked()
    {
        _patients.Rows.Add(Build.Patient("John", "Doe"));
        _patients.Rows.Add(Build.Patient("Gone", "Away", isActive: false));

        var result = await _sut.GetAllAsync(includeInactive: true);

        Assert.Equal(2, result.TotalCount);
    }

    // CMS-013 — patient search
    [Theory]
    [InlineData("john", 1)]
    [InlineData("JOHN", 1)]
    [InlineData("doe", 1)]
    [InlineData("DOE", 1)]
    [InlineData("zzz", 0)]
    public async Task GetAllAsync_SearchIsCaseInsensitiveAcrossBothNames(string term, int expected)
    {
        _patients.Rows.Add(Build.Patient("John", "Doe"));
        _patients.Rows.Add(Build.Patient("Alice", "Brown"));

        var result = await _sut.GetAllAsync(search: term);

        Assert.Equal(expected, result.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyOrWhitespaceSearch_ReturnsEverything()
    {
        _patients.Rows.Add(Build.Patient("John", "Doe"));
        _patients.Rows.Add(Build.Patient("Alice", "Brown"));

        Assert.Equal(2, (await _sut.GetAllAsync(search: null)).TotalCount);
        Assert.Equal(2, (await _sut.GetAllAsync(search: "")).TotalCount);
        Assert.Equal(2, (await _sut.GetAllAsync(search: "   ")).TotalCount);
    }

    // ---------------------------------------------------------------- update

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), Build.UpdatePatient()));
    }

    [Fact]
    public async Task UpdateAsync_PersistsEverySuppliedField()
    {
        var patient = Build.Patient();
        _patients.Rows.Add(patient);

        var response = await _sut.UpdateAsync(patient.Id, Build.UpdatePatient(
            firstName: "Jonathan",
            lastName: "Doherty",
            email: "jonathan@clinic.local",
            gender: "Male",
            address: "9 Tahrir St",
            emergencyContact: "+201333333333"));

        Assert.Equal("Jonathan", response.FirstName);
        Assert.Equal("Doherty", response.LastName);
        Assert.Equal("jonathan@clinic.local", response.Email);
        Assert.Equal("9 Tahrir St", response.Address);
        Assert.Equal("+201333333333", response.EmergencyContact);
    }

    [Fact]
    public async Task UpdateAsync_TrimsTextualInputs()
    {
        var patient = Build.Patient();
        _patients.Rows.Add(patient);

        var response = await _sut.UpdateAsync(
            patient.Id, Build.UpdatePatient(firstName: "  Jonathan  ", address: "  9 Tahrir St  "));

        Assert.Equal("Jonathan", response.FirstName);
        Assert.Equal("9 Tahrir St", response.Address);
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. Same future-date rule on the update path.</summary>
    [Fact]
    public async Task UpdateAsync_FutureDateOfBirth_IsRejected()
    {
        var patient = Build.Patient();
        _patients.Rows.Add(patient);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.UpdateAsync(patient.Id, Build.UpdatePatient(dateOfBirth: tomorrow)));
    }

    // ---------------------------------------------------------------- lifecycle

    [Fact]
    public async Task DeactivateAsync_SoftDeletes_TheRowSurvives()
    {
        var patient = Build.Patient();
        _patients.Rows.Add(patient);

        await _sut.DeactivateAsync(patient.Id);

        Assert.Single(_patients.Rows);
        Assert.False(patient.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_AlreadyInactive_IsIdempotent()
    {
        var patient = Build.Patient(isActive: false);
        _patients.Rows.Add(patient);

        await _sut.DeactivateAsync(patient.Id);

        Assert.Equal(0, _patients.SaveChangesCallCount);
    }

    [Fact]
    public async Task ActivateAsync_ReactivatesAnInactivePatient()
    {
        var patient = Build.Patient(isActive: false);
        _patients.Rows.Add(patient);

        await _sut.ActivateAsync(patient.Id);

        Assert.True(patient.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_AlreadyActive_IsIdempotent()
    {
        var patient = Build.Patient();
        _patients.Rows.Add(patient);

        await _sut.ActivateAsync(patient.Id);

        Assert.Equal(0, _patients.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeactivateAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeactivateAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ActivateAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ActivateAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Documents Sprint 3 review 2.4, and why this too needs an integration test.
    ///
    /// README 946: "Deactivation behavior is defined for any linked Identity account."
    /// DeactivateAsync flips IsActive on the profile row and never touches AspNetUsers.
    /// Verified live: a deactivated patient logged in successfully and received a valid
    /// token, so "deactivated" currently means only "hidden from the default list query".
    ///
    /// PatientService is constructed with IAuthService, so it could disable the account —
    /// but IAuthService has no method for it. That missing method is the finding. Once
    /// something like DisableAccountAsync(userId) exists, replace this with an assertion
    /// that deactivating the profile disables the account inside the same transaction.
    /// </summary>
    [Fact]
    public void IAuthService_HasNoWayToDisableAnAccount()
    {
        var methods = typeof(Application.Interfaces.Services.IAuthService)
            .GetMethods()
            .Select(m => m.Name)
            .ToList();

        Assert.DoesNotContain("DisableAccountAsync", methods);
        Assert.DoesNotContain("LockAccountAsync", methods);
    }
}
