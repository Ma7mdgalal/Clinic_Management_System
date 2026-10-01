using Clinic.Application.Common;
using Clinic.Application.Common.Exceptions;
using Clinic.Application.Services;
using Clinic.Tests.TestDoubles;
using Xunit;

namespace Clinic.Tests;

/// <summary>
/// CMS-011 business rules plus the account-creation behaviour the Sprint 3 hardening
/// checklist calls out (README 926-928).
///
/// The real <see cref="SpecialtyService"/> is used rather than a stub, because the rule
/// under test — "assign a doctor only to an active specialty" — is about how the two
/// services agree on specialty state.
/// </summary>
public class DoctorServiceTests
{
    private readonly FakeTransaction _transaction = new();
    private readonly FakeDoctorRepository _doctors;
    private readonly FakeSpecialtyRepository _specialties;
    private readonly FakeAuthService _auth;
    private readonly RecordingLogger<DoctorService> _logger = new();
    private readonly DoctorService _sut;

    public DoctorServiceTests()
    {
        // One shared transaction across the repository and the auth fake, so a rollback
        // can undo the Identity account as well as the profile row.
        _doctors = new FakeDoctorRepository(_transaction);
        _specialties = new FakeSpecialtyRepository(_transaction);
        _auth = new FakeAuthService(_transaction);

        var specialtyService = new SpecialtyService(
            _specialties, new RecordingLogger<SpecialtyService>());

        _sut = new DoctorService(_doctors, _logger, _auth, specialtyService);
    }

    private Guid SeedSpecialty(bool isActive = true, string name = "Cardiology")
    {
        var specialty = Build.Specialty(name, isActive);
        _specialties.Rows.Add(specialty);
        return specialty.Id;
    }

    // ---------------------------------------------------------------- create

    [Fact]
    public async Task CreateAsync_StoresDoctorAndCreatesOneAccount()
    {
        var specialtyId = SeedSpecialty();

        var response = await _sut.CreateAsync(Build.CreateDoctor(specialtyId));

        Assert.Single(_doctors.Rows);
        Assert.Single(_auth.Accounts);
        Assert.Equal("Gregory House", response.FullName);
        Assert.Equal("Cardiology", response.SpecialtyName);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task CreateAsync_GrantsTheDoctorRole_NeverAdmin()
    {
        var specialtyId = SeedSpecialty();

        await _sut.CreateAsync(Build.CreateDoctor(specialtyId));

        Assert.Equal(Roles.Doctor, _auth.Accounts[0].Role);
    }

    [Fact]
    public async Task CreateAsync_DuplicateLicenseNumber_ThrowsConflict()
    {
        // README 5.2 — "License Number must be unique". Checked before insert so the caller
        // gets 409 rather than a 500 bubbling out of the unique index.
        var specialtyId = SeedSpecialty();
        _doctors.Rows.Add(Build.Doctor(licenseNumber: "LIC-0001"));

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(Build.CreateDoctor(specialtyId, licenseNumber: "LIC-0001")));

        Assert.Single(_doctors.Rows);
        Assert.Empty(_auth.Accounts);
    }

    [Fact]
    public async Task CreateAsync_DuplicateLicenseNumber_CreatesNoAccount()
    {
        // The conflict is detected before the transaction opens, so no Identity account
        // should exist afterwards. A create that half-succeeds is worse than one that fails.
        var specialtyId = SeedSpecialty();
        _doctors.Rows.Add(Build.Doctor(licenseNumber: "LIC-0001"));

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(Build.CreateDoctor(specialtyId, licenseNumber: "LIC-0001")));

        Assert.Empty(_auth.Accounts);
        Assert.Equal(0, _transaction.BeginCount);
    }

    [Fact]
    public async Task CreateAsync_UnknownSpecialty_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateAsync(Build.CreateDoctor(Guid.NewGuid())));

        Assert.Empty(_doctors.Rows);
        Assert.Empty(_auth.Accounts);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.5.
    ///
    /// README 928: "Correct handling of inactive related resources, such as assigning a
    /// doctor only to an active specialty." DoctorService resolves the specialty through
    /// GetByIdAsync, which has no IsActive filter, so a deactivated specialty is accepted.
    /// Verified live: 201 Created.
    ///
    /// Fix by checking specialty.IsActive after the lookup and throwing ValidationException
    /// (400 — the id exists, so 404 would be wrong). Apply it in UpdateAsync too.
    /// </summary>
    [Fact]
    public async Task CreateAsync_InactiveSpecialty_IsRejected()
    {
        var specialtyId = SeedSpecialty(isActive: false, name: "Retired Dept");

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(Build.CreateDoctor(specialtyId)));

        Assert.Empty(_doctors.Rows);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.2.
    ///
    /// README 927 requires validation of "positive monetary values". CreateDoctorRequest
    /// has no [Range] on ConsultationFee and the service does not check it, so the API
    /// accepts a negative fee. Verified live: -500 → 201 Created, stored as -500.00.
    ///
    /// Fix with [Range(0.01, 100000, ...)] on both CreateDoctorRequest and
    /// UpdateDoctorRequest. Data annotations run before the service, so this test asserts
    /// the service also refuses — defence in depth for any non-HTTP caller.
    /// </summary>
    [Theory]
    [InlineData(-500)]
    [InlineData(-0.01)]
    [InlineData(0)]
    public async Task CreateAsync_NonPositiveConsultationFee_IsRejected(decimal fee)
    {
        var specialtyId = SeedSpecialty();

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(Build.CreateDoctor(specialtyId, consultationFee: fee)));
    }

    // ------------------------------------------------- account creation security

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 2.2. This is the most important test in the file.
    ///
    /// README 926: "passwords must never be predictable, derived from profile data,
    /// returned, or logged."
    ///
    /// DoctorService builds the password as string.Concat(FirstName, LicenseNumber).
    /// Verified live: logged in as the created doctor with "GregoryLIC-99001" and received
    /// 200 with a valid token. License numbers are published by medical boards, so the
    /// credential is a deterministic function of public data — that is not a weak password,
    /// it is no password.
    ///
    /// Fix per README 942: either create no login account with the profile, or use an
    /// explicit invitation flow with a cryptographically random password that is never
    /// returned. Patient.UserId is already nullable for precisely the first option.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PasswordIsNotDerivedFromProfileData()
    {
        var specialtyId = SeedSpecialty();
        var request = Build.CreateDoctor(
            specialtyId, firstName: "Gregory", lastName: "House", licenseNumber: "LIC-99001");

        await _sut.CreateAsync(request);

        var password = _auth.Accounts[0].Password;

        Assert.DoesNotContain(request.FirstName, password, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(request.LastName, password, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(request.LicenseNumber, password, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(request.UserName, password, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 2.2.
    ///
    /// Two doctors created from the same first name must not end up with related
    /// credentials. A generated password has to come from a cryptographic RNG, so two
    /// accounts can never collide.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TwoDoctors_GetUnrelatedPasswords()
    {
        var specialtyId = SeedSpecialty();

        await _sut.CreateAsync(Build.CreateDoctor(
            specialtyId, userName: "greg1", licenseNumber: "LIC-1", email: "g1@clinic.local"));
        await _sut.CreateAsync(Build.CreateDoctor(
            specialtyId, userName: "greg2", licenseNumber: "LIC-2", email: "g2@clinic.local"));

        Assert.NotEqual(_auth.Accounts[0].Password, _auth.Accounts[1].Password);
    }

    [Fact]
    public async Task CreateAsync_PasswordIsNeverReturned()
    {
        // DoctorResponse is a record, so its printed form covers every member. If a
        // password field is ever added to the response, this catches it.
        var specialtyId = SeedSpecialty();

        var response = await _sut.CreateAsync(Build.CreateDoctor(specialtyId));

        Assert.DoesNotContain("password", response.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_PasswordIsNeverLogged()
    {
        var specialtyId = SeedSpecialty();

        await _sut.CreateAsync(Build.CreateDoctor(specialtyId));

        Assert.DoesNotContain(
            _auth.Accounts[0].Password, _logger.AllText, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- transaction

    [Fact]
    public async Task CreateAsync_WhenTheProfileInsertFails_RollsBackTheAccount()
    {
        // The verified-good behaviour from the live audit: an over-long Gender column value
        // fails the insert after the Identity user exists, and no orphan account survives.
        // This pins that down so a future refactor cannot quietly lose it.
        var specialtyId = SeedSpecialty();
        _doctors.SaveChangesThrows = new InvalidOperationException("constraint violation");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(Build.CreateDoctor(specialtyId)));

        Assert.Equal(1, _transaction.RollbackCount);
        Assert.Equal(0, _transaction.CommitCount);
        Assert.Empty(_auth.Accounts);
        Assert.Empty(_doctors.Rows);
    }

    [Fact]
    public async Task CreateAsync_WhenAccountCreationFails_RollsBackAndAddsNoDoctor()
    {
        var specialtyId = SeedSpecialty();
        _auth.AddUserThrows = new ConflictException("Email already taken.");

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(Build.CreateDoctor(specialtyId)));

        Assert.Equal(1, _transaction.RollbackCount);
        Assert.Empty(_doctors.Rows);
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_CommitsExactlyOnce()
    {
        var specialtyId = SeedSpecialty();

        await _sut.CreateAsync(Build.CreateDoctor(specialtyId));

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
    public async Task GetByIdAsync_ResolvesTheSpecialtyName()
    {
        var specialtyId = SeedSpecialty(name: "Dentistry");
        var doctor = Build.Doctor(specialtyId: specialtyId);
        _doctors.Rows.Add(doctor);

        var response = await _sut.GetByIdAsync(doctor.Id);

        Assert.Equal("Dentistry", response.SpecialtyName);
    }

    [Fact]
    public async Task GetByLicenseNumberAsync_UnknownLicense_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.GetByLicenseNumberAsync("LIC-NOPE"));
    }

    [Fact]
    public async Task GetAllAsync_ExcludesInactiveByDefault()
    {
        var specialtyId = SeedSpecialty();
        _doctors.Rows.Add(Build.Doctor(licenseNumber: "LIC-1", specialtyId: specialtyId));
        _doctors.Rows.Add(Build.Doctor(licenseNumber: "LIC-2", specialtyId: specialtyId, isActive: false));

        var result = await _sut.GetAllAsync();

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_SearchMatchesFirstOrLastName()
    {
        var specialtyId = SeedSpecialty();
        _doctors.Rows.Add(Build.Doctor("Gregory", "House", "LIC-1", specialtyId));
        _doctors.Rows.Add(Build.Doctor("Lisa", "Cuddy", "LIC-2", specialtyId));

        var byFirst = await _sut.GetAllAsync(search: "greg");
        var byLast = await _sut.GetAllAsync(search: "cuddy");

        Assert.Single(byFirst.Items);
        Assert.Single(byLast.Items);
        Assert.Equal("Lisa Cuddy", byLast.Items[0].FullName);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 4 (optional).
    ///
    /// GetByIdAsync resolves the specialty name through ISpecialtyService; the list path in
    /// ToPagedResult reads d.Specialty?.Name ?? string.Empty instead. Two mapping paths for
    /// one field, so the list shows "" wherever the repository did not Include the
    /// navigation while the detail view shows the real name.
    ///
    /// Fix by using one mapping path for both.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReportsTheSameSpecialtyNameAsGetById()
    {
        var specialtyId = SeedSpecialty(name: "Dentistry");
        var doctor = Build.Doctor(specialtyId: specialtyId);
        _doctors.Rows.Add(doctor);

        var fromList = (await _sut.GetAllAsync()).Items.Single().SpecialtyName;
        var fromDetail = (await _sut.GetByIdAsync(doctor.Id)).SpecialtyName;

        Assert.Equal(fromDetail, fromList);
    }

    // ---------------------------------------------------------------- update

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsNotFound()
    {
        var specialtyId = SeedSpecialty();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), Build.UpdateDoctor(specialtyId)));
    }

    [Fact]
    public async Task UpdateAsync_KeepingItsOwnLicenseNumber_Succeeds()
    {
        var specialtyId = SeedSpecialty();
        var doctor = Build.Doctor(licenseNumber: "LIC-0001", specialtyId: specialtyId);
        _doctors.Rows.Add(doctor);

        var response = await _sut.UpdateAsync(
            doctor.Id, Build.UpdateDoctor(specialtyId, licenseNumber: "LIC-0001", consultationFee: 300m));

        Assert.Equal(300m, response.ConsultationFee);
    }

    [Fact]
    public async Task UpdateAsync_LicenseNumberOwnedByAnotherDoctor_ThrowsConflict()
    {
        var specialtyId = SeedSpecialty();
        var house = Build.Doctor(licenseNumber: "LIC-0001", specialtyId: specialtyId);
        var cuddy = Build.Doctor("Lisa", "Cuddy", "LIC-0002", specialtyId);
        _doctors.Rows.Add(house);
        _doctors.Rows.Add(cuddy);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(cuddy.Id, Build.UpdateDoctor(specialtyId, licenseNumber: "LIC-0001")));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.5. Same rule as create, on the update path.</summary>
    [Fact]
    public async Task UpdateAsync_MovingToAnInactiveSpecialty_IsRejected()
    {
        var activeId = SeedSpecialty();
        var retiredId = SeedSpecialty(isActive: false, name: "Retired Dept");
        var doctor = Build.Doctor(specialtyId: activeId);
        _doctors.Rows.Add(doctor);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.UpdateAsync(doctor.Id, Build.UpdateDoctor(retiredId)));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. Verified live: 200 OK, stored.</summary>
    [Fact]
    public async Task UpdateAsync_NegativeConsultationFee_IsRejected()
    {
        var specialtyId = SeedSpecialty();
        var doctor = Build.Doctor(specialtyId: specialtyId);
        _doctors.Rows.Add(doctor);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.UpdateAsync(doctor.Id, Build.UpdateDoctor(specialtyId, consultationFee: -500m)));
    }

    // ---------------------------------------------------------------- deactivate

    [Fact]
    public async Task DeactivateAsync_SoftDeletes_TheRowSurvives()
    {
        var doctor = Build.Doctor();
        _doctors.Rows.Add(doctor);

        await _sut.DeactivateAsync(doctor.Id);

        Assert.Single(_doctors.Rows);
        Assert.False(doctor.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_AlreadyInactive_IsIdempotent()
    {
        var doctor = Build.Doctor(isActive: false);
        _doctors.Rows.Add(doctor);

        await _sut.DeactivateAsync(doctor.Id);

        Assert.Equal(0, _doctors.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeactivateAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeactivateAsync(Guid.NewGuid()));
    }

    // ---------------------------------------------------------------- response shape

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.6.
    ///
    /// README 5.2 lists Email among a doctor's fields and the entity stores it, but
    /// DoctorResponse has no Email member — a client cannot read back what it wrote.
    /// SpecialtyId is missing too, so an edit form has only a display name to match on.
    ///
    /// Fix by adding both to the record. This test uses reflection because the member does
    /// not exist yet and a direct reference would not compile.
    /// </summary>
    [Fact]
    public void DoctorResponse_ExposesEmailAndSpecialtyId()
    {
        var members = typeof(Application.DTOs.Doctors.DoctorResponse)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.Contains("Email", members);
        Assert.Contains("SpecialtyId", members);
    }
}
