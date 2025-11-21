using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using pto.track.Middleware;
using pto.track.services.Exceptions;
using System.Text.Json;
using Xunit;

namespace pto.track.tests;

public class GlobalExceptionHandlerTests
{
    private static ILogger<GlobalExceptionHandler> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<GlobalExceptionHandler>();
    }

    [Fact]
    public async Task TryHandleAsync_WithAbsenceNotFoundException_Returns404()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(CreateLogger());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new AbsenceNotFoundException(Guid.NewGuid());
        var exceptionFeature = new TestExceptionHandlerFeature(exception);

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task TryHandleAsync_WithEventNotFoundException_Returns404()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(CreateLogger());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new EventNotFoundException(Guid.NewGuid());

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithResourceNotFoundException_Returns404()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(CreateLogger());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new ResourceNotFoundException(999);

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithInvalidAbsenceOperationException_Returns400()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(CreateLogger());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidAbsenceOperationException("Cannot approve already approved request");

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAbsenceAccessException_Returns403()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(CreateLogger());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new UnauthorizedAbsenceAccessException("Not authorized");

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnknownException_Returns500()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(CreateLogger());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("Something went wrong");

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WritesValidProblemDetailsJson()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(CreateLogger());
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var absenceId = Guid.NewGuid();
        var exception = new AbsenceNotFoundException(absenceId);

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        responseBody.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(responseBody);
        var json = await reader.ReadToEndAsync();

        Assert.NotEmpty(json);

        var problemDetails = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(404, problemDetails.GetProperty("status").GetInt32());
        Assert.Contains(absenceId.ToString(), problemDetails.GetProperty("detail").GetString());
    }

    private class TestExceptionHandlerFeature : IExceptionHandlerFeature
    {
        public TestExceptionHandlerFeature(Exception error)
        {
            Error = error;
        }

        public Exception Error { get; }
        public string Path { get; set; } = string.Empty;
        public Endpoint? Endpoint { get; set; }
        public RouteValueDictionary? RouteValues { get; set; }
    }
}
