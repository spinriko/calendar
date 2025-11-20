namespace pto.track.services.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to perform an action they are not authorized to perform.
/// </summary>
public class UnauthorizedAbsenceAccessException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedAbsenceAccessException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnauthorizedAbsenceAccessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedAbsenceAccessException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="absenceId">The ID of the absence.</param>
    /// <param name="employeeId">The ID of the employee attempting the operation.</param>
    public UnauthorizedAbsenceAccessException(string message, Guid absenceId, int employeeId)
        : base(message)
    {
        AbsenceId = absenceId;
        EmployeeId = employeeId;
    }

    /// <summary>
    /// Gets the ID of the absence involved.
    /// </summary>
    public Guid? AbsenceId { get; }

    /// <summary>
    /// Gets the ID of the employee attempting the operation.
    /// </summary>
    public int? EmployeeId { get; }
}
