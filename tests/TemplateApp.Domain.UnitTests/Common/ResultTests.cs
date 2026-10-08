using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Domain.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.NotFound("Sample.NotFound", "Not here.");

    [Fact]
    public void Value_OfFailedResult_Throws()
    {
        Result<int> result = SampleError;

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void FirstError_OfSuccessfulResult_Throws()
    {
        Result<int> result = 42;

        Assert.Throws<InvalidOperationException>(() => result.FirstError);
    }

    [Fact]
    public void FromErrors_WithoutErrors_Throws()
    {
        Assert.Throws<ArgumentException>(() => Result<int>.FromErrors([]));
    }

    [Fact]
    public void Success_FromNullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => (Result<string>)(string)null!);
    }

    [Fact]
    public void Match_InvokesTheBranchForTheOutcome()
    {
        Result<int> success = 21;
        Result<int> failure = SampleError;

        Assert.Equal("42", success.Match(value => (value * 2).ToString(), _ => "error"));
        Assert.Equal("Sample.NotFound", failure.Match(_ => "value", errors => errors[0].Code));
    }

    [Fact]
    public void Failure_PropagatesErrorsToAnotherValueType()
    {
        Result<int> source = new List<Error> { SampleError, Error.Conflict("Other", "Other.") };

        var propagated = Result.Failure<string>(source.Errors);

        Assert.True(propagated.IsError);
        Assert.Equal(source.Errors, propagated.Errors);
    }
}
