using Clinic.Application.Common;
using Clinic.Application.Common.Exceptions;
using Clinic.Application.DTOs.Auth;
using Clinic.Application.DTOs.Patients;
using Clinic.Application.DTOs.Specialties;
using Clinic.Application.Interfaces.Repository;
using Clinic.Application.Interfaces.Services;
using Clinic.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Clinic.Application.Services;

public class PatientService(IPatientRepository _repository,ILogger<PatientService> _logger,IAuthService authService) : IPatientService
{
    
    public async Task<PagedResult<PatientResponse>> GetAllAsync(bool includeInactive = false, string? search = null, int pageNumber = 1, int pageSize = 10)
    {
        var patients = await _repository.GetAllAsync(includeInactive);
        // Apply search filter if provided
        var filteredPatients = SearchPatient(patients, search ?? string.Empty);
        return ToPagedResult(filteredPatients, pageNumber, pageSize);
    }
    public async Task<PatientResponse> GetByIdAsync(Guid id)
    {
        
        var patient = await _repository.GetByIdAsync(id)
            ?? throw NotFoundException.For(nameof(Patient), id);

        return ToResponse(patient);
    }
    public async Task<PatientResponse> CreateAsync(CreatePatientRequest request)
    {
        
        await _repository.BeginTransactionAsync();
        try
        {
              var SpecificUserRequest = new specificUserRequest
        {
            Email = request.Email,
            Password = string.Concat(request.FirstName, request.DateOfBirth.ToString("yyyyMMdd")),
            UserName = request.UserName,
            Role = Roles.Patient
        };
       
        await authService.AddUser(SpecificUserRequest);
        var user = await authService.GetUserbyEmailAsync(request.Email);
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            DateOfBirth = request.DateOfBirth,
            EmergencyContact = request.EmergencyContact,
            Gender = request.Gender,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(patient);
        await _repository.SaveChangesAsync();
        await _repository.CommitTransactionAsync();
          _logger.LogInformation(
            "Patient created. PatientId={PatientId}",
            patient.Id);

        return ToResponse(patient);
            
        }
        catch
        {
            await _repository.RollbackTransactionAsync();
            throw;
            
        }
    
    }

    public async Task<PatientResponse> UpdateAsync(Guid id, UpdatePatientRequest request)
    {
        var patient = await _repository.GetByIdAsync(id)
            ?? throw NotFoundException.For(nameof(Patient), id);

        patient.FirstName = request.FirstName?.Trim()?? patient.FirstName;
        patient.LastName = request.LastName?.Trim()?? patient.LastName;
        patient.Email = request.Email?.Trim()?? patient.Email;
        patient.Phone = request.Phone?.Trim()?? patient.Phone;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender?? patient.Gender;
        patient.Address = request.Address?.Trim()?? patient.Address;
        patient.EmergencyContact = request.EmergencyContact?.Trim()?? patient.EmergencyContact;
        patient.IsActive = request.IsActive;

        _repository.Update(patient);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Patient updated. PatientId={PatientId} Email={Email} IsActive={IsActive}",
            patient.Id, patient.Email, patient.IsActive);

        return ToResponse(patient);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var patient = await _repository.GetByIdAsync(id)
            ?? throw NotFoundException.For(nameof(Patient), id);

        if (!patient.IsActive)
        {
            // Already deactivated. Returning success rather than a conflict keeps the
            // operation idempotent: a retried request must not fail differently.
            _logger.LogInformation(
                "Patient already inactive; no change. PatientId={PatientId}", id);
            return;
        }

        patient.IsActive = false;
        await authService.DisableUserAsync(patient.UserId ?? throw new InvalidOperationException("Patient UserId is null."));
        _repository.Update(patient);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Patient deactivated. PatientId={PatientId}", id);
    }


    public async Task ActivateAsync(Guid id)
    {
        var patient = await _repository.GetByIdAsync(id)
            ?? throw NotFoundException.For(nameof(Patient), id);

        if (patient.IsActive)
        {
            // Already activated. Returning success rather than a conflict keeps the
            // operation idempotent: a retried request must not fail differently.
            _logger.LogInformation(
                "Patient already active; no change. PatientId={PatientId}", id);
            return;
        }
        patient.IsActive = true;
        await authService.EnableUserAsync(patient.UserId ?? throw new InvalidOperationException("Patient UserId is null."));
        _repository.Update(patient);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Patient activated. PatientId={PatientId}", id);
    }
     // Convert Patient entity to PatientResponse DTO
      private static PatientResponse ToResponse(Patient p) =>
        new PatientResponse(p.Id, p.FirstName, p.LastName, p.Email ?? string.Empty, p.Phone, p.DateOfBirth, p.Gender?? string.Empty, p.Address?? string.Empty, p.EmergencyContact?? string.Empty, p.IsActive, p.CreatedAt){};
    
     private static PagedResult<PatientResponse> ToPagedResult(IEnumerable<Patient> patients, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 10;
        }
        var totalCount = patients.Count();
        var items = patients
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();

        return new PagedResult<PatientResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
    private static IEnumerable<Patient> SearchPatient(IEnumerable<Patient> AllPatients, string searchTerm)
    {
        return string.IsNullOrWhiteSpace(searchTerm)
            ? AllPatients
            : AllPatients.Where(p => p.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                      p.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

}
