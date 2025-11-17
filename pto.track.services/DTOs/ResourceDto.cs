using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

public record ResourceDto(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("name")]
    string Name
);
