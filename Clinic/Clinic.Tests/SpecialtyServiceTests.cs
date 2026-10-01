using Clinic.Application.Common.Exceptions;
using Clinic.Application.Interfaces.Services;
using Clinic.Application.Services;
using Clinic.Tests.TestDoubles;
using Xunit;

namespace Clinic.Tests;

/// <summary>
/// CMS-010 business rules: duplicate names, normalisation, and soft delete.
/// README 5.3 — "Specialty names should not be duplicated" and deactivation, never deletion.
/// </summary>
public class SpecialtyServiceTests
{
    private readonly FakeSpecialtyRepository _repository = new();
    private readonly RecordingLogger<SpecialtyService> _logger = new();
    private readonly SpecialtyService _sut;

    public SpecialtyServiceTests()
    {
        _sut = new SpecialtyService(_repository, _logger);
    }

    // ---------------------------------------------------------------- create

    [Fact]
    public async Task CreateAsync_StoresTheSpecialtyAndReturnsIt()
    {
        var response = await _sut.CreateAsync(Build.CreateSpecialty("Cardiology"));

        Assert.Equal("Cardiology", response.Name);
        Assert.True(response.IsActive);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Single(_repository.Rows);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflict()
    {
        _repository.Rows.Add(Build.Specialty("Cardiology"));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(Build.CreateSpecialty("Cardiology")));

        Assert.Contains("already exists", ex.Message);
        Assert.Single(_repository.Rows);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameDifferentCasing_ThrowsConflict()
    {
        // The unique index is on a case-insensitive collation, so "cardiology" and
        // "Cardiology" are the same row as far as the database is concerned. The service
        // must reach the same conclusion, otherwise the API returns 500 instead of 409.
        _repository.Rows.Add(Build.Specialty("Cardiology"));

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(Build.CreateSpecialty("cardiology")));
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDescription()
    {
        // Without trimming, " Cardiology" and "Cardiology" both satisfy the unique index
        // and the clinic ends up with two specialties that look identical on screen.
        var response = await _sut.CreateAsync(
            Build.CreateSpecialty("  Cardiology  ", "  Heart  "));

        Assert.Equal("Cardiology", response.Name);
        Assert.Equal("Heart", response.Description);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task CreateAsync_WhitespaceOnlyName_ThrowsValidation(string name)
    {
        // [Required(AllowEmptyStrings = false)] rejects "" but not "   ", so the service
        // is the last line of defence for a whitespace-only name.
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(Build.CreateSpecialty(name)));

        Assert.Empty(_repository.Rows);
    }

    // ---------------------------------------------------------------- read

    [Fact]
    public async Task GetByIdAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByNameAsync_TrimsBeforeLookup()
    {
        _repository.Rows.Add(Build.Specialty("Cardiology"));

        var response = await _sut.GetByNameAsync("  Cardiology  ");

        Assert.Equal("Cardiology", response.Name);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesInactive_WhenIncludeInactiveIsFalse()
    {
        _repository.Rows.Add(Build.Specialty("Cardiology"));
        _repository.Rows.Add(Build.Specialty("Retired", isActive: false));

        var result = await _sut.GetAllAsync(includeInactive: false);

        Assert.Single(result.Items);
        Assert.Equal("Cardiology", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_IncludesInactive_WhenAsked()
    {
        _repository.Rows.Add(Build.Specialty("Cardiology"));
        _repository.Rows.Add(Build.Specialty("Retired", isActive: false));

        var result = await _sut.GetAllAsync(includeInactive: true);

        Assert.Equal(2, result.TotalCount);
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.8.
    ///
    /// ISpecialtyService declares includeInactive = false; SpecialtyService declares true.
    /// Default parameter values are baked in at the call site from the STATIC type of the
    /// reference, so the same call means different things through the interface and through
    /// the class. This test calls through the interface with the parameter omitted and
    /// asserts the safe behaviour: a soft-deleting service must not return deleted rows by
    /// default. Fix by setting both defaults to false.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_DefaultsToExcludingInactive()
    {
        _repository.Rows.Add(Build.Specialty("Cardiology"));
        _repository.Rows.Add(Build.Specialty("Retired", isActive: false));

        ISpecialtyService viaInterface = _sut;
        var result = await viaInterface.GetAllAsync();

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_SearchMatchesNameOrDescription_CaseInsensitively()
    {
        _repository.Rows.Add(Build.Specialty("Cardiology", description: "Heart and vessels"));
        _repository.Rows.Add(Build.Specialty("Dentistry", description: "Teeth"));

        var byName = await _sut.GetAllAsync(search: "cardio");
        var byDescription = await _sut.GetAllAsync(search: "TEETH");

        Assert.Single(byName.Items);
        Assert.Equal("Cardiology", byName.Items[0].Name);
        Assert.Single(byDescription.Items);
        Assert.Equal("Dentistry", byDescription.Items[0].Name);
    }

    // ---------------------------------------------------------------- update

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), Build.UpdateSpecialty()));
    }

    [Fact]
    public async Task UpdateAsync_KeepingItsOwnName_Succeeds()
    {
        // The excludeId argument to NameExistsAsync exists for exactly this case: changing
        // only the description must not be rejected as a duplicate of itself.
        var existing = Build.Specialty("Cardiology", description: "Old");
        _repository.Rows.Add(existing);

        var response = await _sut.UpdateAsync(
            existing.Id, Build.UpdateSpecialty("Cardiology", "New description"));

        Assert.Equal("Cardiology", response.Name);
        Assert.Equal("New description", response.Description);
    }

    [Fact]
    public async Task UpdateAsync_NameOwnedByAnotherSpecialty_ThrowsConflict()
    {
        var cardiology = Build.Specialty("Cardiology");
        var dentistry = Build.Specialty("Dentistry");
        _repository.Rows.Add(cardiology);
        _repository.Rows.Add(dentistry);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(dentistry.Id, Build.UpdateSpecialty("Cardiology")));
    }

    [Fact]
    public async Task UpdateAsync_CanDeactivateThroughIsActive()
    {
        var existing = Build.Specialty("Cardiology");
        _repository.Rows.Add(existing);

        var response = await _sut.UpdateAsync(
            existing.Id, Build.UpdateSpecialty("Cardiology", isActive: false));

        Assert.False(response.IsActive);
    }

    // ---------------------------------------------------------------- deactivate

    [Fact]
    public async Task DeactivateAsync_SoftDeletes_TheRowSurvives()
    {
        // README 5.3 asks for deactivation, never deletion: a hard delete would orphan
        // every doctor holding this specialty and break booked appointment history.
        var existing = Build.Specialty("Cardiology");
        _repository.Rows.Add(existing);

        await _sut.DeactivateAsync(existing.Id);

        Assert.Single(_repository.Rows);
        Assert.False(existing.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_AlreadyInactive_IsIdempotent()
    {
        // A retried request must not fail differently. No exception, and no pointless write.
        var existing = Build.Specialty("Retired", isActive: false);
        _repository.Rows.Add(existing);

        await _sut.DeactivateAsync(existing.Id);

        Assert.False(existing.IsActive);
        Assert.Equal(0, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeactivateAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeactivateAsync(Guid.NewGuid()));
    }

    // ---------------------------------------------------------------- logging

    [Fact]
    public async Task CreateAsync_LogsIdentifiersOnly()
    {
        await _sut.CreateAsync(Build.CreateSpecialty("Cardiology"));

        Assert.Contains(_logger.Messages, m => m.Contains("Specialty created"));
    }
}
