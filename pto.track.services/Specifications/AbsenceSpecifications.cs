using pto.track.data;

namespace pto.track.services.Specifications;

/// <summary>
/// Specification for querying absence requests within a date range.
/// </summary>
public class AbsencesByDateRangeSpec : BaseSpecification<AbsenceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsencesByDateRangeSpec"/> class.
    /// </summary>
    /// <param name="start">Start date of the range.</param>
    /// <param name="end">End date of the range.</param>
    public AbsencesByDateRangeSpec(DateTime start, DateTime end)
        : base(a => (a.Start < end && a.End > start)!)
    {
        AddInclude(a => a.Employee);
        AddInclude(a => a.Approver);
        ApplyOrderBy(a => a.Start);
    }
}

/// <summary>
/// Specification for querying absence requests by status.
/// </summary>
public class AbsencesByStatusSpec : BaseSpecification<AbsenceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsencesByStatusSpec"/> class.
    /// </summary>
    /// <param name="statuses">The list of statuses to filter by.</param>
    public AbsencesByStatusSpec(List<AbsenceStatus> statuses)
        : base(a => statuses.Contains(a.Status)!)
    {
        AddInclude(a => a.Employee);
        AddInclude(a => a.Approver);
    }
}

/// <summary>
/// Specification for querying absence requests by employee.
/// </summary>
public class AbsencesByEmployeeSpec : BaseSpecification<AbsenceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsencesByEmployeeSpec"/> class.
    /// </summary>
    /// <param name="employeeId">The employee ID to filter by.</param>
    public AbsencesByEmployeeSpec(int employeeId)
        : base(a => (a.EmployeeId == employeeId)!)
    {
        AddInclude(a => a.Employee);
        AddInclude(a => a.Approver);
        ApplyOrderBy(a => a.Start);
    }
}

/// <summary>
/// Composite specification for querying absences with date range, optional statuses, and optional employee filter.
/// </summary>
public class AbsencesFilteredSpec : BaseSpecification<AbsenceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsencesFilteredSpec"/> class.
    /// </summary>
    /// <param name="start">Start date of the range.</param>
    /// <param name="end">End date of the range.</param>
    /// <param name="statuses">Optional list of statuses to filter by. If null or empty, all statuses are included.</param>
    /// <param name="employeeId">Optional employee ID to filter by.</param>
    public AbsencesFilteredSpec(DateTime start, DateTime end, List<AbsenceStatus>? statuses = null, int? employeeId = null)
    {
        // Always include related entities
        AddInclude(a => a.Employee);
        AddInclude(a => a.Approver);
        ApplyOrderBy(a => a.Start);

        // Build the criteria dynamically
        if (employeeId.HasValue)
        {
            // Filter by employee AND date range
            And(a => (a.EmployeeId == employeeId.Value)!);
        }

        // Filter by date range
        And(a => (a.Start < end && a.End > start)!);

        // Filter by statuses if provided
        if (statuses != null && statuses.Any())
        {
            And(a => statuses.Contains(a.Status)!);
        }
    }
}

/// <summary>
/// Specification for querying pending absence requests.
/// </summary>
public class PendingAbsencesSpec : BaseSpecification<AbsenceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PendingAbsencesSpec"/> class.
    /// </summary>
    public PendingAbsencesSpec()
        : base(a => (a.Status == AbsenceStatus.Pending)!)
    {
        AddInclude(a => a.Employee);
        AddInclude(a => a.Approver);
        ApplyOrderBy(a => a.RequestedDate);
    }
}

/// <summary>
/// Specification for querying a single absence request by ID.
/// </summary>
public class AbsenceByIdSpec : BaseSpecification<AbsenceRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsenceByIdSpec"/> class.
    /// </summary>
    /// <param name="id">The absence request ID.</param>
    public AbsenceByIdSpec(Guid id)
        : base(a => (a.Id == id)!)
    {
        AddInclude(a => a.Employee);
        AddInclude(a => a.Approver);
        EnableTracking(); // Enable tracking for update scenarios
    }
}
