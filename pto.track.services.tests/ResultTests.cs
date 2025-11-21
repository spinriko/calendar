using pto.track.services;
using Xunit;

namespace pto.track.services.tests;

public class ResultTests
{
    [Fact]
    public void SuccessResult_WithoutData_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.SuccessResult();

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void SuccessResult_WithData_CreatesSuccessfulResultWithData()
    {
        // Act
        var result = Result<string>.SuccessResult("test data");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("test data", result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void Failure_WithErrorMessage_CreatesFailedResult()
    {
        // Act
        var result = Result.Failure("Something went wrong");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Something went wrong", result.ErrorMessage);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void Failure_WithData_CreatesFailedResultWithErrorMessage()
    {
        // Act
        var result = Result<string>.Failure("Error occurred");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Error occurred", result.ErrorMessage);
        Assert.Null(result.Data);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void ValidationFailure_WithSingleError_CreatesValidationFailedResult()
    {
        // Act
        var errors = new List<string> { "Field is required" };
        var result = Result.ValidationFailure(errors);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Validation failed", result.ErrorMessage);
        Assert.Single(result.ValidationErrors);
        Assert.Equal("Field is required", result.ValidationErrors[0]);
    }

    [Fact]
    public void ValidationFailure_WithMultipleErrors_CreatesValidationFailedResult()
    {
        // Act
        var errors = new List<string>
        {
            "Start date is required",
            "End date must be after start date",
            "Reason must be at least 3 characters"
        };
        var result = Result.ValidationFailure(errors);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Validation failed", result.ErrorMessage);
        Assert.Equal(3, result.ValidationErrors.Count);
        Assert.Contains("Start date is required", result.ValidationErrors);
        Assert.Contains("End date must be after start date", result.ValidationErrors);
        Assert.Contains("Reason must be at least 3 characters", result.ValidationErrors);
    }
    [Fact]
    public void ValidationFailure_WithData_CreatesValidationFailedResult()
    {
        // Act
        var errors = new List<string> { "Invalid data" };
        var result = Result<string>.ValidationFailure(errors);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Validation failed", result.ErrorMessage);
        Assert.Null(result.Data);
        Assert.Single(result.ValidationErrors);
        Assert.Equal("Invalid data", result.ValidationErrors[0]);
    }
}
