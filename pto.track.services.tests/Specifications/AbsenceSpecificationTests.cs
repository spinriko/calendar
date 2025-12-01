using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services.Specifications;

namespace pto.track.services.tests.Specifications;

public class AbsenceSpecificationTests
{
    private PtoTrackDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PtoTrackDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PtoTrackDbContext(options);
    }

    [Fact]
    public async Task AbsencesByDateRangeSpec_FiltersCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Resource { Id = 1, Name = "Employee", GroupId = 1 };
        context.Resources.Add(employee);

        var requests = new List<AbsenceRequest>
        {
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 10), End = new DateTime(2025, 1, 12), EmployeeId = 1, Reason = "In range", Status = AbsenceStatus.Pending },
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 2, 10), End = new DateTime(2025, 2, 12), EmployeeId = 1, Reason = "Out of range", Status = AbsenceStatus.Pending },
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 5), End = new DateTime(2025, 1, 8), EmployeeId = 1, Reason = "Before range", Status = AbsenceStatus.Pending }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new AbsencesByDateRangeSpec(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

        // Act
        var results = await context.AbsenceRequests
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(2, results.Count); // Should get "In range" and "Before range" (partial overlap)
        Assert.Contains(results, r => r.Reason == "In range");
        Assert.Contains(results, r => r.Reason == "Before range");
        Assert.DoesNotContain(results, r => r.Reason == "Out of range");
    }

    [Fact]
    public async Task AbsencesByStatusSpec_FiltersMultipleStatuses()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Resource { Id = 1, Name = "Employee", GroupId = 1 };
        context.Resources.Add(employee);

        var requests = new List<AbsenceRequest>
        {
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Pending", Status = AbsenceStatus.Pending },
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Approved", Status = AbsenceStatus.Approved },
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Rejected", Status = AbsenceStatus.Rejected },
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Cancelled", Status = AbsenceStatus.Cancelled }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new AbsencesByStatusSpec(new List<AbsenceStatus> { AbsenceStatus.Pending, AbsenceStatus.Approved });

        // Act
        var results = await context.AbsenceRequests
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Status == AbsenceStatus.Pending);
        Assert.Contains(results, r => r.Status == AbsenceStatus.Approved);
        Assert.DoesNotContain(results, r => r.Status == AbsenceStatus.Rejected);
        Assert.DoesNotContain(results, r => r.Status == AbsenceStatus.Cancelled);
    }

    [Fact]
    public async Task AbsencesByEmployeeSpec_FiltersCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var employee1 = new Resource { Id = 1, Name = "Employee 1", GroupId = 1 };
        var employee2 = new Resource { Id = 2, Name = "Employee 2", GroupId = 1 };
        context.Resources.AddRange(employee1, employee2);

        var requests = new List<AbsenceRequest>
        {
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Employee 1 Request", Status = AbsenceStatus.Pending },
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 2, Reason = "Employee 2 Request", Status = AbsenceStatus.Pending }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new AbsencesByEmployeeSpec(1);

        // Act
        var results = await context.AbsenceRequests
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Employee 1 Request", results[0].Reason);
    }

    [Fact]
    public async Task AbsencesFilteredSpec_CombinesMultipleFilters()
    {
        // Arrange
        await using var context = CreateContext();
        var employee1 = new Resource { Id = 1, Name = "Employee 1", GroupId = 1 };
        var employee2 = new Resource { Id = 2, Name = "Employee 2", GroupId = 1 };
        context.Resources.AddRange(employee1, employee2);

        var requests = new List<AbsenceRequest>
        {
            // Employee 1, January, Pending
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 10), End = new DateTime(2025, 1, 12), EmployeeId = 1, Reason = "Match", Status = AbsenceStatus.Pending },
            // Employee 1, January, Approved (wrong status)
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 15), End = new DateTime(2025, 1, 17), EmployeeId = 1, Reason = "Wrong status", Status = AbsenceStatus.Approved },
            // Employee 2, January, Pending (wrong employee)
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 20), End = new DateTime(2025, 1, 22), EmployeeId = 2, Reason = "Wrong employee", Status = AbsenceStatus.Pending },
            // Employee 1, February, Pending (wrong date)
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 2, 10), End = new DateTime(2025, 2, 12), EmployeeId = 1, Reason = "Wrong date", Status = AbsenceStatus.Pending }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new AbsencesFilteredSpec(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            new List<AbsenceStatus> { AbsenceStatus.Pending },
            1);

        // Act
        var results = await context.AbsenceRequests
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Match", results[0].Reason);
    }

    [Fact]
    public async Task AbsencesFilteredSpec_WithNullStatuses_ReturnsAllStatuses()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Resource { Id = 1, Name = "Employee", GroupId = 1 };
        context.Resources.Add(employee);

        var requests = new List<AbsenceRequest>
        {
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 10), End = new DateTime(2025, 1, 12), EmployeeId = 1, Reason = "Pending", Status = AbsenceStatus.Pending },
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 15), End = new DateTime(2025, 1, 17), EmployeeId = 1, Reason = "Approved", Status = AbsenceStatus.Approved },
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 20), End = new DateTime(2025, 1, 22), EmployeeId = 1, Reason = "Rejected", Status = AbsenceStatus.Rejected }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new AbsencesFilteredSpec(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), null, null);

        // Act
        var results = await context.AbsenceRequests
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task PendingAbsencesSpec_ReturnsOnlyPending()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Resource { Id = 1, Name = "Employee", GroupId = 1 };
        context.Resources.Add(employee);

        var requests = new List<AbsenceRequest>
        {
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Pending 1", Status = AbsenceStatus.Pending, RequestedDate = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Pending 2", Status = AbsenceStatus.Pending, RequestedDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Approved", Status = AbsenceStatus.Approved }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new PendingAbsencesSpec();

        // Act
        var results = await context.AbsenceRequests
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(AbsenceStatus.Pending, r.Status));
        // Verify ordering by RequestedDate
        Assert.Equal("Pending 1", results[0].Reason);
        Assert.Equal("Pending 2", results[1].Reason);
    }

    [Fact]
    public async Task AbsenceByIdSpec_ReturnsCorrectAbsence()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Resource { Id = 1, Name = "Employee", GroupId = 1 };
        context.Resources.Add(employee);

        var targetId = Guid.NewGuid();
        var requests = new List<AbsenceRequest>
        {
            new() { Id = targetId, Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Target", Status = AbsenceStatus.Pending },
            new() { Id = Guid.NewGuid(), Start = DateTime.UtcNow, End = DateTime.UtcNow.AddDays(1), EmployeeId = 1, Reason = "Other", Status = AbsenceStatus.Pending }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new AbsenceByIdSpec(targetId);

        // Act
        var result = await context.AbsenceRequests
            .ApplySpecification(spec)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetId, result.Id);
        Assert.Equal("Target", result.Reason);
    }

    [Fact]
    public async Task Specifications_IncludeNavigationProperties()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Resource { Id = 1, Name = "Test Employee", GroupId = 1 };
        var approver = new Resource { Id = 2, Name = "Test Approver", GroupId = 1 };
        context.Resources.AddRange(employee, approver);

        var absence = new AbsenceRequest
        {
            Id = Guid.NewGuid(),
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddDays(1),
            EmployeeId = 1,
            ApproverId = 2,
            Reason = "Test",
            Status = AbsenceStatus.Approved
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        var spec = new AbsenceByIdSpec(absence.Id);

        // Act
        var result = await context.AbsenceRequests
            .ApplySpecification(spec)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Employee);
        Assert.Equal("Test Employee", result.Employee.Name);
        Assert.NotNull(result.Approver);
        Assert.Equal("Test Approver", result.Approver.Name);
    }

    [Fact]
    public async Task AbsencesFilteredSpec_WithEmptyStatusList_ReturnsAllStatuses()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Resource { Id = 1, Name = "Employee", GroupId = 1 };
        context.Resources.Add(employee);

        var requests = new List<AbsenceRequest>
        {
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 10), End = new DateTime(2025, 1, 12), EmployeeId = 1, Reason = "Pending", Status = AbsenceStatus.Pending },
            new() { Id = Guid.NewGuid(), Start = new DateTime(2025, 1, 15), End = new DateTime(2025, 1, 17), EmployeeId = 1, Reason = "Approved", Status = AbsenceStatus.Approved }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var spec = new AbsencesFilteredSpec(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), new List<AbsenceStatus>(), null);

        // Act
        var results = await context.AbsenceRequests
            .ApplySpecification(spec)
            .ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
    }
}
