using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace pto.track.data;

/// <summary>
/// Represents a scheduled event in the calendar system.
/// </summary>
public class SchedulerEvent : IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the start date and time of the event.
    /// </summary>
    [Required]
    public DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets the end date and time of the event.
    /// </summary>
    [Required]
    public DateTime End { get; set; }

    /// <summary>
    /// Gets or sets the description or title of the event.
    /// </summary>
    [StringLength(200)]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the color code for displaying the event in the calendar.
    /// </summary>
    [StringLength(50)]
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the ID of the resource assigned to this event.
    /// </summary>
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
