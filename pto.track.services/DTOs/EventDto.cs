using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

/// <summary>
/// Data transfer object representing a calendar event.
/// </summary>
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
