using MediatR;

using TemplateApp.Api.Http;
using TemplateApp.Application.Common.Models;
using TemplateApp.Application.Features.Todos;
using TemplateApp.Application.Features.Todos.CreateTodo;
using TemplateApp.Application.Features.Todos.DeleteTodo;
using TemplateApp.Application.Features.Todos.GetTodoById;
using TemplateApp.Application.Features.Todos.ListTodos;
using TemplateApp.Application.Features.Todos.UpdateTodo;

namespace TemplateApp.Api.Endpoints.Todos;

/// <summary>HTTP transport for the Todos feature. Each endpoint maps a request to one use case and its result to HTTP.</summary>
public static class TodoEndpoints
{
    private const string GetByIdRouteName = "GetTodoById";

    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("/api/v1/todos")
            .WithTags("Todos")
            .RequireAuthorization();

        todos.MapGet("/", ListAsync)
            .WithName("ListTodos")
            .Produces<PagedResult<TodoItemResponse>>()
            .ProducesValidationProblem();

        todos.MapGet("/{id:guid}", GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<TodoItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        todos.MapPost("/", CreateAsync)
            .WithName("CreateTodo")
            .Produces<TodoItemResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        todos.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTodo")
            .Produces<TodoItemResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        todos.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodo")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(ISender sender, CancellationToken cancellationToken, int page = 1, int pageSize = 20)
    {
        var result = await sender.Send(new ListTodosQuery(page, pageSize), cancellationToken);
        return result.ToHttpResult(todos => TypedResults.Ok(todos));
    }

    private static async Task<IResult> GetByIdAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodoByIdQuery(id), cancellationToken);
        return result.ToHttpResult(todo => TypedResults.Ok(todo));
    }

    private static async Task<IResult> CreateAsync(CreateTodoRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTodoCommand(request.Title, request.Description), cancellationToken);
        return result.ToHttpResult(todo => TypedResults.CreatedAtRoute(todo, GetByIdRouteName, new { id = todo.Id }));
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateTodoRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateTodoCommand(id, request.Title, request.Description, request.IsCompleted),
            cancellationToken);
        return result.ToHttpResult(todo => TypedResults.Ok(todo));
    }

    private static async Task<IResult> DeleteAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteTodoCommand(id), cancellationToken);
        return result.ToHttpResult(_ => TypedResults.NoContent());
    }
}
