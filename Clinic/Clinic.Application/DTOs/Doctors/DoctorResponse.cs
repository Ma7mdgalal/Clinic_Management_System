using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Domain.Models;

namespace Clinic.Application.DTOs.Doctors;

public record DoctorResponse(
Guid Id,
// string UserName,
string FullName,
string LicenseNumber,
bool IsActive,
decimal ConsultationFee,
string Phone,
string SpecialtyName,
DateTime CreatedAt);