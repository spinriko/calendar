namespace pto.track.services.Exceptions;

/// <summary>
/// Exception thrown when an operation is attempted on an entity in an invalid state.
/// </summary>
public class InvalidAbsenceOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAbsenceOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidAbsenceOperationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAbsenceOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="absenceId">The ID of the absence involved in the operation.</param>
    public InvalidAbsenceOperationException(string message, Guid absenceId)
        : base(message)
    {
        AbsenceId = absenceId;
    }

    /// <summary>
    /// Gets the ID of the absence involved in the invalid operation.
    /// </summary>
    public Guid? AbsenceId { get; }
}
