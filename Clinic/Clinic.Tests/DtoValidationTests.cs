using System.ComponentModel.DataAnnotations;
using Clinic.Tests.TestDoubles;
using Xunit;

namespace Clinic.Tests;

/// <summary>
/// Data-annotation coverage on the request DTOs.
///
/// These run the same validator ASP.NET Core runs during model binding, so a failure here
/// is a real 400-that-does-not-happen. README 927 asks for validation of "required fields,
/// identifiers, emails, phone numbers, and positive monetary values"; several of these
/// tests fail today and each names the gap it covers.
///
/// Worth knowing while reading: every validation attribute except [Required] passes on a
/// null value by design, so [EmailAddress] alone does not make a field mandatory. And the
/// C# `required` modifier is a compile-time rule about object initialisation — model
/// binding satisfies it with "" and the runtime never objects.
/// </summary>
public class DtoValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }

    private static bool Fails(object instance, string memberName) =>
        Validate(instance).Any(r => r.MemberNames.Contains(memberName));

    // ---------------------------------------------------------------- baseline

    [Fact]
    public void TheBuilders_ProduceValidRequests()
    {
        // If this fails, every "invalid input is rejected" test below is meaningless,
        // because the builder itself would be the thing being rejected.
        Assert.Empty(Validate(Build.CreateSpecialty()));
        Assert.Empty(Validate(Build.UpdateSpecialty()));
        Assert.Empty(Validate(Build.CreateDoctor(Guid.NewGuid())));
        Assert.Empty(Validate(Build.UpdateDoctor(Guid.NewGuid())));
        Assert.Empty(Validate(Build.CreatePatient()));
        Assert.Empty(Validate(Build.UpdatePatient()));
    }

    // ---------------------------------------------------------------- specialty

    [Fact]
    public void CreateSpecialty_EmptyName_IsRejected()
    {
        Assert.True(Fails(Build.CreateSpecialty(name: ""), "Name"));
    }

    [Fact]
    public void CreateSpecialty_OverLongName_IsRejected()
    {
        Assert.True(Fails(Build.CreateSpecialty(name: new string('x', 101)), "Name"));
    }

    [Fact]
    public void CreateSpecialty_OverLongDescription_IsRejected()
    {
        Assert.True(Fails(
            Build.CreateSpecialty(description: new string('x', 501)), "Description"));
    }

    // ---------------------------------------------------------------- doctor

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.2.
    ///
    /// README 927 requires "positive monetary values". ConsultationFee has no [Range] on
    /// either DTO. Verified live: -500 → 201 Created, stored as -500.00. A doctor the clinic
    /// pays patients to see.
    ///
    /// Fix: [Range(0.01, 100000, ErrorMessage = "...")] on both request records.
    /// </summary>
    [Theory]
    [InlineData(-500)]
    [InlineData(-0.01)]
    [InlineData(0)]
    public void CreateDoctor_NonPositiveConsultationFee_IsRejected(decimal fee)
    {
        Assert.True(Fails(
            Build.CreateDoctor(Guid.NewGuid(), consultationFee: fee), "ConsultationFee"));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. Same missing [Range] on the update DTO.</summary>
    [Theory]
    [InlineData(-500)]
    [InlineData(0)]
    public void UpdateDoctor_NonPositiveConsultationFee_IsRejected(decimal fee)
    {
        Assert.True(Fails(
            Build.UpdateDoctor(Guid.NewGuid(), consultationFee: fee), "ConsultationFee"));
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.2.
    ///
    /// UpdateDoctorRequest.Email is `public string Email { get; init; } = default!;` — no
    /// [Required], no [EmailAddress]. Verified live: PUT with "not-an-email-at-all" returned
    /// 200 and stored it. A doctor's contact email is how the clinic reaches them.
    /// </summary>
    [Theory]
    [InlineData("not-an-email-at-all")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@no-local-part.com")]
    public void UpdateDoctor_InvalidEmail_IsRejected(string email)
    {
        Assert.True(Fails(Build.UpdateDoctor(Guid.NewGuid(), email: email), "Email"));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. [EmailAddress] with no [Required] passes on empty.</summary>
    [Fact]
    public void CreateDoctor_EmptyEmail_IsRejected()
    {
        Assert.True(Fails(Build.CreateDoctor(Guid.NewGuid(), email: ""), "Email"));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. LicenseNumber has no attribute at all.</summary>
    [Fact]
    public void CreateDoctor_EmptyLicenseNumber_IsRejected()
    {
        Assert.True(Fails(Build.CreateDoctor(Guid.NewGuid(), licenseNumber: ""), "LicenseNumber"));
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.2.
    ///
    /// SpecialtyId is an unvalidated Guid, so Guid.Empty binds happily and reaches the
    /// service, which then reports 404 for an id no caller could have meant. A [Required]
    /// Guid does not help — default(Guid) is not null. Either a small [NotEmptyGuid]
    /// attribute or an explicit service check gives a 400 with a useful message.
    /// </summary>
    [Fact]
    public void CreateDoctor_EmptySpecialtyId_IsRejected()
    {
        Assert.True(Fails(Build.CreateDoctor(Guid.Empty), "SpecialtyId"));
    }

    [Fact]
    public void CreateDoctor_ShortFirstName_IsRejected()
    {
        // [StringLength(100, MinimumLength = 2)] — this one is already correct.
        Assert.True(Fails(Build.CreateDoctor(Guid.NewGuid(), firstName: "G"), "FirstName"));
    }

    // ---------------------------------------------------------------- patient

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.2 / README 945.
    ///
    /// "Date of birth is valid and cannot be in the future." No attribute expresses this,
    /// and neither DTO nor service checks it. Verified live: 2090-01-01 → 201 Created.
    ///
    /// Data annotations have no built-in rule for this, so write one:
    ///
    ///   public sealed class NotInFutureAttribute : ValidationAttribute
    ///   {
    ///       protected override ValidationResult? IsValid(object? value, ValidationContext ctx) =>
    ///           value is DateOnly d && d > DateOnly.FromDateTime(DateTime.UtcNow)
    ///               ? new ValidationResult(ErrorMessage ?? "Date cannot be in the future.",
    ///                                      [ctx.MemberName!])
    ///               : ValidationResult.Success;
    ///   }
    ///
    /// Declarative, reusable for any future date field, and it shows up in Swagger.
    /// </summary>
    [Fact]
    public void CreatePatient_FutureDateOfBirth_IsRejected()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Assert.True(Fails(Build.CreatePatient(dateOfBirth: tomorrow), "DateOfBirth"));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. Same rule on the update DTO.</summary>
    [Fact]
    public void UpdatePatient_FutureDateOfBirth_IsRejected()
    {
        var farFuture = new DateOnly(2090, 1, 1);

        Assert.True(Fails(Build.UpdatePatient(dateOfBirth: farFuture), "DateOfBirth"));
    }

    [Fact]
    public void CreatePatient_PastDateOfBirth_IsAccepted()
    {
        Assert.False(Fails(
            Build.CreatePatient(dateOfBirth: new DateOnly(1990, 5, 14)), "DateOfBirth"));
    }

    /// <summary>
    /// FAILS TODAY — Sprint 3 review 3.2.
    ///
    /// Gender maps to nvarchar(20), Address to nvarchar(200), EmergencyContact to
    /// nvarchar(100), and none of the three has a [StringLength]. SQL Server rejects the
    /// insert, the exception is not an AppException, and the handler correctly reports 500.
    /// Verified live with an over-long Gender.
    ///
    /// Every one of those 500s is a 400 in disguise. A DTO length must mirror its column
    /// width — otherwise the database is doing your input validation, and doing it badly.
    /// </summary>
    [Fact]
    public void CreatePatient_OverLongGender_IsRejected()
    {
        Assert.True(Fails(Build.CreatePatient(gender: new string('x', 21)), "Gender"));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. Address column is nvarchar(200).</summary>
    [Fact]
    public void CreatePatient_OverLongAddress_IsRejected()
    {
        Assert.True(Fails(Build.CreatePatient(address: new string('x', 201)), "Address"));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. EmergencyContact column is nvarchar(100).</summary>
    [Fact]
    public void CreatePatient_OverLongEmergencyContact_IsRejected()
    {
        Assert.True(Fails(
            Build.CreatePatient(emergencyContact: new string('x', 101)), "EmergencyContact"));
    }

    /// <summary>FAILS TODAY — Sprint 3 review 3.2. The same three fields on the update DTO.</summary>
    [Fact]
    public void UpdatePatient_OverLongTextFields_AreRejected()
    {
        Assert.True(Fails(Build.UpdatePatient(gender: new string('x', 21)), "Gender"));
        Assert.True(Fails(Build.UpdatePatient(address: new string('x', 201)), "Address"));
        Assert.True(Fails(
            Build.UpdatePatient(emergencyContact: new string('x', 101)), "EmergencyContact"));
    }

    [Fact]
    public void CreatePatient_EmptyEmail_IsRejected()
    {
        // FAILS TODAY — [EmailAddress] with no [Required] passes on an empty string.
        Assert.True(Fails(Build.CreatePatient(email: ""), "Email"));
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("no-at-sign.com")]
    public void CreatePatient_InvalidEmail_IsRejected(string email)
    {
        // Already correct — [EmailAddress] is present and does catch malformed values.
        Assert.True(Fails(Build.CreatePatient(email: email), "Email"));
    }

    [Fact]
    public void CreatePatient_ShortFirstName_IsRejected()
    {
        Assert.True(Fails(Build.CreatePatient(firstName: "J"), "FirstName"));
    }

    // ---------------------------------------------------------------- over-posting

    /// <summary>
    /// Sprint 3 review — a create DTO should not carry IsActive.
    ///
    /// Both CreateDoctorRequest and CreatePatientRequest expose a settable IsActive. The
    /// services ignore it and hardcode true, so nothing is exploitable today — but a
    /// property a client can send and the server silently discards is a lie in the API
    /// contract, and the day someone maps it through is the day "create an already
    /// deactivated patient" becomes reachable. Lifecycle belongs to the update and
    /// deactivate endpoints.
    ///
    /// This test documents the current shape rather than asserting a fix; delete the
    /// property and invert the assertion when you clean it up.
    /// </summary>
    [Fact]
    public void CreateRequests_StillCarryIsActive_WhichTheServicesIgnore()
    {
        Assert.NotNull(
            typeof(Application.DTOs.Doctors.CreateDoctorRequest).GetProperty("IsActive"));
        Assert.NotNull(
            typeof(Application.DTOs.Patients.CreatePatientRequest).GetProperty("IsActive"));
    }
}
