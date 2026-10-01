using Clinic.Application.Common;

namespace Clinic.Tests;

/// <summary>
/// The role-escalation guard and the policy-name constants.
///
/// These look like trivial tests of trivial code. They are not: <see cref="Roles.Assignable"/>
/// is the only thing standing between an admin-facing endpoint and self-service Admin
/// promotion, and that bug has already occurred once in this project.
/// </summary>
public class RolesAndPoliciesTests
{
    [Fact]
    public void Admin_IsNotAssignableOverHttp()
    {
        // Administrators come from the bootstrap seeder, never from an API call. If this
        // ever passes "Admin" through, every endpoint that grants roles becomes a
        // privilege-escalation vector.
        Assert.Null(Roles.NormalizeAssignable(Roles.Admin));
        Assert.False(Roles.IsAssignable(Roles.Admin));
        Assert.DoesNotContain(Roles.Admin, Roles.Assignable);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("AdMiN")]
    public void Admin_IsRejectedRegardlessOfCasing(string role)
    {
        // A case-sensitive check would let "admin" through, and Identity role lookups are
        // case-insensitive — so the account would end up genuinely privileged.
        Assert.Null(Roles.NormalizeAssignable(role));
        Assert.False(Roles.IsAssignable(role));
    }

    [Theory]
    [InlineData("doctor", "Doctor")]
    [InlineData("PATIENT", "Patient")]
    [InlineData("rEcEpTiOnIsT", "Receptionist")]
    public void AssignableRoles_AreCanonicalised(string input, string expected)
    {
        // Returning canonical casing matters because the role string is stored and later
        // compared. "doctor" and "Doctor" must not become two different roles.
        Assert.Equal(expected, Roles.NormalizeAssignable(input));
        Assert.True(Roles.IsAssignable(input));
    }

    [Theory]
    [InlineData("Nurse")]
    [InlineData("SuperUser")]
    [InlineData("")]
    [InlineData("  ")]
    public void UnknownRoles_AreRejected(string role)
    {
        Assert.Null(Roles.NormalizeAssignable(role));
        Assert.False(Roles.IsAssignable(role));
    }

    [Fact]
    public void All_ContainsEveryRole_AndAssignableIsAllMinusAdmin()
    {
        Assert.Equal(4, Roles.All.Count);
        Assert.Contains(Roles.Admin, Roles.All);
        Assert.Equal(Roles.All.Where(r => r != Roles.Admin), Roles.Assignable);
    }

    /// <summary>
    /// Sprint 3 review 2.1, stated as a test of the constants.
    ///
    /// [Authorize(Policy = ...)] is a lookup key, not an expression. Concatenating
    /// Policies.RequireAdmin + " or " + Policies.RequireDoctor produced the literal name
    /// "RequireAdmin or RequireDoctor", which nothing registered, so GET /api/doctors threw
    /// InvalidOperationException and returned 500 to every caller — Admin included.
    ///
    /// A policy name must be a single registered identifier with no spaces. This test
    /// enforces that shape on every constant, so the same trick cannot be smuggled into a
    /// constant later. The composite policy itself belongs in Configs.cs:
    /// AddPolicy("RequireAdminOrDoctor", p => p.RequireRole(Roles.Admin, Roles.Doctor)),
    /// exactly as RequireClinicStaff already does for Admin-or-Receptionist.
    /// </summary>
    [Fact]
    public void PolicyNames_ContainNoBooleanOperators()
    {
        var names = typeof(Policies)
            .GetFields()
            .Where(f => f is { IsLiteral: true, FieldType: var t } && t == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(names);

        foreach (var name in names)
        {
            Assert.DoesNotContain(" ", name);
            Assert.DoesNotContain("or", name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("&", name);
            Assert.DoesNotContain("|", name);
        }
    }

    [Fact]
    public void RequireClinicStaff_ExistsForFrontDeskEndpoints()
    {
        // Sprint 3 review 3.7: PatientsController locks the Receptionist out with
        // RequireAdmin, while README 201-202 and 939 require Admin *and* Receptionist to
        // register, update and view patients. The correct policy is already registered and
        // currently unused.
        Assert.Equal("RequireClinicStaff", Policies.RequireClinicStaff);
    }
}
