using System.Collections.Generic;

namespace BuildingBlocks.Contracts.Common;

public sealed record ValidationIssueDto(
    string Field,
    IReadOnlyCollection<string> Errors);

public sealed record ApiErrorDto(
    string Code,
    string Message,
    string? TraceId = null,
    IReadOnlyCollection<ValidationIssueDto>? ValidationIssues = null,
    string? Details = null);

public sealed record PageRequestDto(
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    bool SortDescending = false);

public sealed record PagedResultDto<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);