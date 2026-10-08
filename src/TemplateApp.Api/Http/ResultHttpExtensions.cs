using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Api.Http;

/// <summary>The single place where application errors become HTTP responses.</summary>
public static class ResultHttpExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
        => result.Match(onSuccess, ToProblem);

    public static IResult ToProblem(this IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("Cannot build a problem response without errors.", nameof(errors));
        }

        if (errors.All(error => error.Kind == ErrorKind.Validation))
        {
            // Several rules can fail for the same field, so group instead of assuming one error per code.
            var fieldErrors = errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

            return TypedResults.ValidationProblem(fieldErrors);
        }

        // Mixed lists only happen when a handler concatenates results; the first non-validation error decides the status.
        var error = errors.First(e => e.Kind != ErrorKind.Validation);

        return TypedResults.Problem(
            statusCode: StatusCodeFor(error.Kind),
            detail: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    public static int StatusCodeFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Failure => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };
}
