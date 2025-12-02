using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

/// <summary>
/// Data transfer object representing a group.
/// </summary>
public record GroupDto(
    [property: JsonPropertyName("groupId")]
    int GroupId,
    [property: JsonPropertyName("name")]
    string Name
);

/// <summary>
/// Data transfer object for creating a new group.
/// </summary>
public record CreateGroupDto(
    [property: JsonPropertyName("name")]
    string Name
);

/// <summary>
/// Data transfer object for updating an existing group.
/// </summary>
public record UpdateGroupDto(
    [property: JsonPropertyName("name")]
    string Name
);
