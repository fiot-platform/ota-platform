namespace OTA.API.Models.DTOs
{
    /// <summary>
    /// Generic paginated result wrapper returned by all paged service methods.
    /// Encapsulates the items for the current page along with pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the page.</typeparam>
    public sealed class PagedResult<T>
    {
        /// <summary>Items returned for the current page.</summary>
        public List<T> Items { get; set; } = new();

        /// <summary>Current one-based page number.</summary>
        public int Page { get; set; }

        /// <summary>Maximum items per page as requested.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of items matching the query across all pages.</summary>
        public long TotalCount { get; set; }

        /// <summary>Total number of pages (ceiling division of TotalCount by PageSize).</summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>Whether a previous page exists.</summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>Whether a next page exists.</summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>Creates a new <see cref="PagedResult{T}"/> from a list of items and metadata.</summary>
        public static PagedResult<T> Create(List<T> items, int page, int pageSize, long totalCount)
            => new()
            {
                Items      = items,
                Page       = page,
                PageSize   = pageSize,
                TotalCount = totalCount
            };

        /// <summary>Creates an empty paged result for the requested page/size.</summary>
        public static PagedResult<T> Empty(int page, int pageSize)
            => new()
            {
                Items      = new List<T>(),
                Page       = page,
                PageSize   = pageSize,
                TotalCount = 0
            };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Short-name aliases consumed by service interfaces.
    // These are non-sealed wrappers so they can be used interchangeably.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Alias for <see cref="FirmwareDetailDto"/> used by IFirmwareService.</summary>
    public class FirmwareDto : FirmwareDetailDto { }

    /// <summary>Alias for <see cref="RolloutDetailDto"/> used by IOtaService.</summary>
    public class RolloutDto : RolloutDetailDto { }

    /// <summary>Alias for <see cref="DeviceDetailDto"/> used by IDeviceService.</summary>
    public class DeviceDto : DeviceDetailDto { }

    /// <summary>Alias for <see cref="ProjectDetailDto"/> used by IProjectService.</summary>
    public class ProjectDto : ProjectDetailDto { }

    /// <summary>Alias for <see cref="RepositoryDetailDto"/> used by IRepositoryService.</summary>
    public class RepositoryDto : RepositoryDetailDto { }
}
