using Clinic.Application.Common;
using Clinic.Application.Common.Exceptions;
using Clinic.Application.DTOs.Specialties;
using Clinic.Application.Interfaces.Repository;
using Clinic.Application.Interfaces.Services;
using Clinic.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Clinic.Application.Services;

public class SpecialtyService(ISpecialtyRepository _repository, ILogger<SpecialtyService> _logger) : ISpecialtyService
{

    public async Task<PagedResult<SpecialtyResponse>> GetAllAsync(bool includeInactive = true, string? search = null, int pageNumber = 1, int pageSize = 10)
    {

        var specialties = await _repository.GetAllAsync(includeInactive);
        // Apply search filter if provided
        var filteredSpecialties = SearchSpecialty(specialties, search?? string.Empty);
        return ToPagedResult(filteredSpecialties, pageNumber, pageSize);
    }


    public async Task<SpecialtyResponse> GetByIdAsync(Guid id)
    {
        var specialty = await _repository.GetByIdAsync(id)
            ?? throw NotFoundException.For(nameof(Specialty), id);

        return ToResponse(specialty);
    }

    public async Task<SpecialtyResponse> GetByNameAsync(string name)
    {
        var trimmed = NormalizeName(name);

        var specialty = await _repository.GetByNameAsync(trimmed)
            ?? throw new NotFoundException($"Specialty '{trimmed}' was not found.");

        return ToResponse(specialty);
    }

    public async Task<SpecialtyResponse> CreateAsync(CreateSpecialtyRequest request)
    {
        var name = NormalizeName(request.Name);

        // Checked here so the caller gets a clear 409 instead of a 500 from the unique
        // index. The index still exists and is still the real guarantee — this check can
        // lose a race between two simultaneous creates, and the database must win that race.
        if (await _repository.NameExistsAsync(name))
        {
            throw new ConflictException($"A specialty named '{name}' already exists.");
        }

        var specialty = new Specialty
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(specialty);
        await _repository.SaveChangesAsync();

        // Message template with named placeholders, not string interpolation: Serilog stores
        // SpecialtyId and Name as queryable fields instead of baking them into flat text.
        _logger.LogInformation(
            "Specialty created. SpecialtyId={SpecialtyId} Name={Name}",
            specialty.Id, specialty.Name);

        return ToResponse(specialty);
    }

    public async Task<SpecialtyResponse> UpdateAsync(Guid id, UpdateSpecialtyRequest request)
    {
        var specialty = await _repository.GetByIdAsync(id)
            ?? throw NotFoundException.For(nameof(Specialty), id);

        var name = NormalizeName(request.Name);

        // excludeId keeps a specialty from conflicting with itself when only the
        // description or the active flag changes.
        if (await _repository.NameExistsAsync(name, excludeId: id))
        {
            throw new ConflictException($"A specialty named '{name}' already exists.");
        }

        specialty.Name = name;
        specialty.Description = request.Description?.Trim()?? specialty.Description;
        specialty.IsActive = request.IsActive;

        _repository.Update(specialty);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Specialty updated. SpecialtyId={SpecialtyId} Name={Name} IsActive={IsActive}",
            specialty.Id, specialty.Name, specialty.IsActive);

        return ToResponse(specialty);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var specialty = await _repository.GetByIdAsync(id)
            ?? throw NotFoundException.For(nameof(Specialty), id);

        if (!specialty.IsActive)
        {
            // Already deactivated. Returning success rather than a conflict keeps the
            // operation idempotent: a retried request must not fail differently.
            _logger.LogInformation(
                "Specialty already inactive; no change. SpecialtyId={SpecialtyId}", id);
            return;
        }

        specialty.IsActive = false;

        _repository.Update(specialty);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Specialty deactivated. SpecialtyId={SpecialtyId}", id);
    }

    /// <summary>
    /// Trims surrounding whitespace so " Cardiology" and "Cardiology" cannot both exist.
    /// The [Required] annotation already rejects null and empty, so this only guards
    /// against a whitespace-only value slipping past it.
    /// </summary>
    private static string NormalizeName(string name)
    {
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationException("Specialty name cannot be empty or whitespace.");
        }

        return trimmed;
    }

    private static SpecialtyResponse ToResponse(Specialty s) =>
        new(s.Id, s.Name, s.Description, s.IsActive, s.CreatedAt);

    private static PagedResult<SpecialtyResponse> ToPagedResult(IEnumerable<Specialty> specialties, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 10;
        }
        var totalCount = specialties.Count();
        var items = specialties
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();

        return new PagedResult<SpecialtyResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
    private static IEnumerable<Specialty> SearchSpecialty(IEnumerable<Specialty> AllSpecialties, string searchTerm)
    {
        return string.IsNullOrWhiteSpace(searchTerm)
            ? AllSpecialties
            : AllSpecialties.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                        (s.Description != null && s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
    }

}
