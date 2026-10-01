using System.Security.Claims;
using Clinic.Application.Common;
using Clinic.Application.DTOs.Doctors;
using Clinic.Application.DTOs.Patients;
using Clinic.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }
   

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    [ProducesResponseType(typeof(PagedResult<PatientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<PatientResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,string? search = null, int page = 1, int pageSize = 10)
    {
        var result = await _patientService.GetAllAsync(includeInactive, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientResponse>> GetById(Guid id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        if(User.FindFirstValue(ClaimTypes.NameIdentifier) != patient.Id.ToString())
        {
            return Forbid();
        }
        return Ok(patient);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PatientResponse>> Create(
        [FromBody] CreatePatientRequest request)
    {
        var created = await _patientService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PatientResponse>> Update(
        Guid id, [FromBody] UpdatePatientRequest request)
    {
        var updated = await _patientService.UpdateAsync(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _patientService.DeactivateAsync(id);
        return NoContent();
    }
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Policies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _patientService.ActivateAsync(id);
        return NoContent();
    }
}
