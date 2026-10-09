using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TemplateApp.Application.Common.Exceptions;
using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Queries.GetTodoItems;
using TemplateApp.Infrastructure.Data;
using TemplateApp.Infrastructure.IntegrationTests.Common;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Infrastructure.IntegrationTests.Data;

[Collection(SqlServerCollection.Name)]
public sealed class PersistenceTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Migrations_MatchTheCurrentModel()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());

        // Fails when an entity or configuration changed without "dotnet ef migrations add".
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task SaveChanges_StampsAuditColumnsOnInsertAndUpdate()
    {
        var created = fixture.Clock.Now = new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);
        fixture.User.Id = "creator";
        var todo = TodoItemFactory.CreateTodoItem();

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            db.TodoItems.Add(todo);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        var modified = fixture.Clock.Now = created.AddHours(3);
        fixture.User.Id = "editor";

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var loaded = await db.TodoItems.SingleAsync(item => item.Id == todo.Id);
            loaded.Update(loaded.Title, "edited");
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<IAppDbContext>()
                .TodoItems.AsNoTracking().SingleAsync(item => item.Id == todo.Id);

            Assert.Equal(created, stored.CreatedAtUtc);
            Assert.Equal("creator", stored.CreatedBy);
            Assert.Equal(modified, stored.LastModifiedAtUtc);
            Assert.Equal("editor", stored.LastModifiedBy);
        }
    }

    [Theory]
    [InlineData("Duplicate title {0}", "Duplicate title {0}")]
    [InlineData("Duplicate title {0}", "DUPLICATE TITLE {0}")] // Default SQL Server collation is case-insensitive.
    public async Task SaveChanges_TranslatesUniqueIndexViolations(string firstTitle, string secondTitle)
    {
        var suffix = Guid.NewGuid();

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            db.TodoItems.Add(TodoItemFactory.CreateTodoItem(string.Format(firstTitle, suffix)));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            db.TodoItems.Add(TodoItemFactory.CreateTodoItem(string.Format(secondTitle, suffix)));

            await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => db.SaveChangesAsync(CancellationToken.None));
        }
    }

    [Fact]
    public async Task ListQuery_OrdersNewestFirstWhenTranslatedToSql()
    {
        var prefix = $"Order {Guid.NewGuid():N}";
        List<Guid> idsInCreationOrder = [];

        for (var i = 0; i < 3; i++)
        {
            fixture.Clock.Now = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(i);

            await using var scope = fixture.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var todo = TodoItemFactory.CreateTodoItem($"{prefix} {i}");
            db.TodoItems.Add(todo);
            await db.SaveChangesAsync(CancellationToken.None);
            idsInCreationOrder.Add(todo.Id);
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var handler = new GetTodoItemsQueryHandler(
                scope.ServiceProvider.GetRequiredService<IAppDbContext>(),
                scope.ServiceProvider.GetRequiredService<ICacheService>());

            var page = (await handler.Handle(new GetTodoItemsQuery(1, 100), CancellationToken.None)).Value;

            var returnedIds = page.Items.Where(item => item.Title.StartsWith(prefix, StringComparison.Ordinal)).Select(item => item.TodoItemId);
            Assert.Equal(Enumerable.Reverse(idsInCreationOrder), returnedIds);
        }
    }
}
