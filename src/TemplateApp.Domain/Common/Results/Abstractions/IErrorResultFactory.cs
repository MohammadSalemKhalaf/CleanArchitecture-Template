using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Domain.Common.Results.Abstractions;

/// <summary>Lets generic code build a failed result without reflection or <c>dynamic</c>.</summary>
public interface IErrorResultFactory<out TSelf>
    where TSelf : IErrorResultFactory<TSelf>
{
    static abstract TSelf FromErrors(IReadOnlyList<Error> errors);
}
