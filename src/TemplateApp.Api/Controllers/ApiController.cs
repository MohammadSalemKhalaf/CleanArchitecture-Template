using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Api.Controllers;

/// <summary>
/// Base class for every controller. <see cref="Problem(IReadOnlyList{Error})"/> is the single place where application
/// errors become HTTP responses (RFC 9457 problem details).
/// </summary>
[ApiController]
public abstract class ApiController : ControllerBase
{
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

    protected ActionResult Problem(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("Cannot build a problem response without errors.", nameof(errors));
        }

        if (errors.All(error => error.Kind == ErrorKind.Validation))
        {
            return ValidationProblem(errors);
        }

        // Mixed lists only happen when a handler concatenates results; the first non-validation error decides the status.
        var error = errors.First(e => e.Kind != ErrorKind.Validation);
        var statusCode = StatusCodeFor(error.Kind);

        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode, detail: error.Description);
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    private ActionResult ValidationProblem(IReadOnlyList<Error> errors)
    {
        // ModelStateDictionary accepts several messages per field, so repeated codes are grouped, not rejected.
        var modelState = new ModelStateDictionary();

        foreach (var error in errors)
        {
            modelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(modelState);
    }
}
