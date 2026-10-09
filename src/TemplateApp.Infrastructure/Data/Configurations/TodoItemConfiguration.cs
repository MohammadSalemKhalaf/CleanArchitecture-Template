using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Infrastructure.Data.Configurations;

internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");

        builder.HasKey(todo => todo.Id);
        builder.Property(todo => todo.Id).ValueGeneratedNever();

        builder.Property(todo => todo.Title)
            .HasMaxLength(TodoItem.TitleMaxLength)
            .IsRequired();

        builder.HasIndex(todo => todo.Title).IsUnique();

        builder.Property(todo => todo.Description)
            .HasMaxLength(TodoItem.DescriptionMaxLength);

        builder.Property(todo => todo.CreatedBy).HasMaxLength(128);
        builder.Property(todo => todo.LastModifiedBy).HasMaxLength(128);

        // Supports the default ordering of the paged list.
        builder.HasIndex(todo => todo.CreatedAtUtc);
    }
}
