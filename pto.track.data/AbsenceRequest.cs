using System.ComponentModel.DataAnnotations;

namespace pto.track.data;

/// <summary>
/// Represents the status of an absence request.
/// </summary>
public enum AbsenceStatus
{
    /// <summary>
    /// The absence request is pending approval.
    /// </summary>
    Pending,

    /// <summary>
    /// The absence request has been approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The absence request has been rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// The absence request has been cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents an employee absence request in the system.
/// </summary>
public class AbsenceRequest : IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the absence request.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the start date and time of the absence period.
    /// </summary>
    [Required]
    public DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets the end date and time of the absence period.
    /// </summary>
    [Required]
    public DateTime End { get; set; }

    /// <summary>
    /// Gets or sets the reason for the absence request.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the ID of the employee making the absence request.
    /// </summary>
    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the employee making the request.
    /// </summary>
    public Resource? Employee { get; set; }

    /// <summary>
    /// Gets or sets the current status of the absence request.
    /// </summary>
    [Required]
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Pending;

    /// <summary>
    /// Gets or sets the date when the absence request was submitted.
    /// </summary>
    [Required]
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the approver who processed the request.
    /// </summary>
    public int? ApproverId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the approver who processed the request.
    /// </summary>
    public Resource? Approver { get; set; }

    /// <summary>
    /// Gets or sets the date when the absence request was approved or rejected.
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Gets or sets any comments provided by the approver during the approval process.
    /// </summary>
    [StringLength(1000)]
    public string? ApprovalComments { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
        {
            yield return new ValidationResult("End must be greater than Start.", new[] { nameof(End), nameof(Start) });
        }

        if (Start.Date < DateTime.UtcNow.Date && Status == AbsenceStatus.Pending)
        {
            yield return new ValidationResult("Cannot request absence for past dates.", new[] { nameof(Start) });
        }
    }
}
