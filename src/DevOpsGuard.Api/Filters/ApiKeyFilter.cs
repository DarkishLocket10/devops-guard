using Microsoft.AspNetCore.Http;

namespace DevOpsGuard.Api.Filters;

public sealed class ApiKeyFilter : IEndpointFilter
{
    private readonly string _expected;

    public ApiKeyFilter(string expectedApiKey)
    {
        _expected = expectedApiKey ?? string.Empty;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            // If no expected key is configured, allow all (useful in tests)
            if (string.IsNullOrWhiteSpace(_expected))
                return await next(context);

            var headers = context.HttpContext.Request?.Headers;
            if (headers is null || !headers.TryGetValue("X-API-Key", out var providedValues))
            {
                return Results.Problem(title: "Unauthorized",
                    detail: "Missing or invalid X-API-Key header.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var provided = providedValues.ToString(); // never throws
            if (string.IsNullOrWhiteSpace(provided) || !string.Equals(provided, _expected, StringComparison.Ordinal))
            {
                return Results.Problem(title: "Unauthorized",
                    detail: "Missing or invalid X-API-Key header.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return await next(context);
        }
        catch
        {
            // Never leak exceptions from the filter; return a clean 401
            return Results.Problem(title: "Unauthorized",
                detail: "Missing or invalid X-API-Key header.",
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }
}
