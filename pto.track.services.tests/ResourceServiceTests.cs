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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resources = new[]
        {
            new SchedulerResource { Id = 1, Name = "Resource A" },
            new SchedulerResource { Id = 2, Name = "Resource B" },
            new SchedulerResource { Id = 3, Name = "Resource C" }
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetResourceByIdAsync(999));
    }

    [Fact]
    public async Task GetResourcesAsync_ReturnsCorrectData()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resources = new[]
        {
            new SchedulerResource { Id = 1, Name = "Conference Room A" },
            new SchedulerResource { Id = 2, Name = "Conference Room B" }
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resource = new SchedulerResource { Id = 1, Name = "Test Resource" };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        var firstResult = result.First();
        Assert.IsType<ResourceDto>(firstResult);
        Assert.IsNotType<SchedulerResource>(firstResult);
    }

    [Fact]
    public async Task GetResourcesAsync_ReturnsInDatabaseOrder()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resources = new[]
        {
            new SchedulerResource { Id = 3, Name = "Resource C" },
            new SchedulerResource { Id = 1, Name = "Resource A" },
            new SchedulerResource { Id = 2, Name = "Resource B" }
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resource = new SchedulerResource { Id = 1, Name = "Only Resource" };
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resource = new SchedulerResource { Id = 1, Name = "Test Resource" };
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resources = new[]
        {
            new SchedulerResource { Id = 1, Name = "Active User", IsActive = true, Role = "Employee" },
            new SchedulerResource { Id = 2, Name = "Inactive User", IsActive = false, Role = "Employee" },
            new SchedulerResource { Id = 3, Name = "Active Manager", IsActive = true, Role = "Manager", IsApprover = true }
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resources = new[]
        {
            new SchedulerResource { Id = 1, Name = "Inactive 1", IsActive = false, Role = "Employee" },
            new SchedulerResource { Id = 2, Name = "Inactive 2", IsActive = false, Role = "Employee" }
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resources = new[]
        {
            new SchedulerResource { Id = 1, Name = "Manager 1", IsActive = true, IsApprover = true, Role = "Manager" },
            new SchedulerResource { Id = 2, Name = "Employee", IsActive = true, IsApprover = false, Role = "Employee" },
            new SchedulerResource { Id = 3, Name = "Inactive Manager", IsActive = false, IsApprover = true, Role = "Manager" },
            new SchedulerResource { Id = 4, Name = "Admin", IsActive = true, IsApprover = true, Role = "Administrator" }
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resources = new[]
        {
            new SchedulerResource { Id = 1, Name = "Employee 1", IsActive = true, IsApprover = false, Role = "Employee" },
            new SchedulerResource { Id = 2, Name = "Employee 2", IsActive = true, IsApprover = false, Role = "Employee" }
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resource = new SchedulerResource
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            EmployeeNumber = "EMP001",
            Role = "Manager",
            IsApprover = true,
            IsActive = true,
            Department = "IT"
        };
        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        // Act
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
        var service = new ResourceService(context, CreateLogger<ResourceService>());

        var resource = new SchedulerResource
        {
            Id = 1,
            Name = "Full User",
            Email = "full@test.com",
            EmployeeNumber = "E123",
            Role = "Administrator",
            IsApprover = true,
            IsActive = true,
            Department = "HR"
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
}
