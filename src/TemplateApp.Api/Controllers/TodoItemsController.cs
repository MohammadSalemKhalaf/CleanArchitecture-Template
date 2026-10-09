using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TemplateApp.Application.Common.Models;
using TemplateApp.Application.Features.TodoItems.Commands.CreateTodoItem;
using TemplateApp.Application.Features.TodoItems.Commands.DeleteTodoItem;
using TemplateApp.Application.Features.TodoItems.Commands.UpdateTodoItem;
using TemplateApp.Application.Features.TodoItems.Dtos;
using TemplateApp.Application.Features.TodoItems.Queries.GetTodoItemById;
using TemplateApp.Application.Features.TodoItems.Queries.GetTodoItems;
using TemplateApp.Contracts.Common;
using TemplateApp.Contracts.Requests.TodoItems;

namespace TemplateApp.Api.Controllers;

/// <summary>Sample controller: one action per use case, request mapped to a command or query, result mapped by <see cref="ApiController"/>.</summary>
[Route("api/v{version:apiVersion}/todo-items")]
[ApiVersion("1.0")]
[Authorize]
public sealed class TodoItemsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TodoItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Retrieves a page of todo items.")]
    [EndpointDescription("Returns todo items, newest first.")]
    [EndpointName("GetTodoItems")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Get([FromQuery] PageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetTodoItemsQuery(request.Page, request.PageSize), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{todoItemId:guid}", Name = "GetTodoItemById")]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a todo item by ID.")]
    [EndpointDescription("Returns the todo item if found.")]
    [EndpointName("GetTodoItemById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetById(Guid todoItemId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTodoItemByIdQuery(todoItemId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Creates a todo item.")]
    [EndpointDescription("Adds a todo item. Titles are unique.")]
    [EndpointName("CreateTodoItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Create([FromBody] CreateTodoItemRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateTodoItemCommand(request.Title, request.Description), ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetTodoItemById",
                routeValues: new { version = "1.0", todoItemId = response.TodoItemId },
                value: response),
            Problem);
    }

    [HttpPut("{todoItemId:guid}")]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Updates a todo item.")]
    [EndpointDescription("Replaces the title and description and completes or reopens the item.")]
    [EndpointName("UpdateTodoItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Update(Guid todoItemId, [FromBody] UpdateTodoItemRequest request, CancellationToken ct)
    {
        var command = new UpdateTodoItemCommand(todoItemId, request.Title, request.Description, request.IsCompleted);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{todoItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Deletes a todo item.")]
    [EndpointDescription("Removes the todo item.")]
    [EndpointName("DeleteTodoItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Delete(Guid todoItemId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteTodoItemCommand(todoItemId), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
