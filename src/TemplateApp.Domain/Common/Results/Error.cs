namespace TemplateApp.Domain.Common.Results;

/// <summary>
/// An expected failure. <see cref="Code"/> is stable and machine-readable; <see cref="Description"/> is safe to show to clients.
/// For validation errors, <see cref="Code"/> is the name of the offending field.
/// </summary>
public readonly record struct Error(string Code, string Description, ErrorKind Kind)
{
    public static Error Failure(string code, string description) => new(code, description, ErrorKind.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorKind.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorKind.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorKind.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorKind.Unauthorized);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorKind.Forbidden);

    public static Error Unexpected(string code, string description) => new(code, description, ErrorKind.Unexpected);
}
