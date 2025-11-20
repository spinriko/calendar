using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

/// <summary>
/// Data transfer object for rejecting an absence request.
/// </summary>
public record RejectAbsenceRequestDto(
    [Required]
    [property: JsonPropertyName("approverId")]
    int ApproverId,
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    [property: JsonPropertyName("reason")]
    string Reason
);
