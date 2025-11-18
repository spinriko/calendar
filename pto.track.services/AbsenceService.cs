using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services;

public class AbsenceService : IAbsenceService
{
    private readonly PtoTrackDbContext _context;

    public AbsenceService(PtoTrackDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsAsync(DateTime start, DateTime end)
    {
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.Start < end && a.End > start)
            .AsNoTracking()
            .ToListAsync();

        return absences.Select(MapToDto);
    }

    public async Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsByEmployeeAsync(int employeeId, DateTime start, DateTime end)
    {
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.EmployeeId == employeeId && a.Start < end && a.End > start)
            .AsNoTracking()
            .ToListAsync();

        return absences.Select(MapToDto);
    }

    public async Task<IEnumerable<AbsenceRequestDto>> GetPendingAbsenceRequestsAsync()
    {
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.Status == AbsenceStatus.Pending)
            .OrderBy(a => a.RequestedDate)
            .AsNoTracking()
            .ToListAsync();

        return absences.Select(MapToDto);
    }

    public async Task<AbsenceRequestDto?> GetAbsenceRequestByIdAsync(int id)
    {
        var absence = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        return absence == null ? null : MapToDto(absence);
    }

    public async Task<AbsenceRequestDto> CreateAbsenceRequestAsync(CreateAbsenceRequestDto dto)
    {
        var absence = new AbsenceRequest
        {
            Start = dto.Start,
            End = dto.End,
            Reason = dto.Reason,
            EmployeeId = dto.EmployeeId,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };

        _context.AbsenceRequests.Add(absence);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(absence)
            .Reference(a => a.Employee)
            .LoadAsync();

        return MapToDto(absence);
    }

    public async Task<bool> UpdateAbsenceRequestAsync(int id, UpdateAbsenceRequestDto dto)
    {
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.Status != AbsenceStatus.Pending)
        {
            return false;
        }

        absence.Start = dto.Start;
        absence.End = dto.End;
        absence.Reason = dto.Reason;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveAbsenceRequestAsync(int id, ApproveAbsenceRequestDto dto)
    {
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.Status != AbsenceStatus.Pending)
        {
            return false;
        }

        absence.Status = AbsenceStatus.Approved;
        absence.ApproverId = dto.ApproverId;
        absence.ApprovedDate = DateTime.UtcNow;
        absence.ApprovalComments = dto.Comments;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAbsenceRequestAsync(int id, RejectAbsenceRequestDto dto)
    {
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.Status != AbsenceStatus.Pending)
        {
            return false;
        }

        absence.Status = AbsenceStatus.Rejected;
        absence.ApproverId = dto.ApproverId;
        absence.ApprovedDate = DateTime.UtcNow;
        absence.ApprovalComments = dto.Reason;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelAbsenceRequestAsync(int id, int employeeId)
    {
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.EmployeeId != employeeId || absence.Status == AbsenceStatus.Cancelled)
        {
            return false;
        }

        absence.Status = AbsenceStatus.Cancelled;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAbsenceRequestAsync(int id)
    {
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null)
        {
            return false;
        }

        _context.AbsenceRequests.Remove(absence);
        await _context.SaveChangesAsync();
        return true;
    }

    private static AbsenceRequestDto MapToDto(AbsenceRequest absence)
    {
        return new AbsenceRequestDto(
            Id: absence.Id,
            Start: absence.Start,
            End: absence.End,
            Reason: absence.Reason,
            EmployeeId: absence.EmployeeId,
            EmployeeName: absence.Employee?.Name ?? "Unknown",
            Status: absence.Status.ToString(),
            RequestedDate: absence.RequestedDate,
            ApproverId: absence.ApproverId,
            ApproverName: absence.Approver?.Name,
            ApprovedDate: absence.ApprovedDate,
            ApprovalComments: absence.ApprovalComments
        );
    }
}
