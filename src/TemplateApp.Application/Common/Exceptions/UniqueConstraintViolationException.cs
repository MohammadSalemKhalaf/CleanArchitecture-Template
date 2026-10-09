namespace TemplateApp.Application.Common.Exceptions;

/// <summary>
/// Raised by the persistence layer when a unique index rejects a write. Handlers check for duplicates up front;
/// this covers the race where two requests pass that check concurrently, so it can be translated to a conflict.
/// </summary>
public sealed class UniqueConstraintViolationException(string message, Exception innerException)
    : Exception(message, innerException);
