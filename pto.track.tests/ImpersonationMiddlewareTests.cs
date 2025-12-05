using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using pto.track.Middleware;
using pto.track.Models;
using System.Text.Json;

namespace pto.track.tests;

public class ImpersonationMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<MockAuthenticationMiddleware>> _loggerMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public ImpersonationMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<MockAuthenticationMiddleware>>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(_authServiceMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WhenNotMockMode_DoesNotAuthenticate()
    {
        // Arrange
        _configMock.Setup(c => c["Authentication:Mode"]).Returns("AzureAd");
        var middleware = new MockAuthenticationMiddleware(_nextMock.Object, _configMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Null(context.User.Identity?.Name);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenMockMode_AndNoImpersonationCookie_AuthenticatesDefaultUser()
    {
        // Arrange
        _configMock.Setup(c => c["Authentication:Mode"]).Returns("Mock");
        var middleware = new MockAuthenticationMiddleware(_nextMock.Object, _configMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext { RequestServices = _serviceProviderMock.Object };

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("EMP001", context.User.FindFirst("employeeNumber")?.Value);
        Assert.Equal("Test Employee 1", context.User.Identity?.Name);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenMockMode_AndImpersonationCookieExists_AuthenticatesImpersonatedUser()
    {
        // Arrange
        _configMock.Setup(c => c["Authentication:Mode"]).Returns("Mock");
        var middleware = new MockAuthenticationMiddleware(_nextMock.Object, _configMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext { RequestServices = _serviceProviderMock.Object };

        var impersonationData = new ImpersonationData
        {
            EmployeeNumber = "MGR001",
            Roles = new List<string> { "Employee", "Manager" }
        };
        var cookieValue = JsonSerializer.Serialize(impersonationData);

        context.Request.Headers.Append("Cookie", $"ImpersonationData={Uri.EscapeDataString(cookieValue)}");
        // Note: DefaultHttpContext doesn't parse cookies from headers automatically in unit tests easily,
        // so we might need to mock the cookie collection or just rely on the middleware reading it.
        // Looking at the middleware code: context.Request.Cookies["ImpersonationData"]
        // We need to set the cookies collection.

        var cookieCollection = new Mock<IRequestCookieCollection>();
        cookieCollection.Setup(c => c["ImpersonationData"]).Returns(cookieValue);
        context.Request.Cookies = cookieCollection.Object;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("MGR001", context.User.FindFirst("employeeNumber")?.Value);
        Assert.Equal("Test Manager", context.User.Identity?.Name);
        Assert.Contains(context.User.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Manager");
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenImpersonatingAdmin_HasAdminRole()
    {
        // Arrange
        _configMock.Setup(c => c["Authentication:Mode"]).Returns("Mock");
        var middleware = new MockAuthenticationMiddleware(_nextMock.Object, _configMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext { RequestServices = _serviceProviderMock.Object };

        var impersonationData = new ImpersonationData
        {
            EmployeeNumber = "ADMIN001",
            Roles = new List<string> { "Employee", "Admin" }
        };
        var cookieValue = JsonSerializer.Serialize(impersonationData);

        var cookieCollection = new Mock<IRequestCookieCollection>();
        cookieCollection.Setup(c => c["ImpersonationData"]).Returns(cookieValue);
        context.Request.Cookies = cookieCollection.Object;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("ADMIN001", context.User.FindFirst("employeeNumber")?.Value);
        Assert.Equal("Administrator", context.User.Identity?.Name);
        Assert.Contains(context.User.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }
}
