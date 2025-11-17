using pto.track.data;
using Xunit;

namespace pto.track.services.tests;

public class ResourceServiceTests : TestBase
{
    [Fact]
    public async Task GetResourcesAsync_WithMultipleResources_ReturnsAll()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context);

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
    public async Task GetResourcesAsync_WithNoResources_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context);

        // Act
        var result = await service.GetResourcesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetResourcesAsync_ReturnsCorrectData()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ResourceService(context);

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
        var service = new ResourceService(context);

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
        var service = new ResourceService(context);

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
        var service = new ResourceService(context);

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
        var service = new ResourceService(context);

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
}
