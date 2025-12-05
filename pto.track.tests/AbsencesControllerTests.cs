using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using pto.track.Controllers;
using pto.track.data;
using pto.track.services;
using pto.track.services.Authentication;
using pto.track.services.DTOs;
using Xunit;

namespace pto.track.tests;

public class AbsencesControllerTests
{
    private readonly Mock<IAbsenceService> _mockAbsenceService;
    private readonly Mock<ILogger<AbsencesController>> _mockLogger;
    private readonly Mock<IUserClaimsProvider> _mockClaimsProvider;
    private readonly Mock<IUserSyncService> _mockUserSync;

    public AbsencesControllerTests()
    {
        _mockAbsenceService = new Mock<IAbsenceService>();
        _mockLogger = new Mock<ILogger<AbsencesController>>();
        _mockClaimsProvider = new Mock<IUserClaimsProvider>();
        _mockUserSync = new Mock<IUserSyncService>();
    }

    private AbsencesController CreateController()
    {
        return new AbsencesController(
            _mockAbsenceService.Object,
            _mockLogger.Object,
            _mockClaimsProvider.Object,
            _mockUserSync.Object);
    }

    [Fact]
    public async Task GetAbsenceRequests_WithEmployeeId_ReturnsAbsencesForEmployee()
    {
        // Arrange
        var controller = CreateController();
        var employeeId = 123;
        var absences = new List<AbsenceRequestDto>
        {
            new AbsenceRequestDto(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Reason", employeeId, "Emp Name", "Pending", DateTime.UtcNow, null, null, null, null)
        };

        _mockAbsenceService.Setup(x => x.GetAbsenceRequestsByEmployeeAsync(employeeId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<AbsenceStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(absences);

        // Act
        var result = await controller.GetAbsenceRequests(null, null, employeeId, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAbsences = Assert.IsAssignableFrom<IEnumerable<AbsenceRequestDto>>(okResult.Value);
        Assert.Single(returnedAbsences);
        Assert.Equal(employeeId, returnedAbsences.First().EmployeeId);
    }

    [Fact]
    public async Task GetAbsenceRequests_WithDateRange_ReturnsFilteredAbsences()
    {
        // Arrange
        var controller = CreateController();
        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow.AddDays(7);
        var currentUserId = 1;

        var absences = new List<AbsenceRequestDto>
        {
            new AbsenceRequestDto(Guid.NewGuid(), start, end, "My Absence", currentUserId, "Me", "Pending", DateTime.UtcNow, null, null, null, null),
            new AbsenceRequestDto(Guid.NewGuid(), start, end, "Other Absence", 2, "Other", "Pending", DateTime.UtcNow, null, null, null, null),
            new AbsenceRequestDto(Guid.NewGuid(), start, end, "Approved Absence", 2, "Other", "Approved", DateTime.UtcNow, null, null, null, null)
        };

        _mockAbsenceService.Setup(x => x.GetAbsenceRequestsAsync(start, end, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(absences);

        // Mock current user as regular employee
        _mockClaimsProvider.Setup(x => x.GetRoles()).Returns(new List<string> { "Employee" });
        _mockUserSync.Setup(x => x.EnsureCurrentUserExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = currentUserId, Name = "Test User", Email = "test@example.com" });

        // Act
        var result = await controller.GetAbsenceRequests(start, end, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAbsences = Assert.IsAssignableFrom<IEnumerable<AbsenceRequestDto>>(okResult.Value);

        // Should return own absence + approved absence (2 total), filtering out other's pending absence
        Assert.Equal(2, returnedAbsences.Count());
        Assert.Contains(returnedAbsences, a => a.Reason == "My Absence");
        Assert.Contains(returnedAbsences, a => a.Reason == "Approved Absence");
        Assert.DoesNotContain(returnedAbsences, a => a.Reason == "Other Absence");
    }

    [Fact]
    public async Task GetAbsenceRequests_WithDateRange_ManagerSeesAll()
    {
        // Arrange
        var controller = CreateController();
        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow.AddDays(7);

        var absences = new List<AbsenceRequestDto>
        {
            new AbsenceRequestDto(Guid.NewGuid(), start, end, "My Absence", 1, "Me", "Pending", DateTime.UtcNow, null, null, null, null),
            new AbsenceRequestDto(Guid.NewGuid(), start, end, "Other Absence", 2, "Other", "Pending", DateTime.UtcNow, null, null, null, null)
        };

        _mockAbsenceService.Setup(x => x.GetAbsenceRequestsAsync(start, end, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(absences);

        // Mock current user as Manager
        _mockClaimsProvider.Setup(x => x.GetRoles()).Returns(new List<string> { "Manager" });

        // Act
        var result = await controller.GetAbsenceRequests(start, end, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAbsences = Assert.IsAssignableFrom<IEnumerable<AbsenceRequestDto>>(okResult.Value);

        // Manager sees all
        Assert.Equal(2, returnedAbsences.Count());
    }

    [Fact]
    public async Task GetAbsenceRequests_MissingParameters_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetAbsenceRequests(null, null, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Either provide start and end dates, or provide employeeId", badRequestResult.Value);
    }
}
