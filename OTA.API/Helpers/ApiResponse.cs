namespace OTA.API.Helpers
{
    /// <summary>
    /// Standardised API response envelope used across all endpoints.
    /// Wraps data, error information, and optional pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of the data payload.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>Indicates whether the operation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Human-readable message describing the result.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>The response payload. Null on failure responses.</summary>
        public T? Data { get; set; }

        /// <summary>List of validation or domain error messages. Empty on success.</summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>Pagination metadata included when the endpoint returns paged results.</summary>
        public PaginationInfo? Pagination { get; set; }

        // ── Factory Methods ───────────────────────────────────────────────────

        /// <summary>Creates a successful response wrapping the provided data.</summary>
        public static ApiResponse<T> Ok(T data)
            => new() { Success = true, Message = "Success", Data = data };

        /// <summary>Creates a successful response with a custom message.</summary>
        public static ApiResponse<T> Ok(T data, string message)
            => new() { Success = true, Message = message, Data = data };

        /// <summary>Creates a successful paged response.</summary>
        public static ApiResponse<T> Ok(T data, string message, PaginationInfo pagination)
            => new() { Success = true, Message = message, Data = data, Pagination = pagination };

        /// <summary>Creates a failed response with an error message.</summary>
        public static ApiResponse<T> Fail(string message)
            => new() { Success = false, Message = message };

        /// <summary>Creates a failed response with an error message and a list of detailed errors.</summary>
        public static ApiResponse<T> Fail(string message, List<string> errors)
            => new() { Success = false, Message = message, Errors = errors };
    }

    /// <summary>
    /// Non-generic convenience variant for endpoints that return no data payload.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse OkNoData(string message = "Success")
            => new() { Success = true, Message = message };

        public static new ApiResponse Fail(string message)
            => new() { Success = false, Message = message };

        public static new ApiResponse Fail(string message, List<string> errors)
            => new() { Success = false, Message = message, Errors = errors };
    }

    /// <summary>
    /// Pagination metadata returned alongside paged data collections.
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>Current one-based page number.</summary>
        public int Page { get; set; }

        /// <summary>Maximum number of items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of items matching the query across all pages.</summary>
        public long TotalCount { get; set; }

        /// <summary>Total number of pages calculated from TotalCount and PageSize.</summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>Whether there is a previous page.</summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>Whether there is a next page.</summary>
        public bool HasNextPage => Page < TotalPages;

        public static PaginationInfo Create(int page, int pageSize, long totalCount)
            => new() { Page = page, PageSize = pageSize, TotalCount = totalCount };
    }
}
