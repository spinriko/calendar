using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

public record AbsenceRequestDto(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("start")]
    DateTime Start,
    [property: JsonPropertyName("end")]
    DateTime End,
    [property: JsonPropertyName("reason")]
    string Reason,
    [property: JsonPropertyName("employeeId")]
    int EmployeeId,
    [property: JsonPropertyName("employeeName")]
    string EmployeeName,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("requestedDate")]
    DateTime RequestedDate,
    [property: JsonPropertyName("approverId")]
    int? ApproverId,
    [property: JsonPropertyName("approverName")]
    string? ApproverName,
    [property: JsonPropertyName("approvedDate")]
    DateTime? ApprovedDate,
    [property: JsonPropertyName("approvalComments")]
    string? ApprovalComments
);

public record CreateAbsenceRequestDto(
    [Required]
    [property: JsonPropertyName("start")]
    DateTime Start,
    [Required]
    [property: JsonPropertyName("end")]
    DateTime End,
    [Required]
    [StringLength(500, MinimumLength = 3)]
    [property: JsonPropertyName("reason")]
    string Reason,
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer")]
    [property: JsonPropertyName("employeeId")]
    int EmployeeId
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
        {
            yield return new ValidationResult("End must be greater than Start.", new[] { nameof(End), nameof(Start) });
        }

        if (Start.Date < DateTime.UtcNow.Date)
        {
            yield return new ValidationResult("Cannot request absence for past dates.", new[] { nameof(Start) });
        }
    }
}

public record UpdateAbsenceRequestDto(
    [Required]
    [property: JsonPropertyName("start")]
    DateTime Start,
    [Required]
    [property: JsonPropertyName("end")]
    DateTime End,
    [Required]
    [StringLength(500, MinimumLength = 3)]
    [property: JsonPropertyName("reason")]
    string Reason
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
        {
            yield return new ValidationResult("End must be greater than Start.", new[] { nameof(End), nameof(Start) });
        }
    }
}

public record ApproveAbsenceRequestDto(
    [Required]
    [property: JsonPropertyName("approverId")]
    int ApproverId,
    [StringLength(1000)]
    [property: JsonPropertyName("comments")]
    string? Comments
);

public record RejectAbsenceRequestDto(
    [Required]
    [property: JsonPropertyName("approverId")]
    int ApproverId,
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    [property: JsonPropertyName("reason")]
    string Reason
);
