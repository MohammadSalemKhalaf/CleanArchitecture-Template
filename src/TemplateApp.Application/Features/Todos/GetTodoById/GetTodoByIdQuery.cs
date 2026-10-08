using TemplateApp.Application.Common.Messaging;

namespace TemplateApp.Application.Features.Todos.GetTodoById;

public sealed record GetTodoByIdQuery(Guid Id) : IQuery<TodoItemResponse>;
