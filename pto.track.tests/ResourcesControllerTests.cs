using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pto.track.Controllers;
using pto.track.data;
using pto.track.services;
using Xunit;

namespace pto.track.tests
{
    public class ResourcesControllerTests : TestBase
    {

        [Fact]
        public async Task GetResources_ReturnsAllResources()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var resources = new[]
            {
                new SchedulerResource { Id = 1, Name = "Resource A" },
                new SchedulerResource { Id = 2, Name = "Resource B" },
                new SchedulerResource { Id = 3, Name = "Resource C" }
            };

            context.Resources.AddRange(resources);
            await context.SaveChangesAsync();

            var service = new ResourceService(context);
            var controller = new ResourcesController(service);

            // Act
            var result = await controller.GetResources();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResources = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(okResult.Value);
            Assert.Equal(3, returnedResources.Count());
        }

        [Fact]
        public async Task GetResources_WithNoResources_ReturnsEmptyList()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var service = new ResourceService(context);
            var controller = new ResourcesController(service);

            // Act
            var result = await controller.GetResources();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResources = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(okResult.Value);
            Assert.Empty(returnedResources);
        }

        [Fact]
        public async Task GetResources_ReturnsResourcesWithCorrectData()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var resources = new[]
            {
                new SchedulerResource { Id = 1, Name = "Resource A" },
                new SchedulerResource { Id = 2, Name = "Resource B" }
            };

            context.Resources.AddRange(resources);
            await context.SaveChangesAsync();

            var service = new ResourceService(context);
            var controller = new ResourcesController(service);

            // Act
            var result = await controller.GetResources();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResources = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(okResult.Value).ToList();
            Assert.Equal("Resource A", returnedResources[0].Name);
            Assert.Equal("Resource B", returnedResources[1].Name);
        }
    }
}
