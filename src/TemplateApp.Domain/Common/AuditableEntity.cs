namespace TemplateApp.Domain.Common;

/// <summary>
/// Entities deriving from this type get their audit columns stamped by the persistence layer on save.
/// The setters are private so domain code cannot forge audit data.
/// </summary>
public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset LastModifiedAtUtc { get; private set; }

    public string? LastModifiedBy { get; private set; }
}
