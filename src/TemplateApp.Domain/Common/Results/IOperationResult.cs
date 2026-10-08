namespace TemplateApp.Domain.Common.Results;

/// <summary>Non-generic view of a result, used by pipeline code that does not know the value type.</summary>
public interface IOperationResult
{
    bool IsSuccess { get; }

    IReadOnlyList<Error> Errors { get; }
}
