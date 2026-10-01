using Clinic.Application.DTOs.Doctors;
using Clinic.Application.DTOs.Patients;
using Clinic.Application.DTOs.Specialties;
using Clinic.Domain.Models;

namespace Clinic.Tests.TestDoubles;

/// <summary>
/// Valid-by-default objects that a test overrides one field at a time.
///
/// This keeps each test about the single thing it is checking. Without it, a test that
/// cares only about a negative consultation fee would still have to spell out eight
/// unrelated properties, and the reader would have to work out which one matters.
/// </summary>
public static class Build
{
    public static Specialty Specialty(
        string name = "Cardiology",
        bool isActive = true,
        Guid? id = null,
        string? description = "Heart and vessels") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

    public static Doctor Doctor(
        string firstName = "Gregory",
        string lastName = "House",
        string licenseNumber = "LIC-0001",
        Guid? specialtyId = null,
        decimal consultationFee = 250m,
        bool isActive = true,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = $"user-{licenseNumber}",
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLowerInvariant()}@clinic.local",
            Phone = "+201000000000",
            SpecialtyId = specialtyId ?? Guid.NewGuid(),
            LicenseNumber = licenseNumber,
            ConsultationFee = consultationFee,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

    public static Patient Patient(
        string firstName = "John",
        string lastName = "Doe",
        bool isActive = true,
        Guid? id = null,
        string? address = "12 Elm St",
        DateOnly? dateOfBirth = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = $"user-{firstName}",
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLowerInvariant()}@clinic.local",
            Phone = "+201000000000",
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 5, 14),
            Gender = "Male",
            Address = address,
            EmergencyContact = "+201111111111",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

    public static CreateSpecialtyRequest CreateSpecialty(
        string name = "Cardiology",
        string? description = "Heart and vessels") =>
        new() { Name = name, Description = description };

    public static UpdateSpecialtyRequest UpdateSpecialty(
        string name = "Cardiology",
        string? description = "Heart and vessels",
        bool isActive = true) =>
        new() { Name = name, Description = description, IsActive = isActive };

    public static CreateDoctorRequest CreateDoctor(
        Guid specialtyId,
        string userName = "drhouse",
        string firstName = "Gregory",
        string lastName = "House",
        string licenseNumber = "LIC-0001",
        decimal consultationFee = 250m,
        string email = "gregory@clinic.local") =>
        new()
        {
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "+201000000000",
            SpecialtyId = specialtyId,
            LicenseNumber = licenseNumber,
            ConsultationFee = consultationFee
        };

    public static UpdateDoctorRequest UpdateDoctor(
        Guid specialtyId,
        string licenseNumber = "LIC-0001",
        decimal consultationFee = 250m,
        string email = "gregory@clinic.local",
        bool isActive = true) =>
        new()
        {
            Email = email,
            Phone = "+201000000000",
            SpecialtyId = specialtyId,
            LicenseNumber = licenseNumber,
            ConsultationFee = consultationFee,
            IsActive = isActive
        };

    public static CreatePatientRequest CreatePatient(
        string userName = "jdoe",
        string firstName = "John",
        string lastName = "Doe",
        string email = "jdoe@clinic.local",
        DateOnly? dateOfBirth = null,
        string gender = "Male",
        string address = "12 Elm St",
        string emergencyContact = "+201111111111") =>
        new()
        {
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "+201000000000",
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 5, 14),
            Gender = gender,
            Address = address,
            EmergencyContact = emergencyContact
        };

    public static UpdatePatientRequest UpdatePatient(
        string firstName = "John",
        string lastName = "Doe",
        string email = "jdoe@clinic.local",
        DateOnly? dateOfBirth = null,
        string gender = "Male",
        string address = "12 Elm St",
        string emergencyContact = "+201111111111",
        bool isActive = true) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "+201000000000",
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 5, 14),
            Gender = gender,
            Address = address,
            EmergencyContact = emergencyContact,
            IsActive = isActive
        };
}

// 01001230846