using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;

namespace pto.track.services.tests;

public class AbsenceServiceTests : TestBase
{
    [Fact]
    public async Task GetAbsenceRequestsAsync_WithRequestsInDateRange_ReturnsMatchingRequests()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "John Doe" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence1 = new AbsenceRequest
        {
            Start = new DateTime(2025, 1, 10),
            End = new DateTime(2025, 1, 12),
            Reason = "Vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        var absence2 = new AbsenceRequest
        {
            Start = new DateTime(2025, 1, 15),
            End = new DateTime(2025, 1, 17),
            Reason = "Sick leave",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            RequestedDate = DateTime.UtcNow
        };
        var absence3 = new AbsenceRequest
        {
            Start = new DateTime(2025, 2, 1),
            End = new DateTime(2025, 2, 3),
            Reason = "Conference",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };

        context.AbsenceRequests.AddRange(absence1, absence2, absence3);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestsAsync(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, a => a.Reason == "Vacation");
        Assert.Contains(result, a => a.Reason == "Sick leave");
        Assert.DoesNotContain(result, a => a.Reason == "Conference");
    }

    [Fact]
    public async Task GetAbsenceRequestsAsync_WithNoRequestsInRange_ReturnsEmpty()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Jane Doe" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 6, 1),
            End = new DateTime(2025, 6, 5),
            Reason = "Summer vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestsAsync(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAbsenceRequestsByEmployeeAsync_WithEmployeeId_ReturnsOnlyEmployeeRequests()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee1 = new SchedulerResource { Id = 1, Name = "Alice" };
        var employee2 = new SchedulerResource { Id = 2, Name = "Bob" };
        context.Resources.AddRange(employee1, employee2);
        await context.SaveChangesAsync();

        var absence1 = new AbsenceRequest
        {
            Start = new DateTime(2025, 1, 10),
            End = new DateTime(2025, 1, 12),
            Reason = "Alice vacation",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        var absence2 = new AbsenceRequest
        {
            Start = new DateTime(2025, 1, 15),
            End = new DateTime(2025, 1, 17),
            Reason = "Bob vacation",
            EmployeeId = 2,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };

        context.AbsenceRequests.AddRange(absence1, absence2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestsByEmployeeAsync(1, new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

        // Assert
        Assert.Single(result);
        Assert.Equal("Alice vacation", result.First().Reason);
        Assert.Equal(1, result.First().EmployeeId);
    }

    [Fact]
    public async Task GetPendingAbsenceRequestsAsync_ReturnsOnlyPendingRequests()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Charlie" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var pending1 = new AbsenceRequest
        {
            Start = new DateTime(2025, 1, 10),
            End = new DateTime(2025, 1, 12),
            Reason = "Pending 1",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow.AddDays(-2)
        };
        var pending2 = new AbsenceRequest
        {
            Start = new DateTime(2025, 1, 15),
            End = new DateTime(2025, 1, 17),
            Reason = "Pending 2",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow.AddDays(-1)
        };
        var approved = new AbsenceRequest
        {
            Start = new DateTime(2025, 1, 20),
            End = new DateTime(2025, 1, 22),
            Reason = "Approved",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            RequestedDate = DateTime.UtcNow.AddDays(-3)
        };

        context.AbsenceRequests.AddRange(pending1, pending2, approved);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetPendingAbsenceRequestsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, a => Assert.Equal("Pending", a.Status));
        Assert.DoesNotContain(result, a => a.Reason == "Approved");
    }

    [Fact]
    public async Task GetAbsenceRequestByIdAsync_WithValidId_ReturnsRequest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Diana" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 3, 1),
            End = new DateTime(2025, 3, 5),
            Reason = "Spring break",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestByIdAsync(absence.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Spring break", result.Reason);
        Assert.Equal("Diana", result.EmployeeName);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task GetAbsenceRequestByIdAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        // Act & Assert
        await Assert.ThrowsAsync<AbsenceNotFoundException>(
            () => service.GetAbsenceRequestByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_WithValidDto_CreatesAndReturnsRequest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Eve" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var dto = new CreateAbsenceRequestDto(
            Start: new DateTime(2025, 4, 10),
            End: new DateTime(2025, 4, 15),
            Reason: "Family vacation",
            EmployeeId: 1
        );

        // Act
        var result = await service.CreateAbsenceRequestAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Family vacation", result.Reason);
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal("Eve", result.EmployeeName);
        Assert.Equal("Pending", result.Status);
        Assert.Null(result.ApproverId);

        var savedAbsence = await context.AbsenceRequests.FindAsync(result.Id);
        Assert.NotNull(savedAbsence);
        Assert.Equal(AbsenceStatus.Pending, savedAbsence.Status);
    }

    [Fact]
    public async Task UpdateAbsenceRequestAsync_WithValidIdAndDto_UpdatesRequest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Frank" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 5, 1),
            End = new DateTime(2025, 5, 3),
            Reason = "Original reason",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        var updateDto = new UpdateAbsenceRequestDto(
            Start: new DateTime(2025, 5, 2),
            End: new DateTime(2025, 5, 5),
            Reason: "Updated reason"
        );

        // Act
        var result = await service.UpdateAbsenceRequestAsync(absence.Id, updateDto);

        // Assert
        Assert.True(result.Success);

        var updated = await context.AbsenceRequests.FindAsync(absence.Id);
        Assert.NotNull(updated);
        Assert.Equal(new DateTime(2025, 5, 2), updated.Start);
        Assert.Equal(new DateTime(2025, 5, 5), updated.End);
        Assert.Equal("Updated reason", updated.Reason);
    }

    [Fact]
    public async Task UpdateAbsenceRequestAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var updateDto = new UpdateAbsenceRequestDto(
            Start: new DateTime(2025, 5, 2),
            End: new DateTime(2025, 5, 5),
            Reason: "Updated reason"
        );

        // Act & Assert
        await Assert.ThrowsAsync<AbsenceNotFoundException>(
            () => service.UpdateAbsenceRequestAsync(Guid.NewGuid(), updateDto));
    }

    [Fact]
    public async Task UpdateAbsenceRequestAsync_WithApprovedRequest_ReturnsFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Grace" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 5, 1),
            End = new DateTime(2025, 5, 3),
            Reason = "Already approved",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            RequestedDate = DateTime.UtcNow,
            ApproverId = 1,
            ApprovedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        var updateDto = new UpdateAbsenceRequestDto(
            Start: new DateTime(2025, 5, 2),
            End: new DateTime(2025, 5, 5),
            Reason: "Try to update"
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidAbsenceOperationException>(
            () => service.UpdateAbsenceRequestAsync(absence.Id, updateDto));
    }

    [Fact]
    public async Task ApproveAbsenceRequestAsync_WithValidIdAndDto_ApprovesRequest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Henry" };
        var approver = new SchedulerResource { Id = 2, Name = "Manager" };
        context.Resources.AddRange(employee, approver);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 6, 1),
            End = new DateTime(2025, 6, 5),
            Reason = "To be approved",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        var approveDto = new ApproveAbsenceRequestDto(
            ApproverId: 2,
            Comments: "Approved for business reasons"
        );

        // Act
        var result = await service.ApproveAbsenceRequestAsync(absence.Id, approveDto);

        // Assert
        Assert.True(result.Success);

        var approved = await context.AbsenceRequests.FindAsync(absence.Id);
        Assert.NotNull(approved);
        Assert.Equal(AbsenceStatus.Approved, approved.Status);
        Assert.Equal(2, approved.ApproverId);
        Assert.NotNull(approved.ApprovedDate);
        Assert.Equal("Approved for business reasons", approved.ApprovalComments);
    }

    [Fact]
    public async Task ApproveAbsenceRequestAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var approveDto = new ApproveAbsenceRequestDto(
            ApproverId: 1,
            Comments: "Approved"
        );

        // Act & Assert
        await Assert.ThrowsAsync<AbsenceNotFoundException>(
            () => service.ApproveAbsenceRequestAsync(Guid.NewGuid(), approveDto));
    }

    [Fact]
    public async Task ApproveAbsenceRequestAsync_WithAlreadyApproved_ReturnsFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Ivy" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 6, 1),
            End = new DateTime(2025, 6, 5),
            Reason = "Already approved",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            RequestedDate = DateTime.UtcNow,
            ApproverId = 1,
            ApprovedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        var approveDto = new ApproveAbsenceRequestDto(
            ApproverId: 2,
            Comments: "Try again"
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidAbsenceOperationException>(
            () => service.ApproveAbsenceRequestAsync(absence.Id, approveDto));
    }

    [Fact]
    public async Task RejectAbsenceRequestAsync_WithValidIdAndDto_RejectsRequest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Jack" };
        var approver = new SchedulerResource { Id = 2, Name = "Manager" };
        context.Resources.AddRange(employee, approver);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 7, 1),
            End = new DateTime(2025, 7, 5),
            Reason = "To be rejected",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        var rejectDto = new RejectAbsenceRequestDto(
            ApproverId: 2,
            Reason: "Insufficient staffing"
        );

        // Act
        var result = await service.RejectAbsenceRequestAsync(absence.Id, rejectDto);

        // Assert
        Assert.True(result.Success);

        var rejected = await context.AbsenceRequests.FindAsync(absence.Id);
        Assert.NotNull(rejected);
        Assert.Equal(AbsenceStatus.Rejected, rejected.Status);
        Assert.Equal(2, rejected.ApproverId);
        Assert.NotNull(rejected.ApprovedDate);
        Assert.Equal("Insufficient staffing", rejected.ApprovalComments);
    }

    [Fact]
    public async Task RejectAbsenceRequestAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var rejectDto = new RejectAbsenceRequestDto(
            ApproverId: 1,
            Reason: "Rejected"
        );

        // Act & Assert
        await Assert.ThrowsAsync<AbsenceNotFoundException>(
            () => service.RejectAbsenceRequestAsync(Guid.NewGuid(), rejectDto));
    }

    [Fact]
    public async Task CancelAbsenceRequestAsync_WithValidIdAndEmployeeId_CancelsRequest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Karen" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 8, 1),
            End = new DateTime(2025, 8, 5),
            Reason = "To be cancelled",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CancelAbsenceRequestAsync(absence.Id, 1);

        // Assert
        Assert.True(result.Success);

        var cancelled = await context.AbsenceRequests.FindAsync(absence.Id);
        Assert.NotNull(cancelled);
        Assert.Equal(AbsenceStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelAbsenceRequestAsync_WithWrongEmployeeId_ReturnsFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Leo" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 8, 1),
            End = new DateTime(2025, 8, 5),
            Reason = "Someone else's request",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAbsenceAccessException>(
            () => service.CancelAbsenceRequestAsync(absence.Id, 999));
    }

    [Fact]
    public async Task DeleteAbsenceRequestAsync_WithValidId_DeletesRequest()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Mia" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 9, 1),
            End = new DateTime(2025, 9, 5),
            Reason = "To be deleted",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();
        var absenceId = absence.Id;

        // Act
        var result = await service.DeleteAbsenceRequestAsync(absenceId);

        // Assert
        Assert.True(result.Success);

        var deleted = await context.AbsenceRequests.FindAsync(absenceId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAbsenceRequestAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        // Act & Assert
        await Assert.ThrowsAsync<AbsenceNotFoundException>(
            () => service.DeleteAbsenceRequestAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_MapsEmployeeNameCorrectly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 5, Name = "Noah Smith" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var dto = new CreateAbsenceRequestDto(
            Start: new DateTime(2025, 10, 1),
            End: new DateTime(2025, 10, 5),
            Reason: "Mapping test",
            EmployeeId: 5
        );

        // Act
        var result = await service.CreateAbsenceRequestAsync(dto);

        // Assert
        Assert.Equal("Noah Smith", result.EmployeeName);
        Assert.Equal(5, result.EmployeeId);
    }

    [Fact]
    public async Task GetAbsenceRequestByIdAsync_WithApproverInfo_ReturnsCompleteData()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Olivia" };
        var approver = new SchedulerResource { Id = 2, Name = "Manager Smith" };
        context.Resources.AddRange(employee, approver);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = new DateTime(2025, 11, 1),
            End = new DateTime(2025, 11, 5),
            Reason = "Complete data test",
            EmployeeId = 1,
            Status = AbsenceStatus.Approved,
            RequestedDate = DateTime.UtcNow.AddDays(-5),
            ApproverId = 2,
            ApprovedDate = DateTime.UtcNow.AddDays(-2),
            ApprovalComments = "All good"
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestByIdAsync(absence.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Olivia", result.EmployeeName);
        Assert.Equal("Manager Smith", result.ApproverName);
        Assert.Equal(2, result.ApproverId);
        Assert.Equal("All good", result.ApprovalComments);
        Assert.NotNull(result.ApprovedDate);
        Assert.Equal("Approved", result.Status);
    }

    [Fact]
    public async Task GetAbsenceRequestsAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Test User" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var requests = new[]
        {
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 10),
                End = new DateTime(2025, 1, 12),
                Reason = "Pending request",
                EmployeeId = 1,
                Status = AbsenceStatus.Pending,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 15),
                End = new DateTime(2025, 1, 17),
                Reason = "Approved request",
                EmployeeId = 1,
                Status = AbsenceStatus.Approved,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 20),
                End = new DateTime(2025, 1, 22),
                Reason = "Another approved",
                EmployeeId = 1,
                Status = AbsenceStatus.Approved,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 25),
                End = new DateTime(2025, 1, 27),
                Reason = "Rejected request",
                EmployeeId = 1,
                Status = AbsenceStatus.Rejected,
                RequestedDate = DateTime.UtcNow
            }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestsAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            new List<AbsenceStatus> { AbsenceStatus.Approved });

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, a => Assert.Equal("Approved", a.Status));
        Assert.Contains(list, a => a.Reason == "Approved request");
        Assert.Contains(list, a => a.Reason == "Another approved");
    }

    [Fact]
    public async Task GetAbsenceRequestsAsync_WithPendingStatusFilter_ReturnsOnlyPending()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Test User" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var requests = new[]
        {
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 10),
                End = new DateTime(2025, 1, 12),
                Reason = "Pending 1",
                EmployeeId = 1,
                Status = AbsenceStatus.Pending,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 15),
                End = new DateTime(2025, 1, 17),
                Reason = "Approved",
                EmployeeId = 1,
                Status = AbsenceStatus.Approved,
                RequestedDate = DateTime.UtcNow
            }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestsAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            new List<AbsenceStatus> { AbsenceStatus.Pending });

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("Pending", list[0].Status);
        Assert.Equal("Pending 1", list[0].Reason);
    }

    [Fact]
    public async Task GetAbsenceRequestsAsync_WithNullStatus_ReturnsAllStatuses()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Test User" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var requests = new[]
        {
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 10),
                End = new DateTime(2025, 1, 12),
                Reason = "Pending",
                EmployeeId = 1,
                Status = AbsenceStatus.Pending,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 15),
                End = new DateTime(2025, 1, 17),
                Reason = "Approved",
                EmployeeId = 1,
                Status = AbsenceStatus.Approved,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 20),
                End = new DateTime(2025, 1, 22),
                Reason = "Rejected",
                EmployeeId = 1,
                Status = AbsenceStatus.Rejected,
                RequestedDate = DateTime.UtcNow
            }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestsAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            null);

        // Assert
        var list = result.ToList();
        Assert.Equal(3, list.Count);
        Assert.Contains(list, a => a.Status == "Pending");
        Assert.Contains(list, a => a.Status == "Approved");
        Assert.Contains(list, a => a.Status == "Rejected");
    }

    [Fact]
    public async Task GetAbsenceRequestsAsync_WithCancelledStatusFilter_ReturnsCancelled()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Test User" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var requests = new[]
        {
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 10),
                End = new DateTime(2025, 1, 12),
                Reason = "Cancelled request",
                EmployeeId = 1,
                Status = AbsenceStatus.Cancelled,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 15),
                End = new DateTime(2025, 1, 17),
                Reason = "Approved request",
                EmployeeId = 1,
                Status = AbsenceStatus.Approved,
                RequestedDate = DateTime.UtcNow
            }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAbsenceRequestsAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            new List<AbsenceStatus> { AbsenceStatus.Cancelled });

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("Cancelled", list[0].Status);
        Assert.Equal("Cancelled request", list[0].Reason);
    }

    [Fact]
    public async Task GetAbsenceRequestsAsync_WithStatusAndDateRange_AppliesBothFilters()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Test User" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var requests = new[]
        {
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 10),
                End = new DateTime(2025, 1, 12),
                Reason = "January Approved",
                EmployeeId = 1,
                Status = AbsenceStatus.Approved,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 2, 15),
                End = new DateTime(2025, 2, 17),
                Reason = "February Approved",
                EmployeeId = 1,
                Status = AbsenceStatus.Approved,
                RequestedDate = DateTime.UtcNow
            },
            new AbsenceRequest
            {
                Start = new DateTime(2025, 1, 20),
                End = new DateTime(2025, 1, 22),
                Reason = "January Pending",
                EmployeeId = 1,
                Status = AbsenceStatus.Pending,
                RequestedDate = DateTime.UtcNow
            }
        };
        context.AbsenceRequests.AddRange(requests);
        await context.SaveChangesAsync();

        // Act - Get approved requests in January only
        var result = await service.GetAbsenceRequestsAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            new List<AbsenceStatus> { AbsenceStatus.Approved });

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("January Approved", list[0].Reason);
        Assert.Equal("Approved", list[0].Status);
    }

    [Fact]
    public async Task GetAbsenceRequestsAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetAbsenceRequestsAsync(
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(30),
                null,
                cts.Token));
    }

    [Fact]
    public async Task CreateAbsenceRequestAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Test" };
        context.Resources.Add(employee);
        await context.SaveChangesAsync();

        var dto = new CreateAbsenceRequestDto(
            Start: DateTime.UtcNow.AddDays(10),
            End: DateTime.UtcNow.AddDays(12),
            Reason: "Test",
            EmployeeId: 1
        );

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.CreateAbsenceRequestAsync(dto, cts.Token));
    }

    [Fact]
    public async Task ApproveAbsenceRequestAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AbsenceService(context, CreateLogger<AbsenceService>(), CreateMapper(), CreateUnitOfWork(context));

        var employee = new SchedulerResource { Id = 1, Name = "Test" };
        var approver = new SchedulerResource { Id = 2, Name = "Manager" };
        context.Resources.AddRange(employee, approver);
        await context.SaveChangesAsync();

        var absence = new AbsenceRequest
        {
            Start = DateTime.UtcNow.AddDays(10),
            End = DateTime.UtcNow.AddDays(12),
            Reason = "Test",
            EmployeeId = 1,
            Status = AbsenceStatus.Pending,
            RequestedDate = DateTime.UtcNow
        };
        context.AbsenceRequests.Add(absence);
        await context.SaveChangesAsync();

        var dto = new ApproveAbsenceRequestDto(ApproverId: 2, Comments: "OK");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ApproveAbsenceRequestAsync(absence.Id, dto, cts.Token));
    }
}
