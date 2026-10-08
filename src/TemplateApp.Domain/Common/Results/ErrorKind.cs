namespace TemplateApp.Domain.Common.Results;

public enum ErrorKind
{
    /// <summary>A business rule rejected the operation.</summary>
    Failure,

    /// <summary>The input is malformed or violates a constraint.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The operation conflicts with the current state (duplicates, invalid transitions).</summary>
    Conflict,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed to perform the operation.</summary>
    Forbidden,

    /// <summary>An expected-but-unrecoverable failure that is not the caller's fault.</summary>
    Unexpected,
}
