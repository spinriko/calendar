using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

public record ApproveAbsenceRequestDto(
    [Required]
    [property: JsonPropertyName("approverId")]
    int ApproverId,
    [StringLength(1000)]
    [property: JsonPropertyName("comments")]
    string? Comments
);
