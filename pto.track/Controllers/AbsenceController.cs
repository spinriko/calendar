using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.DTOs;

namespace pto.track.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AbsenceController : ControllerBase
{
    private readonly IAbsenceService _absenceService;

    public AbsenceController(IAbsenceService absenceService)
    {
        _absenceService = absenceService;
    }

    // GET: api/Absence
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceRequestDto>>> GetAbsenceRequests(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] int? employeeId)
    {
        if (employeeId.HasValue)
        {
            var empStart = start ?? DateTime.UtcNow.AddMonths(-3);
            var empEnd = end ?? DateTime.UtcNow.AddMonths(3);
            var absences = await _absenceService.GetAbsenceRequestsByEmployeeAsync(employeeId.Value, empStart, empEnd);
            return Ok(absences);
        }

        if (start.HasValue && end.HasValue)
        {
            var absences = await _absenceService.GetAbsenceRequestsAsync(start.Value, end.Value);
            return Ok(absences);
        }

        return BadRequest("Either provide start and end dates, or provide employeeId");
    }

    // GET: api/Absence/pending
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<AbsenceRequestDto>>> GetPendingAbsenceRequests()
    {
        var absences = await _absenceService.GetPendingAbsenceRequestsAsync();
        return Ok(absences);
    }

    // GET: api/Absence/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AbsenceRequestDto>> GetAbsenceRequest(int id)
    {
        var absence = await _absenceService.GetAbsenceRequestByIdAsync(id);
        if (absence == null)
        {
            return NotFound();
        }

        return Ok(absence);
    }

    // POST: api/Absence
    [HttpPost]
    public async Task<ActionResult<AbsenceRequestDto>> PostAbsenceRequest(CreateAbsenceRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _absenceService.CreateAbsenceRequestAsync(dto);
        return CreatedAtAction(nameof(GetAbsenceRequest), new { id = created.Id }, created);
    }

    // PUT: api/Absence/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAbsenceRequest(int id, UpdateAbsenceRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _absenceService.UpdateAbsenceRequestAsync(id, dto);
        if (!success)
        {
            return NotFound("Absence request not found or cannot be updated (only pending requests can be modified)");
        }

        return NoContent();
    }

    // POST: api/Absence/5/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveAbsenceRequest(int id, ApproveAbsenceRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _absenceService.ApproveAbsenceRequestAsync(id, dto);
        if (!success)
        {
            return NotFound("Absence request not found or already processed");
        }

        return NoContent();
    }

    // POST: api/Absence/5/reject
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectAbsenceRequest(int id, RejectAbsenceRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _absenceService.RejectAbsenceRequestAsync(id, dto);
        if (!success)
        {
            return NotFound("Absence request not found or already processed");
        }

        return NoContent();
    }

    // POST: api/Absence/5/cancel
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelAbsenceRequest(int id, [FromQuery] int employeeId)
    {
        var success = await _absenceService.CancelAbsenceRequestAsync(id, employeeId);
        if (!success)
        {
            return NotFound("Absence request not found or cannot be cancelled");
        }

        return NoContent();
    }

    // DELETE: api/Absence/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAbsenceRequest(int id)
    {
        var success = await _absenceService.DeleteAbsenceRequestAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
