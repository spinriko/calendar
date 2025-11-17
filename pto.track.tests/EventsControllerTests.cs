using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pto.track.Controllers;
using pto.track.data;
using pto.track.services;
using Xunit;

namespace pto.track.tests
{
    public class EventsControllerTests : TestBase
    {


        [Fact]
        public async Task GetSchedulerEvents_WithValidDateRange_ReturnsMatchingEvents()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var start = new DateTime(2025, 11, 13, 9, 0, 0);
            var end = new DateTime(2025, 11, 13, 17, 0, 0);

            var events = new[]
            {
                new SchedulerEvent { Id = 1, Start = new DateTime(2025, 11, 13, 10, 0, 0), End = new DateTime(2025, 11, 13, 11, 0, 0), Text = "Event 1", ResourceId = 1 },
                new SchedulerEvent { Id = 2, Start = new DateTime(2025, 11, 13, 14, 0, 0), End = new DateTime(2025, 11, 13, 15, 0, 0), Text = "Event 2", ResourceId = 1 },
                new SchedulerEvent { Id = 3, Start = new DateTime(2025, 11, 14, 10, 0, 0), End = new DateTime(2025, 11, 14, 11, 0, 0), Text = "Event 3", ResourceId = 1 }
            };

            context.Events.AddRange(events);
            await context.SaveChangesAsync();

            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act
            var result = await controller.GetSchedulerEvents(start, end);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEvents = Assert.IsAssignableFrom<IEnumerable<EventDto>>(okResult.Value);
            Assert.Equal(2, returnedEvents.Count());
        }

        [Fact]
        public async Task GetSchedulerEvent_WithValidId_ReturnsEvent()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var eventId = 1;
            var testEvent = new SchedulerEvent
            {
                Id = eventId,
                Start = new DateTime(2025, 11, 13, 10, 0, 0),
                End = new DateTime(2025, 11, 13, 11, 0, 0),
                Text = "Test Event",
                ResourceId = 1
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act
            var result = await controller.GetSchedulerEvent(eventId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEvent = Assert.IsType<EventDto>(okResult.Value);
            Assert.Equal(eventId, returnedEvent.Id);
            Assert.Equal("Test Event", returnedEvent.Text);
        }

        [Fact]
        public async Task GetSchedulerEvent_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act
            var result = await controller.GetSchedulerEvent(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostSchedulerEvent_WithValidEvent_ReturnsCreatedAtAction()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var newEvent = new CreateEventDto(
                Start: new DateTime(2025, 11, 13, 10, 0, 0),
                End: new DateTime(2025, 11, 13, 11, 0, 0),
                Text: "New Event",
                Color: null,
                ResourceId: 1
            );

            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act
            var result = await controller.PostSchedulerEvent(newEvent);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("GetSchedulerEvent", createdResult.ActionName);
            var returnedEvent = Assert.IsType<EventDto>(createdResult.Value);
            Assert.NotEqual(0, returnedEvent.Id);
        }

        [Fact]
        public async Task PutSchedulerEvent_WithValidIdAndEvent_ReturnsNoContent()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var eventId = 1;
            var testEvent = new SchedulerEvent
            {
                Id = eventId,
                Start = new DateTime(2025, 11, 13, 10, 0, 0),
                End = new DateTime(2025, 11, 13, 11, 0, 0),
                Text = "Original Event",
                ResourceId = 1
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var updateDto = new UpdateEventDto(
                Start: new DateTime(2025, 11, 13, 11, 0, 0),
                End: new DateTime(2025, 11, 13, 12, 0, 0),
                Text: "Updated Event",
                Color: null,
                ResourceId: 1
            );

            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act
            var result = await controller.PutSchedulerEvent(eventId, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify the update
            var verifyEvent = await context.Events.FindAsync(eventId);
            Assert.NotNull(verifyEvent);
            Assert.Equal("Updated Event", verifyEvent.Text);
        }

        [Fact]
        public async Task PutSchedulerEvent_WithMismatchedId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var updateDto = new UpdateEventDto(
                Start: DateTime.Now,
                End: DateTime.Now.AddHours(1),
                Text: null,
                Color: null,
                ResourceId: 1
            );
            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act - passing id=1 but no event exists
            var result = await controller.PutSchedulerEvent(1, updateDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteSchedulerEvent_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var eventId = 1;
            var testEvent = new SchedulerEvent
            {
                Id = eventId,
                Start = new DateTime(2025, 11, 13, 10, 0, 0),
                End = new DateTime(2025, 11, 13, 11, 0, 0),
                Text = "Event to Delete",
                ResourceId = 1
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act
            var result = await controller.DeleteSchedulerEvent(eventId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify deletion
            var deletedEvent = await context.Events.FindAsync(eventId);
            Assert.Null(deletedEvent);
        }

        [Fact]
        public async Task DeleteSchedulerEvent_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var service = new EventService(context);
            var controller = new EventsController(service);

            // Act
            var result = await controller.DeleteSchedulerEvent(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
