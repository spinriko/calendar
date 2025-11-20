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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of absence requests.</returns>
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsAsync(DateTime start, DateTime end, AbsenceStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all absence requests for a specific employee within a date range.
    /// </summary>
    /// <param name="employeeId">The employee's resource ID.</param>
    /// <param name="start">Start date of the range.</param>
    /// <param name="end">End date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of absence requests for the specified employee.</returns>
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsByEmployeeAsync(int employeeId, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all absence requests with Pending status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of pending absence requests.</returns>
    Task<IEnumerable<AbsenceRequestDto>> GetPendingAbsenceRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific absence request by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The absence request if found; otherwise, null.</returns>
    Task<AbsenceRequestDto?> GetAbsenceRequestByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new absence request.
    /// </summary>
    /// <param name="dto">The absence request creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created absence request.</returns>
    Task<AbsenceRequestDto> CreateAbsenceRequestAsync(CreateAbsenceRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing absence request (only allowed for Pending requests).
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="dto">The updated absence request data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateAbsenceRequestAsync(Guid id, UpdateAbsenceRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves an absence request.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="dto">The approval data including approver ID and optional comments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ApproveAbsenceRequestAsync(Guid id, ApproveAbsenceRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects an absence request.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="dto">The rejection data including approver ID and reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RejectAbsenceRequestAsync(Guid id, RejectAbsenceRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an absence request (only allowed by the employee who created it).
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="employeeId">The employee's resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> CancelAbsenceRequestAsync(Guid id, int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an absence request permanently.
    /// </summary>
    /// <param name="id">The unique identifier of the absence request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteAbsenceRequestAsync(Guid id, CancellationToken cancellationToken = default);
}
