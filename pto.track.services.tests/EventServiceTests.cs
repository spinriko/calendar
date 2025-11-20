using pto.track.data;
using pto.track.services;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;
using Xunit;

namespace pto.track.services.tests;

public class EventServiceTests : TestBase
{
    [Fact]
    public async Task GetEventsAsync_WithEventsInDateRange_ReturnsMatchingEvents()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var events = new[]
        {
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 10, 0, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "Event 1", ResourceId = 1 },
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 14, 0, 0), End = new DateTime(2025, 11, 13, 15, 0, 0), Text = "Event 2", ResourceId = 1 },
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 14, 10, 0, 0), End = new DateTime(2025, 11, 14, 11, 0, 0), Text = "Event 3", ResourceId = 1 }
        };
        context.Events.AddRange(events);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetEventsAsync(
            new DateTime(2025, 11, 13, 9, 0, 0),
            new DateTime(2025, 11, 13, 17, 0, 0)
        );

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Equal(13, e.Start.Day));
    }

    [Fact]
    public async Task GetEventsAsync_WithNoEventsInRange_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var event1 = new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 10, 10, 0, 0), End = new DateTime(2025, 11, 10, 11, 0, 0), Text = "Event 1", ResourceId = 1 };
        context.Events.Add(event1);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetEventsAsync(
            new DateTime(2025, 11, 15, 0, 0, 0),
            new DateTime(2025, 11, 16, 0, 0, 0)
        );

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEventsAsync_WithOverlappingEvents_ReturnsCorrectly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var events = new[]
        {
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 8, 0, 0), End = new DateTime(2025, 11, 13, 9, 0, 0), Text = "Before", ResourceId = 1 },
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 9, 30, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "Overlapping", ResourceId = 1 },
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 12, 0, 0), End = new DateTime(2025, 11, 13, 13, 0, 0), Text = "After", ResourceId = 1 }
        };
        context.Events.AddRange(events);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetEventsAsync(
            new DateTime(2025, 11, 13, 9, 30, 0),
            new DateTime(2025, 11, 13, 11, 30, 0)
        );

        // Assert
        Assert.Single(result);
        Assert.Equal("Overlapping", result.First().Text);
    }
    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ReturnsEvent()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var event1 = new SchedulerEvent
        {
            Id = Guid.NewGuid(),
            Start = new DateTime(2025, 11, 13, 10, 0, 0),
            End = new DateTime(2025, 11, 13, 11, 0, 0),
            Text = "Test Event",
            Color = "blue",
            ResourceId = 1
        };
        context.Events.Add(event1);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetEventByIdAsync(event1.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Event", result.Text);
        Assert.Equal("blue", result.Color);
        Assert.Equal(1, result.ResourceId);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(
            () => service.GetEventByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateEventAsync_WithValidDto_CreatesAndReturnsEvent()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var createDto = new CreateEventDto(
            Start: new DateTime(2025, 11, 13, 10, 0, 0),
            End: new DateTime(2025, 11, 13, 11, 0, 0),
            Text: "New Event",
            Color: "red",
            ResourceId: 1
        );

        // Act
        var result = await service.CreateEventAsync(createDto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("New Event", result.Text);
        Assert.Equal("red", result.Color);

        // Verify in database
        var dbEvent = await context.Events.FindAsync(result.Id);
        Assert.NotNull(dbEvent);
        Assert.Equal("New Event", dbEvent.Text);
    }

    [Fact]
    public async Task CreateEventAsync_WithNullableFields_CreatesSuccessfully()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var createDto = new CreateEventDto(
            Start: new DateTime(2025, 11, 13, 10, 0, 0),
            End: new DateTime(2025, 11, 13, 11, 0, 0),
            Text: null,
            Color: null,
            ResourceId: 1
        );

        // Act
        var result = await service.CreateEventAsync(createDto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Null(result.Text);
        Assert.Null(result.Color);
    }

    [Fact]
    public async Task UpdateEventAsync_WithValidIdAndDto_UpdatesEvent()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var event1 = new SchedulerEvent
        {
            Id = Guid.NewGuid(),
            Start = new DateTime(2025, 11, 13, 10, 0, 0),
            End = new DateTime(2025, 11, 13, 11, 0, 0),
            Text = "Original",
            ResourceId = 1
        };
        context.Events.Add(event1);
        await context.SaveChangesAsync();

        var updateDto = new UpdateEventDto(
            Start: new DateTime(2025, 11, 13, 14, 0, 0),
            End: new DateTime(2025, 11, 13, 15, 0, 0),
            Text: "Updated",
            Color: "green",
            ResourceId: 2
        );

        // Act
        var result = await service.UpdateEventAsync(event1.Id, updateDto);

        // Assert
        Assert.True(result.Success);

        var updated = await context.Events.FindAsync(event1.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Text);
        Assert.Equal("green", updated.Color);
        Assert.Equal(2, updated.ResourceId);
        Assert.Equal(new DateTime(2025, 11, 13, 14, 0, 0), updated.Start);
    }

    [Fact]
    public async Task UpdateEventAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var updateDto = new UpdateEventDto(
            Start: new DateTime(2025, 11, 13, 14, 0, 0),
            End: new DateTime(2025, 11, 13, 15, 0, 0),
            Text: "Updated",
            Color: null,
            ResourceId: 1
        );

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(
            () => service.UpdateEventAsync(Guid.NewGuid(), updateDto));
    }

    [Fact]
    public async Task DeleteEventAsync_WithValidId_DeletesEvent()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var event1 = new SchedulerEvent
        {
            Id = Guid.NewGuid(),
            Start = new DateTime(2025, 11, 13, 10, 0, 0),
            End = new DateTime(2025, 11, 13, 11, 0, 0),
            Text = "To Delete",
            ResourceId = 1
        };
        context.Events.Add(event1);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteEventAsync(event1.Id);

        // Assert
        Assert.True(result.Success);

        var deleted = await context.Events.FindAsync(event1.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteEventAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(
            () => service.DeleteEventAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateEventAsync_PreservesAllFields()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var createDto = new CreateEventDto(
            Start: new DateTime(2025, 11, 13, 10, 30, 45),
            End: new DateTime(2025, 11, 13, 12, 15, 30),
            Text: "Detailed Event",
            Color: "#FF5733",
            ResourceId: 5
        );

        // Act
        var result = await service.CreateEventAsync(createDto);

        // Assert
        Assert.Equal(new DateTime(2025, 11, 13, 10, 30, 45), result.Start);
        Assert.Equal(new DateTime(2025, 11, 13, 12, 15, 30), result.End);
        Assert.Equal("Detailed Event", result.Text);
        Assert.Equal("#FF5733", result.Color);
        Assert.Equal(5, result.ResourceId);
    }

    [Fact]
    public async Task GetEventsAsync_WithMultipleResources_ReturnsAllEvents()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new EventService(context, CreateLogger<EventService>());

        var events = new[]
        {
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 10, 0, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "Resource 1", ResourceId = 1 },
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 10, 0, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "Resource 2", ResourceId = 2 },
            new SchedulerEvent { Id = Guid.NewGuid(), Start = new DateTime(2025, 11, 13, 10, 0, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "Resource 3", ResourceId = 3 }
        };
        context.Events.AddRange(events);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetEventsAsync(
            new DateTime(2025, 11, 13, 9, 0, 0),
            new DateTime(2025, 11, 13, 12, 0, 0)
        );

        // Assert
        Assert.Equal(3, result.Count());
        Assert.Contains(result, e => e.ResourceId == 1);
        Assert.Contains(result, e => e.ResourceId == 2);
        Assert.Contains(result, e => e.ResourceId == 3);
    }
}
