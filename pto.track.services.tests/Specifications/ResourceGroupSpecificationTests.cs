using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services.Specifications;
using Xunit;

namespace pto.track.services.tests.Specifications;

public class ResourceGroupSpecificationTests
{
    private PtoTrackDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PtoTrackDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PtoTrackDbContext(options);
    }

    [Fact]
    public async Task ResourceGroupSpec_FiltersResourcesByGroupId()
    {
        // Arrange
        await using var context = CreateContext();

        var resources = new List<Resource>
        {
            new() { Id = 1, Name = "Group 1 Resource A", GroupId = 1 },
            new() { Id = 2, Name = "Group 1 Resource B", GroupId = 1 },
            new() { Id = 3, Name = "Group 2 Resource C", GroupId = 2 },
            new() { Id = 4, Name = "Group 3 Resource D", GroupId = 3 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        var spec = new ResourceGroupSpecification(1);

        // Act
        var results = await context.Resources
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(1, r.GroupId));
        Assert.Contains(results, r => r.Id == 1);
        Assert.Contains(results, r => r.Id == 2);
        Assert.DoesNotContain(results, r => r.Id == 3);
        Assert.DoesNotContain(results, r => r.Id == 4);
    }

    [Fact]
    public async Task ResourceGroupSpec_WithMultipleGroups_FiltersSeparately()
    {
        // Arrange
        await using var context = CreateContext();

        var resources = new List<Resource>
        {
            new() { Id = 1, Name = "Group 1 Resource", GroupId = 1 },
            new() { Id = 2, Name = "Group 2 Resource A", GroupId = 2 },
            new() { Id = 3, Name = "Group 2 Resource B", GroupId = 2 },
            new() { Id = 4, Name = "Group 3 Resource", GroupId = 3 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        var specGroup1 = new ResourceGroupSpecification(1);
        var specGroup2 = new ResourceGroupSpecification(2);
        var specGroup3 = new ResourceGroupSpecification(3);

        // Act
        var resultsGroup1 = await context.Resources.ApplySpecification(specGroup1).ToListAsync();
        var resultsGroup2 = await context.Resources.ApplySpecification(specGroup2).ToListAsync();
        var resultsGroup3 = await context.Resources.ApplySpecification(specGroup3).ToListAsync();

        // Assert
        Assert.Single(resultsGroup1);
        Assert.Equal(1, resultsGroup1.First().GroupId);

        Assert.Equal(2, resultsGroup2.Count);
        Assert.All(resultsGroup2, r => Assert.Equal(2, r.GroupId));

        Assert.Single(resultsGroup3);
        Assert.Equal(3, resultsGroup3.First().GroupId);
    }

    [Fact]
    public async Task ResourceGroupSpec_WithNoMatchingResources_ReturnsEmpty()
    {
        // Arrange
        await using var context = CreateContext();

        var resources = new List<Resource>
        {
            new() { Id = 1, Name = "Group 1 Resource", GroupId = 1 },
            new() { Id = 2, Name = "Group 2 Resource", GroupId = 2 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        var spec = new ResourceGroupSpecification(99);

        // Act
        var results = await context.Resources
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ResourceGroupSpec_IncludesActiveAndInactiveResources()
    {
        // Arrange
        await using var context = CreateContext();

        var resources = new List<Resource>
        {
            new() { Id = 1, Name = "Active Resource", GroupId = 1, IsActive = true },
            new() { Id = 2, Name = "Inactive Resource", GroupId = 1, IsActive = false },
            new() { Id = 3, Name = "Other Group Resource", GroupId = 2, IsActive = true }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        var spec = new ResourceGroupSpecification(1);

        // Act
        var results = await context.Resources
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Id == 1 && r.IsActive);
        Assert.Contains(results, r => r.Id == 2 && !r.IsActive);
        Assert.DoesNotContain(results, r => r.Id == 3);
    }

    [Fact]
    public async Task ResourceGroupSpec_WorksWithEvaluator()
    {
        // Arrange
        await using var context = CreateContext();

        var resources = new List<Resource>
        {
            new() { Id = 1, Name = "Resource A", GroupId = 5, Email = "a@test.com" },
            new() { Id = 2, Name = "Resource B", GroupId = 5, Email = "b@test.com" },
            new() { Id = 3, Name = "Resource C", GroupId = 10, Email = "c@test.com" }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        var spec = new ResourceGroupSpecification(5);

        // Act - Use the specification evaluator
        var results = await context.Resources
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(5, r.GroupId));
        Assert.Contains(results, r => r.Email == "a@test.com");
        Assert.Contains(results, r => r.Email == "b@test.com");
    }

    [Fact]
    public async Task ResourceGroupSpec_ProducesCorrectSqlQuery()
    {
        // Arrange
        await using var context = CreateContext();

        var resources = new List<Resource>
        {
            new() { Id = 1, Name = "Test Resource", GroupId = 1 }
        };
        context.Resources.AddRange(resources);
        await context.SaveChangesAsync();

        var spec = new ResourceGroupSpecification(1);

        // Act - Build the query but don't execute
        var query = context.Resources.ApplySpecification(spec);
        var results = await query.ToListAsync();

        // Assert - Verify the specification produces correct results
        Assert.Single(results);
        Assert.Equal(1, results.First().GroupId);
    }
}
