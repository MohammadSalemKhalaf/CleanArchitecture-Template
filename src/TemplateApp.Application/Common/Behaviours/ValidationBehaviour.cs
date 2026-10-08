using FluentValidation;

using MediatR;

using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Common.Behaviours;

/// <summary>Runs every FluentValidation validator for the request and short-circuits with validation errors.</summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorResultFactory<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);

            errors.AddRange(result.Errors.Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage)));
        }

        return errors.Count == 0
            ? await next(cancellationToken)
            : TResponse.FromErrors(errors);
    }
}
