
using Clinic.Application.Common;
using Clinic.Application.Common.Exceptions;
using Clinic.Application.DTOs.Auth;
using Clinic.Application.DTOs.Doctors;
using Clinic.Application.Interfaces.Repository;
using Clinic.Application.Interfaces.Services;
using Clinic.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Clinic.Application.Services;

public class DoctorService(IDoctorRepository doctorRepository, ILogger<DoctorService> logger,IAuthService authService,ISpecialtyService specialtyService) : IDoctorService
{

    // Create a new doctor and register the user in the identity system
    public async Task<DoctorResponse> CreateAsync(CreateDoctorRequest request)
    {
        if (await doctorRepository.GetByLicenseNumberAsync(request.LicenseNumber) is not null)
        {
            throw new ConflictException($"A doctor with license number '{request.LicenseNumber}' already exists.");
        }
        var specialty = await specialtyService.GetByIdAsync(request.SpecialtyId);
        await doctorRepository.BeginTransactionAsync();
        try
        {
              var SpecificUserRequest = new specificUserRequest
        {
            Email = request.Email,
            Password = string.Concat(request.FirstName, request.LicenseNumber),
            UserName = request.UserName,
            Role = Roles.Doctor
        };
       
        await authService.AddUser(SpecificUserRequest);
        var user = await authService.GetUserbyEmailAsync(request.Email);
        var doctor = new Doctor
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            LicenseNumber = request.LicenseNumber,
            ConsultationFee = request.ConsultationFee,
            Phone = request.Phone,
            SpecialtyId = request.SpecialtyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await doctorRepository.AddAsync(doctor);
        await doctorRepository.SaveChangesAsync();
        await doctorRepository.CommitTransactionAsync();
          logger.LogInformation(
            "Doctor created. DoctorId={DoctorId} LicenseNumber={LicenseNumber}",
            doctor.Id, doctor.LicenseNumber);

        return ToResponse(doctor, specialty.Name);
            
        }
        catch
        {
            await doctorRepository.RollbackTransactionAsync();
            throw;
        }
      
    }
    // Deactivates the doctor

    public async Task DeactivateAsync(Guid id)
    {
        var doctor = await doctorRepository.GetByIdAsync(id) ?? throw NotFoundException.For(nameof(Doctor), id);
        if (!doctor.IsActive)
        {
            logger.LogInformation("Doctor already inactive. DoctorId={DoctorId} LicenseNumber={LicenseNumber}", doctor.Id, doctor.LicenseNumber);
            return;
        }
        await authService.DisableUserAsync(doctor.UserId);
        doctor.IsActive = false;
        doctorRepository.Update(doctor);
        await doctorRepository.SaveChangesAsync();
        logger.LogInformation("Doctor deactivated. DoctorId={DoctorId} LicenseNumber={LicenseNumber}", doctor.Id, doctor.LicenseNumber);
    }
    // Reactivates the doctor
        public async Task ReactivateAsync(Guid id)
    {
        var doctor = await doctorRepository.GetByIdAsync(id) ?? throw NotFoundException.For(nameof(Doctor), id);
        if (doctor.IsActive)
        {
            logger.LogInformation("Doctor already active. DoctorId={DoctorId} LicenseNumber={LicenseNumber}", doctor.Id, doctor.LicenseNumber);
            return;
        }
        await authService.EnableUserAsync(doctor.UserId);
        doctor.IsActive = true;
        doctorRepository.Update(doctor);
        await doctorRepository.SaveChangesAsync();
        logger.LogInformation("Doctor reactivated. DoctorId={DoctorId} LicenseNumber={LicenseNumber}", doctor.Id, doctor.LicenseNumber);
    }

// Get All doctors
    public async Task<PagedResult<DoctorResponse>> GetAllAsync(bool includeInactive = false, string? search = null, int pageNumber = 1, int pageSize = 10)
    {
        var allDoctors = await doctorRepository.GetAllAsync(includeInactive);
        var filteredDoctors = SearchDoctor(allDoctors, search ?? string.Empty);
        return ToPagedResult(filteredDoctors, pageNumber, pageSize);
    }

    // Get doctor by Id
    public async Task<DoctorResponse> GetByIdAsync(Guid id)
    {
        var doctor = await doctorRepository.GetByIdAsync(id) ?? throw NotFoundException.For(nameof(Doctor), id);
        var specialty = await specialtyService.GetByIdAsync(doctor.SpecialtyId);
        return ToResponse(doctor, specialty!.Name);
    }

// Get doctor by License Number
    public async Task<DoctorResponse> GetByLicenseNumberAsync(string name)
    {
        var doctor = await doctorRepository.GetByLicenseNumberAsync(name) ?? throw new NotFoundException($"Doctor with license number '{name}' was not found.");
        var specialty = await specialtyService.GetByIdAsync(doctor.SpecialtyId);
        return ToResponse(doctor, specialty.Name);
    }

// Update doctor details
    public async Task<DoctorResponse> UpdateAsync(Guid id, UpdateDoctorRequest request)
    {
        var doctor = await doctorRepository.GetByIdAsync(id) ?? throw NotFoundException.For(nameof(Doctor), id);
        if (doctor.LicenseNumber != request.LicenseNumber && await doctorRepository.GetByLicenseNumberAsync(request.LicenseNumber) is not null)
        {
            throw new ConflictException($"A doctor with license number '{request.LicenseNumber}' already exists.");
        }
        var specialty = await specialtyService.GetByIdAsync(request.SpecialtyId);
        doctor.Email = request.Email;
        doctor.Phone = request.Phone;
        doctor.SpecialtyId = request.SpecialtyId;
        doctor.LicenseNumber = request.LicenseNumber;
        doctor.ConsultationFee = request.ConsultationFee;
        doctor.IsActive = request.IsActive;

        doctorRepository.Update(doctor);
        await doctorRepository.SaveChangesAsync();

        logger.LogInformation(
            "Doctor updated. DoctorId={DoctorId} LicenseNumber={LicenseNumber} IsActive={IsActive}",
            doctor.Id, doctor.LicenseNumber, doctor.IsActive);

        return ToResponse(doctor, specialty.Name);
    }
    // Convert Doctor entity to DoctorResponse DTO
      private static DoctorResponse ToResponse(Doctor d, string specialtyName) =>
         new DoctorResponse(d.Id,string.Concat(d.FirstName, " ", d.LastName),d.LicenseNumber,d.IsActive,d.ConsultationFee,d.Phone,specialtyName,d.CreatedAt);
          private static PagedResult<DoctorResponse> ToPagedResult(IEnumerable<Doctor> doctors, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 10;
        }
        var totalCount = doctors.Count();
        var items = doctors
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => ToResponse(d, d.Specialty?.Name ?? string.Empty))
            .ToList();

        return new PagedResult<DoctorResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
    private static IEnumerable<Doctor> SearchDoctor(IEnumerable<Doctor> AllDoctors, string searchTerm)
    {
        return string.IsNullOrWhiteSpace(searchTerm)
            ? AllDoctors
            : AllDoctors.Where(d => d.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                     d.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

}