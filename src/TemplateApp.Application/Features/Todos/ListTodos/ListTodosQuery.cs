using TemplateApp.Application.Common.Messaging;
using TemplateApp.Application.Common.Models;

namespace TemplateApp.Application.Features.Todos.ListTodos;

public sealed record ListTodosQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<TodoItemResponse>>;
