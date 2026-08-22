using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProbMind.Application.Validation;

namespace ProbMind.Api.Filters;

public sealed class RequestValidationFilter : IAsyncActionFilter
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<IRequestValidator>> _validators;

    public RequestValidationFilter(IEnumerable<IRequestValidator> validators)
    {
        _validators = validators
            .GroupBy(x => x.RequestType)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<IRequestValidator>)group.ToArray());
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var issues = new List<ValidationIssue>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            if (!_validators.TryGetValue(argument.GetType(), out var matching))
                continue;

            foreach (var validator in matching)
                issues.AddRange(validator.ValidateObject(argument));
        }

        if (issues.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "validation_error",
                message = "Request validation failed.",
                issues
            });
            return;
        }

        await next();
    }
}
