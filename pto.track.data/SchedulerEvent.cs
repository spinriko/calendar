using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace pto.track.data;

public class SchedulerEvent : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    [StringLength(200)]
    public string? Text { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [JsonPropertyName("resource")]
    [Range(1, int.MaxValue, ErrorMessage = "ResourceId must be a positive integer")]
    public int ResourceId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End <= Start)
        {
            yield return new ValidationResult("End must be greater than Start.", new[] { nameof(End), nameof(Start) });
        }
    }
}
