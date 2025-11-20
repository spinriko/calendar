using System.Text.Json.Serialization;

namespace pto.track.services.DTOs;

public record AbsenceRequestDto(
    [property: JsonPropertyName("id")]
    Guid Id,
    [property: JsonPropertyName("start")]
    DateTime Start,
    [property: JsonPropertyName("end")]
    DateTime End,
    [property: JsonPropertyName("reason")]
    string Reason,
    [property: JsonPropertyName("employeeId")]
    int EmployeeId,
    [property: JsonPropertyName("employeeName")]
    string EmployeeName,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("requestedDate")]
    DateTime RequestedDate,
    [property: JsonPropertyName("approverId")]
    int? ApproverId,
    [property: JsonPropertyName("approverName")]
    string? ApproverName,
    [property: JsonPropertyName("approvedDate")]
    DateTime? ApprovedDate,
    [property: JsonPropertyName("approvalComments")]
    string? ApprovalComments
);
