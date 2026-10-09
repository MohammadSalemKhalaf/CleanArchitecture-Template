using MediatR;

using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Common.Interfaces;

/// <summary>A request that changes state. Every command returns a <see cref="Result{TValue}"/>.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
