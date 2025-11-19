using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services;

public class AbsenceService : IAbsenceService
{
    private readonly PtoTrackDbContext _context;
    private readonly ILogger<AbsenceService> _logger;

    public AbsenceService(PtoTrackDbContext context, ILogger<AbsenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsAsync(DateTime start, DateTime end)
    {
        _logger.LogDebug("AbsenceService.GetAbsenceRequestsAsync: start={Start}, end={End}", start, end);
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.Start < end && a.End > start)
            .AsNoTracking()
            .ToListAsync();
        _logger.LogDebug("AbsenceService.GetAbsenceRequestsAsync: Found {Count} absences", absences.Count);

        return absences.Select(MapToDto);
    }

    public async Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsByEmployeeAsync(int employeeId, DateTime start, DateTime end)
    {
        _logger.LogDebug("AbsenceService.GetAbsenceRequestsByEmployeeAsync: employeeId={EmployeeId}, start={Start}, end={End}", employeeId, start, end);
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.EmployeeId == employeeId && a.Start < end && a.End > start)
            .AsNoTracking()
            .ToListAsync();
        _logger.LogDebug("AbsenceService.GetAbsenceRequestsByEmployeeAsync: Found {Count} absences for employee {EmployeeId}", absences.Count, employeeId);

        return absences.Select(MapToDto);
    }

    public async Task<IEnumerable<AbsenceRequestDto>> GetPendingAbsenceRequestsAsync()
    {
        _logger.LogDebug("AbsenceService.GetPendingAbsenceRequestsAsync: Fetching pending absences");
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.Status == AbsenceStatus.Pending)
            .OrderBy(a => a.RequestedDate)
            .AsNoTracking()
            .ToListAsync();
        _logger.LogDebug("AbsenceService.GetPendingAbsenceRequestsAsync: Found {Count} pending absences", absences.Count);

        return absences.Select(MapToDto);
    }

    public async Task<AbsenceRequestDto?> GetAbsenceRequestByIdAsync(Guid id)
    {
        _logger.LogDebug("AbsenceService.GetAbsenceRequestByIdAsync: id={Id}", id);
        var absence = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.GetAbsenceRequestByIdAsync: Absence {Id} not found", id);
        }
        return absence == null ? null : MapToDto(absence);
    }

    public async Task<AbsenceRequestDto> CreateAbsenceRequestAsync(CreateAbsenceRequestDto dto)
    {
        _logger.LogDebug("AbsenceService.CreateAbsenceRequestAsync: Creating absence for employee {EmployeeId}", dto.EmployeeId);
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
        _logger.LogDebug("AbsenceService.CreateAbsenceRequestAsync: Created absence with id={Id}", absence.Id);

        // Reload with navigation properties
        await _context.Entry(absence)
            .Reference(a => a.Employee)
            .LoadAsync();

        return MapToDto(absence);
    }

    public async Task<bool> UpdateAbsenceRequestAsync(Guid id, UpdateAbsenceRequestDto dto)
    {
        _logger.LogDebug("AbsenceService.UpdateAbsenceRequestAsync: id={Id}", id);
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.Status != AbsenceStatus.Pending)
        {
            _logger.LogDebug("AbsenceService.UpdateAbsenceRequestAsync: Absence {Id} not found or not pending", id);
            return false;
        }

        absence.Start = dto.Start;
        absence.End = dto.End;
        absence.Reason = dto.Reason;

        await _context.SaveChangesAsync();
        _logger.LogDebug("AbsenceService.UpdateAbsenceRequestAsync: Absence {Id} updated successfully", id);
        return true;
    }

    public async Task<bool> ApproveAbsenceRequestAsync(Guid id, ApproveAbsenceRequestDto dto)
    {
        _logger.LogDebug("AbsenceService.ApproveAbsenceRequestAsync: id={Id}, approverId={ApproverId}", id, dto.ApproverId);
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.Status != AbsenceStatus.Pending)
        {
            _logger.LogDebug("AbsenceService.ApproveAbsenceRequestAsync: Absence {Id} not found or not pending", id);
            return false;
        }

        absence.Status = AbsenceStatus.Approved;
        absence.ApproverId = dto.ApproverId;
        absence.ApprovedDate = DateTime.UtcNow;
        absence.ApprovalComments = dto.Comments;

        await _context.SaveChangesAsync();
        _logger.LogDebug("AbsenceService.ApproveAbsenceRequestAsync: Absence {Id} approved successfully", id);
        return true;
    }

    public async Task<bool> RejectAbsenceRequestAsync(Guid id, RejectAbsenceRequestDto dto)
    {
        _logger.LogDebug("AbsenceService.RejectAbsenceRequestAsync: id={Id}, approverId={ApproverId}", id, dto.ApproverId);
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.Status != AbsenceStatus.Pending)
        {
            _logger.LogDebug("AbsenceService.RejectAbsenceRequestAsync: Absence {Id} not found or not pending", id);
            return false;
        }

        absence.Status = AbsenceStatus.Rejected;
        absence.ApproverId = dto.ApproverId;
        absence.ApprovedDate = DateTime.UtcNow;
        absence.ApprovalComments = dto.Reason;

        await _context.SaveChangesAsync();
        _logger.LogDebug("AbsenceService.RejectAbsenceRequestAsync: Absence {Id} rejected successfully", id);
        return true;
    }

    public async Task<bool> CancelAbsenceRequestAsync(Guid id, int employeeId)
    {
        _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: id={Id}, employeeId={EmployeeId}", id, employeeId);
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null || absence.EmployeeId != employeeId || absence.Status == AbsenceStatus.Cancelled)
        {
            _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: Absence {Id} not found, wrong employee, or already cancelled", id);
            return false;
        }

        absence.Status = AbsenceStatus.Cancelled;
        await _context.SaveChangesAsync();
        _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: Absence {Id} cancelled successfully", id);
        return true;
    }

    public async Task<bool> DeleteAbsenceRequestAsync(Guid id)
    {
        _logger.LogDebug("AbsenceService.DeleteAbsenceRequestAsync: id={Id}", id);
        var absence = await _context.AbsenceRequests.FindAsync(id);
        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.DeleteAbsenceRequestAsync: Absence {Id} not found", id);
            return false;
        }

        _context.AbsenceRequests.Remove(absence);
        await _context.SaveChangesAsync();
        _logger.LogDebug("AbsenceService.DeleteAbsenceRequestAsync: Absence {Id} deleted successfully", id);
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
