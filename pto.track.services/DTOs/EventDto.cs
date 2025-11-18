using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

public record EventDto(
    [property: JsonPropertyName("id")]
    Guid Id,
    [property: JsonPropertyName("start")]
    DateTime Start,
    [property: JsonPropertyName("end")]
    DateTime End,
    [property: JsonPropertyName("text")]
    string? Text,
    [property: JsonPropertyName("color")]
    string? Color,
    [property: JsonPropertyName("resource")]
    int ResourceId
);

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

public record UpdateEventDto(
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
