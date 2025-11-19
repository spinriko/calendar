using Microsoft.AspNetCore.Mvc;
using pto.track.data;
using pto.track.services;
using pto.track.services.DTOs;

namespace pto.track.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AbsencesController : ControllerBase
{
    private readonly IAbsenceService _absenceService;
    private readonly ILogger<AbsencesController> _logger;

    public AbsencesController(IAbsenceService absenceService, ILogger<AbsencesController> logger)
    {
        _absenceService = absenceService;
        _logger = logger;
    }

    // GET: api/Absences
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceRequestDto>>> GetAbsenceRequests(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] int? employeeId,
        [FromQuery] string? status)
    {
        _logger.LogDebug("GetAbsenceRequests called with start={Start}, end={End}, employeeId={EmployeeId}, status={Status}", start, end, employeeId, status);

        // Parse status parameter
        AbsenceStatus? absenceStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AbsenceStatus>(status, true, out var parsedStatus))
        {
            absenceStatus = parsedStatus;
        }

        if (employeeId.HasValue)
        {
            var empStart = start ?? DateTime.UtcNow.AddMonths(-3);
            var empEnd = end ?? DateTime.UtcNow.AddMonths(3);
            var absences = (await _absenceService.GetAbsenceRequestsByEmployeeAsync(employeeId.Value, empStart, empEnd)).ToList();
            _logger.LogDebug("Returning {Count} absences for employee {EmployeeId}", absences.Count, employeeId);
            return Ok(absences);
        }

        if (start.HasValue && end.HasValue)
        {
            var absences = (await _absenceService.GetAbsenceRequestsAsync(start.Value, end.Value, absenceStatus)).ToList();
            _logger.LogDebug("Returning {Count} absences for date range", absences.Count);
            return Ok(absences);
        }

        _logger.LogDebug("Bad request - missing required parameters");
        return BadRequest("Either provide start and end dates, or provide employeeId");
    }

    // GET: api/Absences/pending
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<AbsenceRequestDto>>> GetPendingAbsenceRequests()
    {
        _logger.LogDebug("GetPendingAbsenceRequests called");
        var absences = (await _absenceService.GetPendingAbsenceRequestsAsync()).ToList();
        _logger.LogDebug("Returning {Count} pending absences", absences.Count);
        return Ok(absences);
    }

    // GET: api/Absences/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AbsenceRequestDto>> GetAbsenceRequest(Guid id)
    {
        _logger.LogDebug("GetAbsenceRequest called with id={Id}", id);
        var absence = await _absenceService.GetAbsenceRequestByIdAsync(id);
        if (absence == null)
        {
            _logger.LogDebug("Absence {Id} not found", id);
            return NotFound();
        }

        return Ok(absence);
    }

    // POST: api/Absences
    [HttpPost]
    public async Task<ActionResult<AbsenceRequestDto>> PostAbsenceRequest(CreateAbsenceRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogDebug("Invalid model state for CreateAbsenceRequestDto: {ModelState}", ModelState);
            return BadRequest(ModelState);
        }

        var created = await _absenceService.CreateAbsenceRequestAsync(dto);
        _logger.LogDebug("Absence request created with id={Id}", created.Id);
        return CreatedAtAction(nameof(GetAbsenceRequest), new { id = created.Id }, created);
    }

    // PUT: api/Absences/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAbsenceRequest(Guid id, UpdateAbsenceRequestDto dto)
    {
        _logger.LogDebug("PutAbsenceRequest called with id={Id}", id);
        if (!ModelState.IsValid)
        {
            _logger.LogDebug("Invalid ModelState for PutAbsenceRequest");
            return BadRequest(ModelState);
        }

        var success = await _absenceService.UpdateAbsenceRequestAsync(id, dto);
        if (!success)
        {
            _logger.LogDebug("Absence {Id} not found or update failed", id);
            return NotFound("Absence request not found or cannot be updated (only pending requests can be modified)");
        }

        return NoContent();
    }

    // POST: api/Absences/5/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveAbsenceRequest(Guid id, ApproveAbsenceRequestDto dto)
    {
        _logger.LogDebug("ApproveAbsenceRequest called with id={Id}, approverId={ApproverId}", id, dto.ApproverId);
        if (!ModelState.IsValid)
        {
            _logger.LogDebug("Invalid ModelState for ApproveAbsenceRequest");
            return BadRequest(ModelState);
        }

        var success = await _absenceService.ApproveAbsenceRequestAsync(id, dto);
        if (!success)
        {
            _logger.LogDebug("Absence {Id} not found or approval failed", id);
            return NotFound("Absence request not found or already processed");
        }

        return NoContent();
    }

    // POST: api/Absences/5/reject
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectAbsenceRequest(Guid id, RejectAbsenceRequestDto dto)
    {
        _logger.LogDebug("RejectAbsenceRequest called with id={Id}, approverId={ApproverId}", id, dto.ApproverId);
        if (!ModelState.IsValid)
        {
            _logger.LogDebug("Invalid ModelState for RejectAbsenceRequest");
            return BadRequest(ModelState);
        }

        var success = await _absenceService.RejectAbsenceRequestAsync(id, dto);
        if (!success)
        {
            _logger.LogDebug("Absence {Id} not found or rejection failed", id);
            return NotFound("Absence request not found or already processed");
        }

        return NoContent();
    }

    // POST: api/Absences/5/cancel
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelAbsenceRequest(Guid id, [FromQuery] int employeeId)
    {
        _logger.LogDebug("CancelAbsenceRequest called with id={Id}, employeeId={EmployeeId}", id, employeeId);
        var success = await _absenceService.CancelAbsenceRequestAsync(id, employeeId);
        if (!success)
        {
            _logger.LogDebug("Absence {Id} not found or cancel failed", id);
            return NotFound("Absence request not found or cannot be cancelled");
        }

        return NoContent();
    }

    // DELETE: api/Absences/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAbsenceRequest(Guid id)
    {
        _logger.LogDebug("DeleteAbsenceRequest called with id={Id}", id);
        var success = await _absenceService.DeleteAbsenceRequestAsync(id);
        if (!success)
        {
            _logger.LogDebug("Absence {Id} not found or delete failed", id);
            return NotFound();
        }

        _logger.LogDebug("Absence {Id} deleted successfully", id);
        return NoContent();
    }
}
