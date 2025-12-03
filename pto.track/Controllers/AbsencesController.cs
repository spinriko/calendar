using Microsoft.AspNetCore.Mvc;
using pto.track.data;
using pto.track.services;
using pto.track.services.Authentication;
using pto.track.services.DTOs;

namespace pto.track.Controllers;

/// <summary>
/// API controller for managing employee absence requests.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AbsencesController : ControllerBase
{
    private readonly IAbsenceService _absenceService;
    private readonly ILogger<AbsencesController> _logger;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IUserSyncService _userSync;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbsencesController"/> class.
    /// </summary>
    /// <param name="absenceService">The absence service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="claimsProvider">The user claims provider.</param>
    /// <param name="userSync">The user synchronization service.</param>
    public AbsencesController(
        IAbsenceService absenceService,
        ILogger<AbsencesController> logger,
        IUserClaimsProvider claimsProvider,
        IUserSyncService userSync)
    {
        _absenceService = absenceService;
        _logger = logger;
        _claimsProvider = claimsProvider;
        _userSync = userSync;
    }

    /// <summary>
    /// Retrieves absence requests based on filter criteria.
    /// </summary>
    /// <param name="start">Optional start date for filtering.</param>
    /// <param name="end">Optional end date for filtering.</param>
    /// <param name="employeeId">Optional employee ID for filtering.</param>
    /// <param name="status">Optional status values for filtering (can specify multiple using status[]=value notation).</param>
    /// <returns>A collection of absence requests matching the criteria.</returns>
    // GET: api/Absences
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceRequestDto>>> GetAbsenceRequests(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] int? employeeId,
        [FromQuery(Name = "status[]")] List<string>? status)
    {
        _logger.LogDebug("GetAbsenceRequests called with start={Start}, end={End}, employeeId={EmployeeId}, status={Status}",
            start, end, employeeId, status != null ? string.Join(",", status) : "null");

        var absenceStatuses = ParseStatusParameters(status);

        if (employeeId.HasValue)
        {
            var empStart = start ?? DateTime.UtcNow.AddMonths(-3);
            var empEnd = end ?? DateTime.UtcNow.AddMonths(3);
            var absences = (await _absenceService.GetAbsenceRequestsByEmployeeAsync(employeeId.Value, empStart, empEnd, absenceStatuses)).ToList();
            _logger.LogDebug("Returning {Count} absences for employee {EmployeeId}", absences.Count, employeeId);
            return Ok(absences);
        }

        if (start.HasValue && end.HasValue)
        {
            var absences = (await _absenceService.GetAbsenceRequestsAsync(start.Value, end.Value, absenceStatuses)).ToList();
            _logger.LogDebug("Returning {Count} absences for date range", absences.Count);
            return Ok(absences);
        }

        _logger.LogDebug("Bad request - missing required parameters");
        return BadRequest("Either provide start and end dates, or provide employeeId");
    }

    /// <summary>
    /// Parses status parameters from query string into AbsenceStatus enum values.
    /// </summary>
    /// <param name="status">The list of status strings to parse.</param>
    /// <returns>A list of parsed AbsenceStatus values, or null if no valid statuses were provided.</returns>
    private List<AbsenceStatus>? ParseStatusParameters(List<string>? status)
    {
        if (status == null || !status.Any())
        {
            _logger.LogInformation("No status filters provided, will return all statuses");
            return null;
        }

        _logger.LogInformation("Parsing {Count} status values: [{Statuses}]", status.Count, string.Join(", ", status));
        var absenceStatuses = new List<AbsenceStatus>();

        foreach (var s in status)
        {
            if (Enum.TryParse<AbsenceStatus>(s, true, out var parsedStatus))
            {
                absenceStatuses.Add(parsedStatus);
                _logger.LogInformation("Parsed status: {Status} -> {ParsedStatus}", s, parsedStatus);
            }
            else
            {
                _logger.LogWarning("Failed to parse status: {Status}", s);
            }
        }

        if (absenceStatuses.Count == 0)
        {
            _logger.LogInformation("No valid statuses parsed, will return all statuses");
            return null;
        }

        _logger.LogInformation("Total parsed statuses: {Count} - [{Statuses}]",
            absenceStatuses.Count, string.Join(", ", absenceStatuses));
        return absenceStatuses;
    }

    /// <summary>
    /// Retrieves all pending absence requests.
    /// </summary>
    /// <returns>A collection of pending absence requests.</returns>
    // GET: api/Absences/pending
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<AbsenceRequestDto>>> GetPendingAbsenceRequests()
    {
        _logger.LogDebug("GetPendingAbsenceRequests called");
        var absences = (await _absenceService.GetPendingAbsenceRequestsAsync()).ToList();
        _logger.LogDebug("Returning {Count} pending absences", absences.Count);
        return Ok(absences);
    }

    /// <summary>
    /// Retrieves a specific absence request by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <returns>The absence request with the specified ID, or NotFound if it doesn't exist.</returns>
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

    /// <summary>
    /// Creates a new absence request.
    /// </summary>
    /// <param name="dto">The absence request data to create.</param>
    /// <returns>The created absence request with a location header pointing to the new resource.</returns>
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

    /// <summary>
    /// Updates an existing absence request. Only the employee who created the request can update it, and only if it's still pending.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request to update.</param>
    /// <param name="dto">The updated absence request data.</param>
    /// <returns>NoContent if successful, NotFound if the request doesn't exist, Forbid if the user is not authorized.</returns>
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

        // Authorization: User can only update their own pending requests
        var currentUserId = await _userSync.GetCurrentUserResourceIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Unauthorized update attempt - no authenticated user");
            return Unauthorized("User is not authenticated");
        }

        var existingRequest = await _absenceService.GetAbsenceRequestByIdAsync(id);
        if (existingRequest == null)
        {
            _logger.LogDebug("Absence {Id} not found", id);
            return NotFound("Absence request not found");
        }

        if (existingRequest.EmployeeId != currentUserId.Value)
        {
            _logger.LogWarning("User {UserId} attempted to update absence {AbsenceId} belonging to employee {EmployeeId}",
                currentUserId, id, existingRequest.EmployeeId);
            return Forbid();
        }

        var result = await _absenceService.UpdateAbsenceRequestAsync(id, dto);
        if (!result.Success)
        {
            _logger.LogDebug("Absence {Id} update failed (only pending requests can be modified)", id);
            return BadRequest("Only pending requests can be modified");
        }

        return NoContent();
    }

    /// <summary>
    /// Approves an absence request. Only users with Manager, Approver, or Admin roles can approve requests.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request to approve.</param>
    /// <param name="dto">The approval data including approver ID and optional comments.</param>
    /// <returns>NoContent if successful, NotFound if the request doesn't exist, Forbid if the user is not authorized.</returns>
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

        // Authorization: Only managers/approvers can approve requests
        if (!_claimsProvider.IsInRole("Manager") && !_claimsProvider.IsInRole("Approver") && !_claimsProvider.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized approval attempt by user without Manager/Approver role");
            return Forbid();
        }

        // Verify the approver ID matches the current user
        var currentUserId = await _userSync.GetCurrentUserResourceIdAsync();
        if (!currentUserId.HasValue || currentUserId.Value != dto.ApproverId)
        {
            _logger.LogWarning("ApproverId mismatch: current user {CurrentUserId}, provided {ProvidedId}",
                currentUserId, dto.ApproverId);
            return BadRequest("Approver ID must match the authenticated user");
        }

        var result = await _absenceService.ApproveAbsenceRequestAsync(id, dto);
        if (!result.Success)
        {
            _logger.LogDebug("Absence {Id} not found or approval failed", id);
            return NotFound("Absence request not found or already processed");
        }

        return NoContent();
    }

    /// <summary>
    /// Rejects an absence request. Only users with Manager, Approver, or Admin roles can reject requests.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request to reject.</param>
    /// <param name="dto">The rejection data including approver ID and optional comments.</param>
    /// <returns>NoContent if successful, NotFound if the request doesn't exist, Forbid if the user is not authorized.</returns>
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

        // Authorization: Only managers/approvers can reject requests
        if (!_claimsProvider.IsInRole("Manager") && !_claimsProvider.IsInRole("Approver") && !_claimsProvider.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized rejection attempt by user without Manager/Approver role");
            return Forbid();
        }

        // Verify the approver ID matches the current user
        var currentUserId = await _userSync.GetCurrentUserResourceIdAsync();
        if (!currentUserId.HasValue || currentUserId.Value != dto.ApproverId)
        {
            _logger.LogWarning("ApproverId mismatch: current user {CurrentUserId}, provided {ProvidedId}",
                currentUserId, dto.ApproverId);
            return BadRequest("Approver ID must match the authenticated user");
        }

        var result = await _absenceService.RejectAbsenceRequestAsync(id, dto);
        if (!result.Success)
        {
            _logger.LogDebug("Absence {Id} not found or rejection failed", id);
            return NotFound("Absence request not found or already processed");
        }

        return NoContent();
    }

    /// <summary>
    /// Cancels an absence request. Only the employee who created the request can cancel it.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request to cancel.</param>
    /// <param name="employeeId">The ID of the employee cancelling the request.</param>
    /// <returns>NoContent if successful, NotFound if the request doesn't exist, Forbid if the user is not authorized.</returns>
    // POST: api/Absences/5/cancel
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelAbsenceRequest(Guid id, [FromQuery] int employeeId)
    {
        _logger.LogDebug("CancelAbsenceRequest called with id={Id}, employeeId={EmployeeId}", id, employeeId);

        // Authorization: User can only cancel their own requests
        var currentUserId = await _userSync.GetCurrentUserResourceIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Unauthorized cancel attempt - no authenticated user");
            return Unauthorized("User is not authenticated");
        }

        if (currentUserId.Value != employeeId)
        {
            _logger.LogWarning("User {UserId} attempted to cancel request belonging to employee {EmployeeId}",
                currentUserId, employeeId);
            return Forbid();
        }

        var result = await _absenceService.CancelAbsenceRequestAsync(id, employeeId);
        if (!result.Success)
        {
            _logger.LogDebug("Absence {Id} not found or cancel failed", id);
            return NotFound("Absence request not found or cannot be cancelled");
        }

        return NoContent();
    }

    /// <summary>
    /// Permanently deletes an absence request from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request to delete.</param>
    /// <returns>NoContent if successful, NotFound if the request doesn't exist.</returns>
    // DELETE: api/Absences/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAbsenceRequest(Guid id)
    {
        _logger.LogDebug("DeleteAbsenceRequest called with id={Id}", id);
        var result = await _absenceService.DeleteAbsenceRequestAsync(id);
        if (!result.Success)
        {
            _logger.LogDebug("Absence {Id} not found or delete failed", id);
            return NotFound();
        }

        _logger.LogDebug("Absence {Id} deleted successfully", id);
        return NoContent();
    }
}
