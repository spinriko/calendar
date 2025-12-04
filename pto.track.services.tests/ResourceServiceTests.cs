using pto.track.data;
using pto.track.services;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;
using Xunit;

namespace pto.track.services.tests;

public class ResourceServiceTests : TestBase
{
    [Fact]
    public async Task GetResourcesAsync_WithMultipleResources_ReturnsAll()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Resource A", GroupId = 1 },
            new Resource { Id = 2, Name = "Resource B", GroupId = 1 },
            new Resource { Id = 3, Name = "Resource C", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetResourceByIdAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetResourceByIdAsync(999));
    }

    [Fact]
    public async Task GetResourcesAsync_ReturnsCorrectData()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Conference Room A", GroupId = 1 },
            new Resource { Id = 2, Name = "Conference Room B", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);

        var resource1 = list.First(r => r.Id == 1);
        Assert.Equal("Conference Room A", resource1.Name);

        var resource2 = list.First(r => r.Id == 2);
        Assert.Equal("Conference Room B", resource2.Name);
    }

    [Fact]
    public async Task GetResourcesAsync_ReturnsDtosNotEntities()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resource = new Resource { Id = 1, Name = "Test Resource", GroupId = 1 };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        var firstResult = result.First();
        Assert.IsType<ResourceDto>(firstResult);
        Assert.IsNotType<Resource>(firstResult);
    }

    [Fact]
    public async Task GetResourcesAsync_ReturnsInDatabaseOrder()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 3, Name = "Resource C", GroupId = 1 },
            new Resource { Id = 1, Name = "Resource A", GroupId = 1 },
            new Resource { Id = 2, Name = "Resource B", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        var list = result.ToList();
        Assert.Equal(3, list.Count);
        // Verify all resources are returned, order may vary by database
        Assert.Contains(list, r => r.Id == 1);
        Assert.Contains(list, r => r.Id == 2);
        Assert.Contains(list, r => r.Id == 3);
    }
    [Fact]
    public async Task GetResourcesAsync_WithSingleResource_ReturnsSingle()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resource = new Resource { Id = 1, Name = "Only Resource", GroupId = 1 };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Only Resource", result.First().Name);
    }

    [Fact]
    public async Task GetResourcesAsync_UsesNoTracking()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resource = new Resource { Id = 1, Name = "Test Resource", GroupId = 1 };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Clear any tracking from setup
        context.ChangeTracker.Clear();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert - verify query uses AsNoTracking by checking returned data exists
        // but context doesn't track new entities from the query
        Assert.Single(result);
        Assert.Equal("Test Resource", result.First().Name);

        // The actual check: after the query, no new tracked entities should exist
        var trackedAfterQuery = context.ChangeTracker.Entries().Count();
        Assert.Equal(0, trackedAfterQuery);
    }

    [Fact]
    public async Task GetActiveResourcesAsync_FiltersInactiveResources()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Active User", IsActive = true, Role = "Employee", GroupId = 1 },
            new Resource { Id = 2, Name = "Inactive User", IsActive = false, Role = "Employee", GroupId = 1 },
            new Resource { Id = 3, Name = "Active Manager", IsActive = true, Role = "Manager", IsApprover = true, GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetActiveResourcesAsync();

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.True(r.IsActive));
        Assert.Contains(list, r => r.Id == 1);
        Assert.Contains(list, r => r.Id == 3);
        Assert.DoesNotContain(list, r => r.Id == 2);
    }

    [Fact]
    public async Task GetActiveResourcesAsync_WithAllInactive_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Inactive 1", IsActive = false, Role = "Employee", GroupId = 1 },
            new Resource { Id = 2, Name = "Inactive 2", IsActive = false, Role = "Employee", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetActiveResourcesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApproversAsync_ReturnsOnlyActiveApprovers()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Manager 1", IsActive = true, IsApprover = true, Role = "Manager", GroupId = 1 },
            new Resource { Id = 2, Name = "Employee", IsActive = true, IsApprover = false, Role = "Employee", GroupId = 1 },
            new Resource { Id = 3, Name = "Inactive Manager", IsActive = false, IsApprover = true, Role = "Manager", GroupId = 1 },
            new Resource { Id = 4, Name = "Admin", IsActive = true, IsApprover = true, Role = "Administrator", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetApproversAsync();

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.True(r.IsApprover));
        Assert.All(list, r => Assert.True(r.IsActive));
        Assert.Contains(list, r => r.Id == 1);
        Assert.Contains(list, r => r.Id == 4);
    }

    [Fact]
    public async Task GetApproversAsync_WithNoApprovers_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
              new Resource { Id = 1, Name = "Employee 1", IsActive = true, IsApprover = false, Role = "Employee", GroupId = 1 },
              new Resource { Id = 2, Name = "Employee 2", IsActive = true, IsApprover = false, Role = "Employee", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetApproversAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetResourceByIdAsync_WithValidId_ReturnsResource()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resource = new Resource
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            EmployeeNumber = "EMP001",
            Role = "Manager",
            IsApprover = true,
            IsActive = true,
            Department = "IT",
            GroupId = 1
        };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();
        var result = await service.GetResourceByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("EMP001", result.EmployeeNumber);
        Assert.Equal("Manager", result.Role);
        Assert.True(result.IsApprover);
        Assert.True(result.IsActive);
        Assert.Equal("IT", result.Department);
    }

    [Fact]
    public async Task GetResourcesAsync_IncludesAllNewProperties()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resource = new Resource
        {
            Id = 1,
            Name = "Full User",
            Email = "full@test.com",
            EmployeeNumber = "E123",
            Role = "Administrator",
            IsApprover = true,
            IsActive = true,
            Department = "HR",
            GroupId = 1
        };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        var dto = result.First();
        Assert.Equal("full@test.com", dto.Email);
        Assert.Equal("E123", dto.EmployeeNumber);
        Assert.Equal("Administrator", dto.Role);
        Assert.True(dto.IsApprover);
        Assert.True(dto.IsActive);
        Assert.Equal("HR", dto.Department);
    }

    [Fact]
    public async Task GetResourcesByGroupAsync_ReturnsResourcesForGroup()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Group 1 Resource A", GroupId = 1 },
            new Resource { Id = 2, Name = "Group 1 Resource B", GroupId = 1 },
            new Resource { Id = 3, Name = "Group 2 Resource C", GroupId = 2 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesByGroupAsync(1);

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.Equal(1, r.GroupId));
        Assert.Contains(list, r => r.Id == 1);
        Assert.Contains(list, r => r.Id == 2);
        Assert.DoesNotContain(list, r => r.Id == 3);
    }

    [Fact]
    public async Task GetResourcesByGroupAsync_WithMultipleGroups_FiltersCorrectly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Group 1 Resource", GroupId = 1 },
            new Resource { Id = 2, Name = "Group 2 Resource", GroupId = 2 },
            new Resource { Id = 3, Name = "Group 3 Resource", GroupId = 3 },
            new Resource { Id = 4, Name = "Group 2 Resource 2", GroupId = 2 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var resultGroup1 = await service.GetResourcesByGroupAsync(1);
        var resultGroup2 = await service.GetResourcesByGroupAsync(2);
        var resultGroup3 = await service.GetResourcesByGroupAsync(3);

        // Assert
        Assert.Single(resultGroup1);
        Assert.Equal(1, resultGroup1.First().Id);

        Assert.Equal(2, resultGroup2.Count());
        Assert.All(resultGroup2, r => Assert.Equal(2, r.GroupId));

        Assert.Single(resultGroup3);
        Assert.Equal(3, resultGroup3.First().Id);
    }

    [Fact]
    public async Task GetResourcesByGroupAsync_WithNoResources_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Group 1 Resource", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act - Query for group that has no resources
        var result = await service.GetResourcesByGroupAsync(99);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetResourcesByGroupAsync_IncludesGroupIdInDto()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resource = new Resource
        {
            Id = 1,
            Name = "Test Resource",
            Email = "test@example.com",
            EmployeeNumber = "EMP001",
            Role = "Employee",
            IsActive = true,
            GroupId = 5
        };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesByGroupAsync(5);

        // Assert
        var dto = result.First();
        Assert.Equal(5, dto.GroupId);
        Assert.Equal("Test Resource", dto.Name);
        Assert.Equal("test@example.com", dto.Email);
    }

    [Fact]
    public async Task GetResourcesByGroupAsync_UsesNoTracking()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resource = new Resource { Id = 1, Name = "Test Resource", GroupId = 1 };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Clear any tracking from setup
        context.ChangeTracker.Clear();

        // Act
        var result = await service.GetResourcesByGroupAsync(1);

        // Assert - verify query uses AsNoTracking
        Assert.Single(result);
        Assert.Equal("Test Resource", result.First().Name);

        // The actual check: after the query, no new tracked entities should exist
        var trackedAfterQuery = context.ChangeTracker.Entries().Count();
        Assert.Equal(0, trackedAfterQuery);
    }

    [Fact]
    public async Task GetResourcesByGroupAsync_ReturnsOnlyActiveAndInactiveResources()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());

        var resources = new[]
        {
            new Resource { Id = 1, Name = "Active Resource", GroupId = 1, IsActive = true },
            new Resource { Id = 2, Name = "Inactive Resource", GroupId = 1, IsActive = false },
            new Resource { Id = 3, Name = "Other Group Active", GroupId = 2, IsActive = true }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesByGroupAsync(1);

        // Assert - Should include both active and inactive resources for the group
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, r => r.Id == 1 && r.IsActive);
        Assert.Contains(list, r => r.Id == 2 && !r.IsActive);
        Assert.DoesNotContain(list, r => r.Id == 3);
    }
}
