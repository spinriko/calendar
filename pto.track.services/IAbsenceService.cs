using pto.track.services.DTOs;

namespace pto.track.services;

public interface IAbsenceService
{
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsAsync(DateTime start, DateTime end);
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsByEmployeeAsync(int employeeId, DateTime start, DateTime end);
    Task<IEnumerable<AbsenceRequestDto>> GetPendingAbsenceRequestsAsync();
    Task<AbsenceRequestDto?> GetAbsenceRequestByIdAsync(Guid id);
    Task<AbsenceRequestDto> CreateAbsenceRequestAsync(CreateAbsenceRequestDto dto);
    Task<bool> UpdateAbsenceRequestAsync(Guid id, UpdateAbsenceRequestDto dto);
    Task<bool> ApproveAbsenceRequestAsync(Guid id, ApproveAbsenceRequestDto dto);
    Task<bool> RejectAbsenceRequestAsync(Guid id, RejectAbsenceRequestDto dto);
    Task<bool> CancelAbsenceRequestAsync(Guid id, int employeeId);
    Task<bool> DeleteAbsenceRequestAsync(Guid id);
}
