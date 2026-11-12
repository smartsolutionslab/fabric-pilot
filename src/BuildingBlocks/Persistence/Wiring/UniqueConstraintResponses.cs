using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class UniqueConstraintResponses
{
    /// <summary>
    /// A write a unique index refuses answers <c>409</c> problem details with the code
    /// <see cref="UniqueConstraintExceptionHandler.Code"/>; every other database failure stays a <c>500</c>.
    /// </summary>
    public static IServiceCollection AddUniqueConstraintResponses(this IServiceCollection services) =>
        services.AddProblemDetails()
            .AddExceptionHandler<UniqueConstraintExceptionHandler>();

    /// <summary>
    /// The backstop for a race past an application-level uniqueness check. The check stays the primary signal: it
    /// answers with an error that names the rule. Between the check and the insert another writer can take the key;
    /// the unique index then refuses the second insert, and this answers <c>409</c> instead of <c>500</c>.
    /// </summary>
    private sealed partial class UniqueConstraintExceptionHandler(IProblemDetailsService problemDetails, ILogger<UniqueConstraintExceptionHandler> logger) : IExceptionHandler
    {
        /// <summary>The <c>code</c> of every problem a unique-index violation answers.</summary>
        public const string Code = "already-exists";

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            Ensure.That(httpContext).IsNotNull();

            if (!IsUniqueViolation(exception))
            {
                return false;
            }

            LogRefused(logger);

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

            // Nothing from the exception reaches the caller: its message names the index and quotes the colliding values.
            await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails =
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Something with that name or key already exists.",
                    Detail = "Choose a different one; retrying this request unchanged is refused again.",
                    Extensions = { ["code"] = Code },
                },
            });

            return true;
        }

        /// <summary>
        /// Matched on the database's error code, never on the exception type alone: <see cref="DbUpdateConcurrencyException"/>
        /// is a <see cref="DbUpdateException"/> too, and a lost update is not a duplicate key. EF Core wraps the
        /// provider's exception on SaveChanges; raw SQL throws it unwrapped.
        /// </summary>
        private static bool IsUniqueViolation(Exception exception) => exception switch
        {
            PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
            DbUpdateException { InnerException: PostgresException inner } => inner.SqlState == PostgresErrorCodes.UniqueViolation,
            _ => false,
        };

        [LoggerMessage(Level = LogLevel.Information, Message = "A unique index refused the write; answered 409")]
        private static partial void LogRefused(ILogger logger);
    }
}
