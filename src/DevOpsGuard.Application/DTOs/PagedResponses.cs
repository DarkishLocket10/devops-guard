using System.Collections.Generic;

namespace DevOpsGuard.Application.DTOs;

public sealed record WorkItemListResponse(
    int Page,
    int PageSize,
    int Total,
    List<WorkItemResponse> Items);
