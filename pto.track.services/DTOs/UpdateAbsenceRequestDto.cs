using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

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
