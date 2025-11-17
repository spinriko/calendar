using System.ComponentModel.DataAnnotations;

namespace pto.track.services;

public record EventDto(
    int Id,
    DateTime Start,
    DateTime End,
    string? Text,
    string? Color,
    int ResourceId
);

public record CreateEventDto(
    [Required]
    DateTime Start,
    [Required]
    DateTime End,
    [StringLength(200)]
    string? Text,
    [StringLength(50)]
    string? Color,
    [Range(1, int.MaxValue, ErrorMessage = "ResourceId must be a positive integer")]
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
    DateTime Start,
    [Required]
    DateTime End,
    [StringLength(200)]
    string? Text,
    [StringLength(50)]
    string? Color,
    [Range(1, int.MaxValue, ErrorMessage = "ResourceId must be a positive integer")]
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
