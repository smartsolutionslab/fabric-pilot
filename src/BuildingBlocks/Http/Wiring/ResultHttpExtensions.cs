using Microsoft.AspNetCore.Http.HttpResults;

namespace Microsoft.AspNetCore.Http;

public static class ResultHttpExtensions
{
    /// <summary>200 with the value, or problem details carrying the error's status and code.</summary>
    /// <remarks>The typed union tells the OpenAPI document what a success returns, so clients are generated with its type.</remarks>
    public static Results<Ok<TValue>, ProblemHttpResult> ToHttpResult<TValue, TError>(this Result<TValue, TError> result)
        where TValue : notnull
        where TError : ApiError =>
        result.ToHttpResult(value => TypedResults.Ok(value));

    /// <summary>
    /// The success answered as <paramref name="onSuccess"/> makes it (<c>201</c> with a location, a file), or the same
    /// problem details as <see cref="ToHttpResult{TValue, TError}(Result{TValue, TError})"/>.
    /// </summary>
    public static Results<TSuccess, ProblemHttpResult> ToHttpResult<TValue, TError, TSuccess>(this Result<TValue, TError> result, Func<TValue, TSuccess> onSuccess)
        where TValue : notnull
        where TError : ApiError
        where TSuccess : IResult =>
        result.Match<Results<TSuccess, ProblemHttpResult>>(
            value => onSuccess(value),
            error => TypedResults.Problem(
                title: error.Message,
                statusCode: (int)error.Status,
                extensions: new Dictionary<string, object?> { ["code"] = error.Code }));
}
