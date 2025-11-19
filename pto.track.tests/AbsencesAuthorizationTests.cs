using Microsoft.AspNetCore.Http;
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

/// <summary>
/// Tests for authorization and role-based access control in AbsencesController
/// </summary>
public class AbsencesAuthorizationTests
{
    private readonly Mock<IAbsenceService> _mockAbsenceService;
    private readonly Mock<ILogger<AbsencesController>> _mockLogger;
    private readonly Mock<IUserClaimsProvider> _mockClaimsProvider;
    private readonly Mock<IUserSyncService> _mockUserSync;

    public AbsencesAuthorizationTests()
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

    #region Approve Authorization Tests

    [Fact]
    public async Task ApproveAbsenceRequest_WithManagerRole_Succeeds()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var approverId = 5;
        var dto = new ApproveAbsenceRequestDto(approverId, "Approved");

        _mockClaimsProvider.Setup(x => x.IsInRole("Manager")).Returns(true);
        _mockClaimsProvider.Setup(x => x.IsInRole("Approver")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Admin")).Returns(false);
        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(approverId);
        _mockAbsenceService.Setup(x => x.ApproveAbsenceRequestAsync(absenceId, dto)).ReturnsAsync(true);

        // Act
        var result = await controller.ApproveAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockAbsenceService.Verify(x => x.ApproveAbsenceRequestAsync(absenceId, dto), Times.Once);
    }

    [Fact]
    public async Task ApproveAbsenceRequest_WithApproverRole_Succeeds()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var approverId = 3;
        var dto = new ApproveAbsenceRequestDto(approverId, "Looks good");

        _mockClaimsProvider.Setup(x => x.IsInRole("Manager")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Approver")).Returns(true);
        _mockClaimsProvider.Setup(x => x.IsInRole("Admin")).Returns(false);
        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(approverId);
        _mockAbsenceService.Setup(x => x.ApproveAbsenceRequestAsync(absenceId, dto)).ReturnsAsync(true);

        // Act
        var result = await controller.ApproveAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ApproveAbsenceRequest_WithAdminRole_Succeeds()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var approverId = 1;
        var dto = new ApproveAbsenceRequestDto(approverId, null);

        _mockClaimsProvider.Setup(x => x.IsInRole("Manager")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Approver")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Admin")).Returns(true);
        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(approverId);
        _mockAbsenceService.Setup(x => x.ApproveAbsenceRequestAsync(absenceId, dto)).ReturnsAsync(true);

        // Act
        var result = await controller.ApproveAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ApproveAbsenceRequest_WithEmployeeRole_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var dto = new ApproveAbsenceRequestDto(10, null);

        _mockClaimsProvider.Setup(x => x.IsInRole("Manager")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Approver")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Admin")).Returns(false);

        // Act
        var result = await controller.ApproveAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAbsenceService.Verify(x => x.ApproveAbsenceRequestAsync(It.IsAny<Guid>(), It.IsAny<ApproveAbsenceRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAbsenceRequest_WithMismatchedApproverId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var currentUserId = 5;
        var providedApproverId = 10;
        var dto = new ApproveAbsenceRequestDto(providedApproverId, null);

        _mockClaimsProvider.Setup(x => x.IsInRole("Manager")).Returns(true);
        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(currentUserId);

        // Act
        var result = await controller.ApproveAbsenceRequest(absenceId, dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Approver ID must match the authenticated user", badRequestResult.Value);
        _mockAbsenceService.Verify(x => x.ApproveAbsenceRequestAsync(It.IsAny<Guid>(), It.IsAny<ApproveAbsenceRequestDto>()), Times.Never);
    }

    #endregion

    #region Reject Authorization Tests

    [Fact]
    public async Task RejectAbsenceRequest_WithManagerRole_Succeeds()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var approverId = 5;
        var dto = new RejectAbsenceRequestDto(approverId, "Not enough coverage");

        _mockClaimsProvider.Setup(x => x.IsInRole("Manager")).Returns(true);
        _mockClaimsProvider.Setup(x => x.IsInRole("Approver")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Admin")).Returns(false);
        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(approverId);
        _mockAbsenceService.Setup(x => x.RejectAbsenceRequestAsync(absenceId, dto)).ReturnsAsync(true);

        // Act
        var result = await controller.RejectAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockAbsenceService.Verify(x => x.RejectAbsenceRequestAsync(absenceId, dto), Times.Once);
    }

    [Fact]
    public async Task RejectAbsenceRequest_WithEmployeeRole_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var dto = new RejectAbsenceRequestDto(10, "Test");

        _mockClaimsProvider.Setup(x => x.IsInRole("Manager")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Approver")).Returns(false);
        _mockClaimsProvider.Setup(x => x.IsInRole("Admin")).Returns(false);

        // Act
        var result = await controller.RejectAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAbsenceService.Verify(x => x.RejectAbsenceRequestAsync(It.IsAny<Guid>(), It.IsAny<RejectAbsenceRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task RejectAbsenceRequest_WithMismatchedApproverId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var currentUserId = 3;
        var providedApproverId = 7;
        var dto = new RejectAbsenceRequestDto(providedApproverId, "No");

        _mockClaimsProvider.Setup(x => x.IsInRole("Approver")).Returns(true);
        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(currentUserId);

        // Act
        var result = await controller.RejectAbsenceRequest(absenceId, dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Approver ID must match the authenticated user", badRequestResult.Value);
    }

    #endregion

    #region Update Authorization Tests

    [Fact]
    public async Task PutAbsenceRequest_UserUpdatesOwnRequest_Succeeds()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var employeeId = 8;
        var startDate = DateTime.UtcNow.AddDays(5);
        var dto = new UpdateAbsenceRequestDto(startDate, startDate.AddDays(2), "Updated reason");

        var existingRequest = new AbsenceRequestDto(
            absenceId,
            startDate,
            startDate.AddDays(1),
            "Original reason",
            employeeId,
            "Test Employee",
            "Pending",
            DateTime.UtcNow,
            null,
            null,
            null,
            null
        );

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(employeeId);
        _mockAbsenceService.Setup(x => x.GetAbsenceRequestByIdAsync(absenceId)).ReturnsAsync(existingRequest);
        _mockAbsenceService.Setup(x => x.UpdateAbsenceRequestAsync(absenceId, dto)).ReturnsAsync(true);

        // Act
        var result = await controller.PutAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockAbsenceService.Verify(x => x.UpdateAbsenceRequestAsync(absenceId, dto), Times.Once);
    }

    [Fact]
    public async Task PutAbsenceRequest_UserUpdatesOthersRequest_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var currentUserId = 5;
        var requestOwnerId = 10;
        var startDate = DateTime.UtcNow.AddDays(5);
        var dto = new UpdateAbsenceRequestDto(startDate, startDate.AddDays(2), "Trying to update someone else's request");

        var existingRequest = new AbsenceRequestDto(
            absenceId,
            startDate,
            startDate.AddDays(1),
            "Original",
            requestOwnerId,
            "Other Employee",
            "Pending",
            DateTime.UtcNow,
            null,
            null,
            null,
            null
        );

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(currentUserId);
        _mockAbsenceService.Setup(x => x.GetAbsenceRequestByIdAsync(absenceId)).ReturnsAsync(existingRequest);

        // Act
        var result = await controller.PutAbsenceRequest(absenceId, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAbsenceService.Verify(x => x.UpdateAbsenceRequestAsync(It.IsAny<Guid>(), It.IsAny<UpdateAbsenceRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task PutAbsenceRequest_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(5);
        var dto = new UpdateAbsenceRequestDto(startDate, startDate.AddDays(2), "Test");

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync((int?)null);

        // Act
        var result = await controller.PutAbsenceRequest(absenceId, dto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User is not authenticated", unauthorizedResult.Value);
    }

    [Fact]
    public async Task PutAbsenceRequest_NonexistentRequest_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(5);
        var dto = new UpdateAbsenceRequestDto(startDate, startDate.AddDays(2), "Test");

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(5);
        _mockAbsenceService.Setup(x => x.GetAbsenceRequestByIdAsync(absenceId)).ReturnsAsync((AbsenceRequestDto?)null);

        // Act
        var result = await controller.PutAbsenceRequest(absenceId, dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Absence request not found", notFoundResult.Value);
    }

    #endregion

    #region Cancel Authorization Tests

    [Fact]
    public async Task CancelAbsenceRequest_UserCancelsOwnRequest_Succeeds()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var employeeId = 12;

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(employeeId);
        _mockAbsenceService.Setup(x => x.CancelAbsenceRequestAsync(absenceId, employeeId)).ReturnsAsync(true);

        // Act
        var result = await controller.CancelAbsenceRequest(absenceId, employeeId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockAbsenceService.Verify(x => x.CancelAbsenceRequestAsync(absenceId, employeeId), Times.Once);
    }

    [Fact]
    public async Task CancelAbsenceRequest_UserCancelsOthersRequest_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var currentUserId = 5;
        var targetEmployeeId = 15;

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(currentUserId);

        // Act
        var result = await controller.CancelAbsenceRequest(absenceId, targetEmployeeId);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAbsenceService.Verify(x => x.CancelAbsenceRequestAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelAbsenceRequest_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var employeeId = 10;

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync((int?)null);

        // Act
        var result = await controller.CancelAbsenceRequest(absenceId, employeeId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User is not authenticated", unauthorizedResult.Value);
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public async Task ApproveAbsenceRequest_WithoutRole_LogsWarning()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var dto = new ApproveAbsenceRequestDto(1, null);

        _mockClaimsProvider.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);

        // Act
        await controller.ApproveAbsenceRequest(absenceId, dto);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unauthorized approval attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PutAbsenceRequest_WrongUser_LogsWarning()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var currentUserId = 5;
        var requestOwnerId = 10;
        var startDate = DateTime.UtcNow;
        var dto = new UpdateAbsenceRequestDto(startDate, startDate.AddDays(1), "Test");

        var existingRequest = new AbsenceRequestDto(
            absenceId,
            startDate,
            startDate.AddDays(1),
            "Test",
            requestOwnerId,
            "Other Employee",
            "Pending",
            DateTime.UtcNow,
            null,
            null,
            null,
            null
        );

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(currentUserId);
        _mockAbsenceService.Setup(x => x.GetAbsenceRequestByIdAsync(absenceId)).ReturnsAsync(existingRequest);

        // Act
        await controller.PutAbsenceRequest(absenceId, dto);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to update absence")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelAbsenceRequest_WrongUser_LogsWarning()
    {
        // Arrange
        var controller = CreateController();
        var absenceId = Guid.NewGuid();
        var currentUserId = 3;
        var targetEmployeeId = 8;

        _mockUserSync.Setup(x => x.GetCurrentUserResourceIdAsync()).ReturnsAsync(currentUserId);

        // Act
        await controller.CancelAbsenceRequest(absenceId, targetEmployeeId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to cancel request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
