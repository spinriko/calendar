using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

public record ResourceDto(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("email")]
    string? Email,
    [property: JsonPropertyName("employeeNumber")]
    string? EmployeeNumber,
    [property: JsonPropertyName("role")]
    string Role,
    [property: JsonPropertyName("isApprover")]
    bool IsApprover,
    [property: JsonPropertyName("isActive")]
    bool IsActive,
    [property: JsonPropertyName("department")]
    string? Department
);
