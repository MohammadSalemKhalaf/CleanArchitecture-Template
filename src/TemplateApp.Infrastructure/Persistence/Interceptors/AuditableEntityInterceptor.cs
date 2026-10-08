using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TemplateApp.Application.Common.Identity;
using TemplateApp.Domain.Common;

namespace TemplateApp.Infrastructure.Persistence.Interceptors;

/// <summary>Stamps creation and modification metadata on every <see cref="AuditableEntity"/> being saved.</summary>
public sealed class AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAtUtc).CurrentValue = now;
                entry.Property(entity => entity.CreatedBy).CurrentValue = userId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(entity => entity.LastModifiedAtUtc).CurrentValue = now;
                entry.Property(entity => entity.LastModifiedBy).CurrentValue = userId;
            }
        }
    }
}
