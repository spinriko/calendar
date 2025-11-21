namespace pto.track.services.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class ResourceNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    /// <param name="resourceId">The ID of the resource that was not found.</param>
    public ResourceNotFoundException(int resourceId)
        : base($"Resource with ID '{resourceId}' was not found.")
    {
        ResourceId = resourceId;
    }

    /// <summary>
    /// Gets the ID of the resource that was not found.
    /// </summary>
    public int ResourceId { get; }
}
