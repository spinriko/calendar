using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

/// <summary>
/// Data transfer object for creating a new absence request.
/// </summary>
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
