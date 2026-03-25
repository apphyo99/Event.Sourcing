namespace EventSourcing.BuildingBlocks.Application.Common;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates if the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Collection of validation errors
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    protected Result(bool isSuccess, string? error, IEnumerable<string>? errors = null)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Successful result cannot have an error");

        if (!isSuccess && string.IsNullOrEmpty(error) && (errors == null || !errors.Any()))
            throw new InvalidOperationException("Failed result must have an error");

        IsSuccess = isSuccess;
        Error = error;
        Errors = errors?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success()
    {
        return new Result(true, null);
    }

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    public static Result Failure(string error)
    {
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed result with multiple errors
    /// </summary>
    public static Result Failure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new Result(false, errorList.FirstOrDefault(), errorList);
    }

    /// <summary>
    /// Implicit conversion from string to failed result
    /// </summary>
    public static implicit operator Result(string error)
    {
        return Failure(error);
    }
}

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
/// <typeparam name="T">The type of the value</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// The value if the operation was successful
    /// </summary>
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string? error, IEnumerable<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new Result<T>(value, true, null);
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public new static Result<T> Failure(string error)
    {
        return new Result<T>(default, false, error);
    }

    /// <summary>
    /// Creates a failed result with multiple errors
    /// </summary>
    public new static Result<T> Failure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new Result<T>(default, false, errorList.FirstOrDefault(), errorList);
    }

    /// <summary>
    /// Implicit conversion from T to successful result
    /// </summary>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    /// <summary>
    /// Implicit conversion from string to failed result
    /// </summary>
    public static implicit operator Result<T>(string error)
    {
        return Failure(error);
    }
}

/// <summary>
/// Paged result for queries that return multiple items
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public IReadOnlyCollection<T> Items { get; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResult<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
    }
}
