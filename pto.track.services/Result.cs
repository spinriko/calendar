namespace pto.track.services;

/// <summary>
/// Represents the result of an operation, containing either success with data or failure with error information.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the data returned from the operation if successful.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the collection of validation errors if the operation failed due to validation.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    private Result(bool success, T? data, string? errorMessage, IReadOnlyList<string>? validationErrors)
    {
        Success = success;
        Data = data;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Creates a successful result with the provided data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> SuccessResult(T data)
    {
        return new Result<T>(true, data, null, null);
    }

    /// <summary>
    /// Creates a failed result with the provided error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string errorMessage)
    {
        return new Result<T>(false, default, errorMessage, null);
    }

    /// <summary>
    /// Creates a failed result with validation errors.
    /// </summary>
    /// <param name="validationErrors">The collection of validation error messages.</param>
    /// <returns>A failed result with validation errors.</returns>
    public static Result<T> ValidationFailure(IReadOnlyList<string> validationErrors)
    {
        return new Result<T>(false, default, "Validation failed", validationErrors);
    }

    /// <summary>
    /// Creates a failed result with a single validation error.
    /// </summary>
    /// <param name="validationError">The validation error message.</param>
    /// <returns>A failed result with a validation error.</returns>
    public static Result<T> ValidationFailure(string validationError)
    {
        return new Result<T>(false, default, "Validation failed", new[] { validationError });
    }
}

/// <summary>
/// Represents the result of an operation that doesn't return data.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the collection of validation errors if the operation failed due to validation.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    private Result(bool success, string? errorMessage, IReadOnlyList<string>? validationErrors)
    {
        Success = success;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result SuccessResult()
    {
        return new Result(true, null, null);
    }

    /// <summary>
    /// Creates a failed result with the provided error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string errorMessage)
    {
        return new Result(false, errorMessage, null);
    }

    /// <summary>
    /// Creates a failed result with validation errors.
    /// </summary>
    /// <param name="validationErrors">The collection of validation error messages.</param>
    /// <returns>A failed result with validation errors.</returns>
    public static Result ValidationFailure(IReadOnlyList<string> validationErrors)
    {
        return new Result(false, "Validation failed", validationErrors);
    }

    /// <summary>
    /// Creates a failed result with a single validation error.
    /// </summary>
    /// <param name="validationError">The validation error message.</param>
    /// <returns>A failed result with a validation error.</returns>
    public static Result ValidationFailure(string validationError)
    {
        return new Result(false, "Validation failed", new[] { validationError });
    }
}
