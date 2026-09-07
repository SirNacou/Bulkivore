using FastEndpoints;
using FluentValidation.Results;

namespace Bulkivore.Api.Endpoints.Common;

public static class EndpointResultExtensions
{
    extension<TRequest, TResponse>(ResponseSender<TRequest, TResponse> send)
        where TRequest : notnull
        where TResponse : notnull
    {
        public Task ErrorOrResultAsync(
            ErrorOr<TResponse> result,
            CancellationToken ct = default)
        {
            var httpContext = send.HttpContext;
            var response = httpContext.Response;

            if (httpContext.ResponseStarted())
            {
                return Task.CompletedTask;
            }

            // 1. Success: Strongly typed, zero reflection needed
            if (!result.IsError)
            {
                return response.SendAsync(result.Value, cancellation: ct);
            }

            // 2. Validation Errors: Maps to RFC 7807 problem details via FastEndpoints
            if (result.Errors.All(e => e.Type == ErrorType.Validation))
            {
                List<ValidationFailure> failures =
                [
                    .. result.Errors.Select(e => new ValidationFailure(e.Code, e.Description))
                ];
                return response.SendErrorsAsync(failures, cancellation: ct);
            }

            // 3. First non-validation failure mapping
            var problem = result.Errors
                .Cast<Error?>()
                .FirstOrDefault(e => e!.Value.Type != ErrorType.Validation);

            return problem?.Type switch
            {
                ErrorType.Conflict => response.SendAsync(
                    "Duplicate submission!",
                    StatusCodes.Status409Conflict,
                    cancellation: ct
                ),
                ErrorType.NotFound => response.SendNotFoundAsync(ct),
                ErrorType.Unauthorized => response.SendUnauthorizedAsync(ct),
                ErrorType.Forbidden => response.SendForbiddenAsync(ct),
                null => throw new InvalidOperationException(
                    "An error occurred, but no matching non-validation error was found."
                ),
                _ => response.SendAsync(
                    new { error = problem.Value.Description ?? "Internal server error" },
                    StatusCodes.Status500InternalServerError,
                    cancellation: ct
                )
            };
        }
    }
}
