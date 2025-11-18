using System.ComponentModel.DataAnnotations;

namespace pto.track.data;

public enum AbsenceStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public class AbsenceRequest : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    [Required]
    [StringLength(500)]
    public required string Reason { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public SchedulerResource? Employee { get; set; }

    [Required]
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Pending;

    [Required]
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

    public int? ApproverId { get; set; }

    public SchedulerResource? Approver { get; set; }

    public DateTime? ApprovedDate { get; set; }

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
