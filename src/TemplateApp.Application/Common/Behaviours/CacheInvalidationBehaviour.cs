using MediatR;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Domain.Common.Results.Abstractions;

namespace TemplateApp.Application.Common.Behaviours;

/// <summary>Invalidates the tags declared by an <see cref="IInvalidatesCache"/> command once it has succeeded.</summary>
public sealed class CacheInvalidationBehaviour<TRequest, TResponse>(ICacheService cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IOperationResult
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (request is IInvalidatesCache invalidating && response.IsSuccess)
        {
            foreach (var tag in invalidating.CacheTags)
            {
                // The write is already committed. Do not let a cancelled request leave stale entries behind.
                await cache.RemoveByTagAsync(tag, CancellationToken.None);
            }
        }

        return response;
    }
}
