using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services;

/// <summary>
/// Service for managing absence requests (PTO, vacation, sick leave, etc.).
/// </summary>
public interface IAbsenceService
{
    /// <summary>
    /// Gets absence requests within a date range, optionally filtered by status.
    /// </summary>
    /// <param name="start">Start date of the range.</param>
    /// <param name="end">End date of the range.</param>
    /// <param name="status">Optional status filter (Pending, Approved, Rejected, Cancelled).</param>
    /// <returns>A collection of absence requests.</returns>
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsAsync(DateTime start, DateTime end, AbsenceStatus? status = null);

    /// <summary>
    /// Gets all absence requests for a specific employee within a date range.
    /// </summary>
    /// <param name="employeeId">The employee's resource ID.</param>
    /// <param name="start">Start date of the range.</param>
    /// <param name="end">End date of the range.</param>
    /// <returns>A collection of absence requests for the specified employee.</returns>
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsByEmployeeAsync(int employeeId, DateTime start, DateTime end);

    /// <summary>
    /// Gets all absence requests with Pending status.
    /// </summary>
    /// <returns>A collection of pending absence requests.</returns>
    Task<IEnumerable<AbsenceRequestDto>> GetPendingAbsenceRequestsAsync();

    /// <summary>
    /// Gets a specific absence request by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <returns>The absence request if found; otherwise, null.</returns>
    Task<AbsenceRequestDto?> GetAbsenceRequestByIdAsync(Guid id);

    /// <summary>
    /// Creates a new absence request.
    /// </summary>
    /// <param name="dto">The absence request creation data.</param>
    /// <returns>The created absence request.</returns>
    Task<AbsenceRequestDto> CreateAbsenceRequestAsync(CreateAbsenceRequestDto dto);

    /// <summary>
    /// Updates an existing absence request (only allowed for Pending requests).
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="dto">The updated absence request data.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateAbsenceRequestAsync(Guid id, UpdateAbsenceRequestDto dto);

    /// <summary>
    /// Approves an absence request.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="dto">The approval data including approver ID and optional comments.</param>
    /// <returns>True if the approval was successful; otherwise, false.</returns>
    Task<bool> ApproveAbsenceRequestAsync(Guid id, ApproveAbsenceRequestDto dto);

    /// <summary>
    /// Rejects an absence request.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="dto">The rejection data including approver ID and reason.</param>
    /// <returns>True if the rejection was successful; otherwise, false.</returns>
    Task<bool> RejectAbsenceRequestAsync(Guid id, RejectAbsenceRequestDto dto);

    /// <summary>
    /// Cancels an absence request (only allowed by the employee who created it).
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="employeeId">The employee's resource ID.</param>
    /// <returns>True if the cancellation was successful; otherwise, false.</returns>
    Task<bool> CancelAbsenceRequestAsync(Guid id, int employeeId);

    /// <summary>
    /// Deletes an absence request permanently.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteAbsenceRequestAsync(Guid id);
}
