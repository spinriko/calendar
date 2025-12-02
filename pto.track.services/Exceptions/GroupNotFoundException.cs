namespace pto.track.services.Exceptions;

/// <summary>
/// Exception thrown when a requested group is not found.
/// </summary>
public class GroupNotFoundException : Exception
{
    public int GroupId { get; }

    public GroupNotFoundException(int groupId)
        : base($"Group with ID {groupId} was not found.")
    {
        GroupId = groupId;
    }
}
