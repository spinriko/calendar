using pto.track.services.DTOs;

namespace pto.track.services;

public interface IAbsenceService
{
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsAsync(DateTime start, DateTime end);
    Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsByEmployeeAsync(int employeeId, DateTime start, DateTime end);
    Task<IEnumerable<AbsenceRequestDto>> GetPendingAbsenceRequestsAsync();
    Task<AbsenceRequestDto?> GetAbsenceRequestByIdAsync(int id);
    Task<AbsenceRequestDto> CreateAbsenceRequestAsync(CreateAbsenceRequestDto dto);
    Task<bool> UpdateAbsenceRequestAsync(int id, UpdateAbsenceRequestDto dto);
    Task<bool> ApproveAbsenceRequestAsync(int id, ApproveAbsenceRequestDto dto);
    Task<bool> RejectAbsenceRequestAsync(int id, RejectAbsenceRequestDto dto);
    Task<bool> CancelAbsenceRequestAsync(int id, int employeeId);
    Task<bool> DeleteAbsenceRequestAsync(int id);
}
