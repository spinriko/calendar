namespace pto.track.services.Exceptions;

/// <summary>
/// Exception thrown when a requested absence is not found.
/// </summary>
public class AbsenceNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsenceNotFoundException"/> class.
    /// </summary>
    /// <param name="absenceId">The ID of the absence that was not found.</param>
    public AbsenceNotFoundException(Guid absenceId)
        : base($"Absence request with ID '{absenceId}' was not found.")
    {
        AbsenceId = absenceId;
    }

    /// <summary>
    /// Gets the ID of the absence that was not found.
    /// </summary>
    public Guid AbsenceId { get; }
}
