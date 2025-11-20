using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;

namespace pto.track.services;

public class AbsenceService : IAbsenceService
{
    private readonly PtoTrackDbContext _context;
    private readonly ILogger<AbsenceService> _logger;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public AbsenceService(PtoTrackDbContext context, ILogger<AbsenceService> logger, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsAsync(DateTime start, DateTime end, AbsenceStatus? status = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.GetAbsenceRequestsAsync: start={Start}, end={End}, status={Status}", start, end, status);

        var query = _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.Start < end && a.End > start);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var absences = await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogDebug("AbsenceService.GetAbsenceRequestsAsync: Found {Count} absences", absences.Count);

        return _mapper.Map<IEnumerable<AbsenceRequestDto>>(absences);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AbsenceRequestDto>> GetAbsenceRequestsByEmployeeAsync(int employeeId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.GetAbsenceRequestsByEmployeeAsync: employeeId={EmployeeId}, start={Start}, end={End}", employeeId, start, end);
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.EmployeeId == employeeId && a.Start < end && a.End > start)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.GetAbsenceRequestsByEmployeeAsync: Found {Count} absences for employee {EmployeeId}", absences.Count, employeeId);

        return _mapper.Map<IEnumerable<AbsenceRequestDto>>(absences);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AbsenceRequestDto>> GetPendingAbsenceRequestsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.GetPendingAbsenceRequestsAsync: Fetching pending absences");
        var absences = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .Where(a => a.Status == AbsenceStatus.Pending)
            .OrderBy(a => a.RequestedDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.GetPendingAbsenceRequestsAsync: Found {Count} pending absences", absences.Count);

        return _mapper.Map<IEnumerable<AbsenceRequestDto>>(absences);
    }

    /// <inheritdoc />
    public async Task<AbsenceRequestDto?> GetAbsenceRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.GetAbsenceRequestByIdAsync: id={Id}", id);
        var absence = await _context.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.Approver)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.GetAbsenceRequestByIdAsync: Absence {Id} not found", id);
            throw new AbsenceNotFoundException(id);
        }
        return _mapper.Map<AbsenceRequestDto>(absence);
    }

    /// <inheritdoc />
    public async Task<AbsenceRequestDto> CreateAbsenceRequestAsync(CreateAbsenceRequestDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.CreateAbsenceRequestAsync: Creating absence for employee {EmployeeId}", dto.EmployeeId);
        var absence = _mapper.Map<AbsenceRequest>(dto);

        _context.AbsenceRequests.Add(absence);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.CreateAbsenceRequestAsync: Created absence with id={Id}", absence.Id);

        // Reload with navigation properties
        await _context.Entry(absence)
            .Reference(a => a.Employee)
            .LoadAsync(cancellationToken);

        return _mapper.Map<AbsenceRequestDto>(absence);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAbsenceRequestAsync(Guid id, UpdateAbsenceRequestDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.UpdateAbsenceRequestAsync: id={Id}", id);
        var absence = await _context.AbsenceRequests.FindAsync(new object[] { id }, cancellationToken);
        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.UpdateAbsenceRequestAsync: Absence {Id} not found", id);
            throw new AbsenceNotFoundException(id);
        }

        if (absence.Status != AbsenceStatus.Pending)
        {
            _logger.LogDebug("AbsenceService.UpdateAbsenceRequestAsync: Absence {Id} not pending", id);
            throw new InvalidAbsenceOperationException("Only pending absence requests can be updated.", id);
        }

        _mapper.Map(dto, absence);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.UpdateAbsenceRequestAsync: Absence {Id} updated successfully", id);
        return Result.SuccessResult();
    }

    /// <inheritdoc />
    public async Task<Result> ApproveAbsenceRequestAsync(Guid id, ApproveAbsenceRequestDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.ApproveAbsenceRequestAsync: id={Id}, approverId={ApproverId}", id, dto.ApproverId);
        var absence = await _context.AbsenceRequests.FindAsync(new object[] { id }, cancellationToken);
        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.ApproveAbsenceRequestAsync: Absence {Id} not found", id);
            throw new AbsenceNotFoundException(id);
        }

        if (absence.Status != AbsenceStatus.Pending)
        {
            _logger.LogDebug("AbsenceService.ApproveAbsenceRequestAsync: Absence {Id} not pending", id);
            throw new InvalidAbsenceOperationException("Only pending absence requests can be approved.", id);
        }

        absence.Status = AbsenceStatus.Approved;
        absence.ApproverId = dto.ApproverId;
        absence.ApprovedDate = DateTime.UtcNow;
        absence.ApprovalComments = dto.Comments;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.ApproveAbsenceRequestAsync: Absence {Id} approved successfully", id);
        return Result.SuccessResult();
    }

    /// <inheritdoc />
    public async Task<Result> RejectAbsenceRequestAsync(Guid id, RejectAbsenceRequestDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.RejectAbsenceRequestAsync: id={Id}, approverId={ApproverId}", id, dto.ApproverId);
        var absence = await _context.AbsenceRequests.FindAsync(new object[] { id }, cancellationToken);
        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.RejectAbsenceRequestAsync: Absence {Id} not found", id);
            throw new AbsenceNotFoundException(id);
        }

        if (absence.Status != AbsenceStatus.Pending)
        {
            _logger.LogDebug("AbsenceService.RejectAbsenceRequestAsync: Absence {Id} not pending", id);
            throw new InvalidAbsenceOperationException("Only pending absence requests can be rejected.", id);
        }

        absence.Status = AbsenceStatus.Rejected;
        absence.ApproverId = dto.ApproverId;
        absence.ApprovedDate = DateTime.UtcNow;
        absence.ApprovalComments = dto.Reason;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.RejectAbsenceRequestAsync: Absence {Id} rejected successfully", id);
        return Result.SuccessResult();
    }

    /// <inheritdoc />
    public async Task<Result> CancelAbsenceRequestAsync(Guid id, int employeeId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: id={Id}, employeeId={EmployeeId}", id, employeeId);
        var absence = await _context.AbsenceRequests.FindAsync(new object[] { id }, cancellationToken);
        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: Absence {Id} not found", id);
            throw new AbsenceNotFoundException(id);
        }

        if (absence.EmployeeId != employeeId)
        {
            _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: Employee {EmployeeId} not authorized for absence {Id}", employeeId, id);
            throw new UnauthorizedAbsenceAccessException("Only the employee who created the absence request can cancel it.", id, employeeId);
        }

        if (absence.Status == AbsenceStatus.Cancelled)
        {
            _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: Absence {Id} already cancelled", id);
            throw new InvalidAbsenceOperationException("Absence request is already cancelled.", id);
        }

        absence.Status = AbsenceStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.CancelAbsenceRequestAsync: Absence {Id} cancelled successfully", id);
        return Result.SuccessResult();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAbsenceRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AbsenceService.DeleteAbsenceRequestAsync: id={Id}", id);
        var absence = await _context.AbsenceRequests.FindAsync(new object[] { id }, cancellationToken);
        if (absence == null)
        {
            _logger.LogDebug("AbsenceService.DeleteAbsenceRequestAsync: Absence {Id} not found", id);
            throw new AbsenceNotFoundException(id);
        }

        _context.AbsenceRequests.Remove(absence);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("AbsenceService.DeleteAbsenceRequestAsync: Absence {Id} deleted successfully", id);
        return Result.SuccessResult();
    }
}
