using MediatR;

using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Common.Interfaces;

/// <summary>A request that reads state and must not change it.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
