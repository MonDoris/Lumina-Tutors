namespace LuminaTutors.Domain.Common;

/// <summary>
/// Functional result pattern — eliminates exception-driven control flow.
/// Use in Service layer to communicate success/failure + data to Controllers.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }
    public List<string> ValidationErrors { get; private set; } = [];

    private Result() { }

    public static Result<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    public static Result<T> Failure(string error, string? errorCode = null) =>
        new() { IsSuccess = false, Error = error, ErrorCode = errorCode };

    public static Result<T> ValidationFailure(List<string> errors) =>
        new() { IsSuccess = false, Error = "Validation failed", ErrorCode = "VALIDATION_ERROR", ValidationErrors = errors };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }
    public List<string> ValidationErrors { get; private set; } = [];

    private Result() { }

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(string error, string? errorCode = null) =>
        new() { IsSuccess = false, Error = error, ErrorCode = errorCode };

    public static Result ValidationFailure(List<string> errors) =>
        new() { IsSuccess = false, Error = "Validation failed", ErrorCode = "VALIDATION_ERROR", ValidationErrors = errors };
}

/// <summary>
/// Paginated result for list queries.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PagedResult<T> Create(List<T> items, int totalCount, int pageNumber, int pageSize) =>
        new() { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
}
