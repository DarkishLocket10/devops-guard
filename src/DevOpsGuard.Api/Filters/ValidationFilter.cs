using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsGuard.Api.Filters;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Find the first argument of type T (our body DTO)
        var arg = context.Arguments.FirstOrDefault(a => a is T) as T;
        if (arg is null) return await next(context);

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null) return await next(context);

        var result = await validator.ValidateAsync(arg);
        if (result.IsValid) return await next(context);

        var errors = result.ToDictionary();
        return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");
    }
}
