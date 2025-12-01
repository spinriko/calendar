using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pto.track.Controllers;
using pto.track.data;
using pto.track.services;
using pto.track.services.DTOs;
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
                new Resource { Id = 1, Name = "Resource A" },
                new Resource { Id = 2, Name = "Resource B" },
                new Resource { Id = 3, Name = "Resource C" }
            };

            context.Resources.AddRange(resources);
            await context.SaveChangesAsync();

            var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());
            var controller = new ResourcesController(service, CreateLogger<ResourcesController>());

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
            var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());
            var controller = new ResourcesController(service, CreateLogger<ResourcesController>());

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
                new Resource { Id = 1, Name = "Resource A" },
                new Resource { Id = 2, Name = "Resource B" }
            };
            context.Resources.AddRange(resources);
            await context.SaveChangesAsync();

            var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());
            var controller = new ResourcesController(service, CreateLogger<ResourcesController>());

            // Act
            var result = await controller.GetResources();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResources = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(okResult.Value).ToList();
            Assert.Equal("Resource A", returnedResources[0].Name);
            Assert.Equal("Resource B", returnedResources[1].Name);
        }

        [Fact]
        public async Task GetResourcesByGroup_ReturnsOnlyGroupResources()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var resources = new[]
            {
                new Resource { Id = 1, Name = "Resource A", GroupId = 1 },
                new Resource { Id = 2, Name = "Resource B", GroupId = 2 },
                new Resource { Id = 3, Name = "Resource C", GroupId = 1 }
            };
            context.Resources.AddRange(resources);
            await context.SaveChangesAsync();

            var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());
            var controller = new ResourcesController(service, CreateLogger<ResourcesController>());

            // Act
            var result = await controller.GetResourcesByGroup(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResources = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(okResult.Value).ToList();
            Assert.Equal(2, returnedResources.Count);
            Assert.All(returnedResources, r => Assert.Equal(1, r.GroupId));
        }

        [Fact]
        public async Task GetResourcesByGroup_WithNoResources_ReturnsEmptyList()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var service = new ResourceService(context, CreateLogger<ResourceService>(), CreateMapper());
            var controller = new ResourcesController(service, CreateLogger<ResourcesController>());

            // Act
            var result = await controller.GetResourcesByGroup(99);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResources = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(okResult.Value);
            Assert.Empty(returnedResources);
        }
    }
}
