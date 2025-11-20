using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

/// <summary>
/// Data transfer object for creating a new calendar event.
/// </summary>
public record CreateEventDto(
    [Required]
    [property: JsonPropertyName("start")]
    DateTime Start,
    [Required]
    [property: JsonPropertyName("end")]
    DateTime End,
    [StringLength(200)]
    [property: JsonPropertyName("text")]
    string? Text,
    [StringLength(50)]
    [property: JsonPropertyName("color")]
    string? Color,
    [Range(1, int.MaxValue, ErrorMessage = "ResourceId must be a positive integer")]
    [property: JsonPropertyName("resource")]
    int ResourceId
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
