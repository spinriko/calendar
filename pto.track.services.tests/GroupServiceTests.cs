using pto.track.data.Models;
using pto.track.services;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;
using Xunit;

namespace pto.track.services.tests;

public class GroupServiceTests : TestBase
{
    [Fact]
    public async Task GetGroupsAsync_WithMultipleGroups_ReturnsAll()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var groups = new[]
        {
            new Group { GroupId = 1, Name = "Group A" },
            new Group { GroupId = 2, Name = "Group B" },
            new Group { GroupId = 3, Name = "Group C" }
        };
        context.Groups.AddRange(groups);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetGroupsAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetGroupsAsync_WithNoGroups_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        // Act
        var result = await service.GetGroupsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetGroupsAsync_ReturnsDtosNotEntities()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var group = new Group { GroupId = 1, Name = "Test Group" };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetGroupsAsync();

        // Assert
        var firstResult = result.First();
        Assert.IsType<GroupDto>(firstResult);
        Assert.IsNotType<Group>(firstResult);
    }

    [Fact]
    public async Task GetGroupByIdAsync_WithValidId_ReturnsGroup()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var group = new Group { GroupId = 1, Name = "Test Group" };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetGroupByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.GroupId);
        Assert.Equal("Test Group", result.Name);
    }

    [Fact]
    public async Task GetGroupByIdAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        // Act & Assert
        await Assert.ThrowsAsync<GroupNotFoundException>(
            () => service.GetGroupByIdAsync(999));
    }

    [Fact]
    public async Task CreateGroupAsync_CreatesGroup()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var createDto = new CreateGroupDto("New Group");

        // Act
        var result = await service.CreateGroupAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.GroupId > 0);
        Assert.Equal("New Group", result.Name);

        // Verify it was saved to database
        var savedGroup = await context.Groups.FindAsync(result.GroupId);
        Assert.NotNull(savedGroup);
        Assert.Equal("New Group", savedGroup.Name);
    }

    [Fact]
    public async Task UpdateGroupAsync_WithValidId_UpdatesGroup()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var group = new Group { GroupId = 1, Name = "Original Name" };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var updateDto = new UpdateGroupDto("Updated Name");

        // Act
        var result = await service.UpdateGroupAsync(1, updateDto);

        // Assert
        Assert.True(result);

        // Verify the update
        var updatedGroup = await context.Groups.FindAsync(1);
        Assert.NotNull(updatedGroup);
        Assert.Equal("Updated Name", updatedGroup.Name);
    }

    [Fact]
    public async Task UpdateGroupAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var updateDto = new UpdateGroupDto("Updated Name");

        // Act & Assert
        await Assert.ThrowsAsync<GroupNotFoundException>(
            () => service.UpdateGroupAsync(999, updateDto));
    }

    [Fact]
    public async Task DeleteGroupAsync_WithValidId_DeletesGroup()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var group = new Group { GroupId = 1, Name = "Test Group" };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteGroupAsync(1);

        // Assert
        Assert.True(result);

        // Verify it was deleted
        var deletedGroup = await context.Groups.FindAsync(1);
        Assert.Null(deletedGroup);
    }

    [Fact]
    public async Task DeleteGroupAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        // Act & Assert
        await Assert.ThrowsAsync<GroupNotFoundException>(
            () => service.DeleteGroupAsync(999));
    }

    [Fact]
    public async Task DeleteGroupAsync_WithAssociatedResources_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var group = new Group { GroupId = 1, Name = "Test Group" };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var resource = new data.Resource
        {
            Id = 1,
            Name = "Test Resource",
            GroupId = 1
        };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteGroupAsync(1));
    }

    [Fact]
    public async Task GetGroupsAsync_UsesNoTracking()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var group = new Group { GroupId = 1, Name = "Test Group" };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Clear any tracking from setup
        context.ChangeTracker.Clear();

        // Act
        var result = await service.GetGroupsAsync();

        // Assert - verify query uses AsNoTracking
        Assert.Single(result);
        Assert.Equal("Test Group", result.First().Name);

        // The actual check: after the query, no new tracked entities should exist
        var trackedAfterQuery = context.ChangeTracker.Entries().Count();
        Assert.Equal(0, trackedAfterQuery);
    }

    [Fact]
    public async Task CreateGroupAsync_PersistsToDatabase()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new GroupService(context, CreateLogger<GroupService>(), CreateMapper());

        var createDto = new CreateGroupDto("Persisted Group");

        // Act
        var result = await service.CreateGroupAsync(createDto);

        // Clear context to ensure we're reading from database
        context.ChangeTracker.Clear();

        // Assert - Read from database to verify persistence
        var groups = await service.GetGroupsAsync();
        var persistedGroup = groups.FirstOrDefault(g => g.GroupId == result.GroupId);
        Assert.NotNull(persistedGroup);
        Assert.Equal("Persisted Group", persistedGroup.Name);
    }
}
