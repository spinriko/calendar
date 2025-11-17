using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pto.track.Controllers;
using pto.track.data;
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

            var controller = new EventsController(context);

            // Act
            var result = await controller.GetSchedulerEvents(start, end);

            // Assert
            var returnedEvents = Assert.IsAssignableFrom<IEnumerable<SchedulerEvent>>(result.Value);
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

            var controller = new EventsController(context);

            // Act
            var result = await controller.GetSchedulerEvent(eventId);

            // Assert
            var returnedEvent = Assert.IsType<SchedulerEvent>(result.Value);
            Assert.Equal(eventId, returnedEvent.Id);
            Assert.Equal("Test Event", returnedEvent.Text);
        }

        [Fact]
        public async Task GetSchedulerEvent_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var controller = new EventsController(context);

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
            var newEvent = new SchedulerEvent
            {
                Start = new DateTime(2025, 11, 13, 10, 0, 0),
                End = new DateTime(2025, 11, 13, 11, 0, 0),
                Text = "New Event",
                ResourceId = 1
            };

            var controller = new EventsController(context);

            // Act
            var result = await controller.PostSchedulerEvent(newEvent);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("GetSchedulerEvent", createdResult.ActionName);
            var returnedEvent = Assert.IsType<SchedulerEvent>(createdResult.Value);
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

            // Detach the original event so we can attach an updated one
            context.Entry(testEvent).State = EntityState.Detached;

            var updatedEvent = new SchedulerEvent
            {
                Id = eventId,
                Start = new DateTime(2025, 11, 13, 11, 0, 0),
                End = new DateTime(2025, 11, 13, 12, 0, 0),
                Text = "Updated Event",
                ResourceId = 1
            };

            var controller = new EventsController(context);

            // Act
            var result = await controller.PutSchedulerEvent(eventId, updatedEvent);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify the update
            var verifyEvent = await context.Events.FindAsync(eventId);
            Assert.Equal("Updated Event", verifyEvent.Text);
        }

        [Fact]
        public async Task PutSchedulerEvent_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var event_ = new SchedulerEvent { Id = 2, Start = DateTime.Now, End = DateTime.Now.AddHours(1), ResourceId = 1 };
            var controller = new EventsController(context);

            // Act - passing id=1 but event has id=2
            var result = await controller.PutSchedulerEvent(1, event_);

            // Assert
            Assert.IsType<BadRequestResult>(result);
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

            var controller = new EventsController(context);

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
            var controller = new EventsController(context);

            // Act
            var result = await controller.DeleteSchedulerEvent(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
