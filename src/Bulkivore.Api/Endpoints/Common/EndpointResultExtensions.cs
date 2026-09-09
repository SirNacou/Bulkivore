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

            // 1. Success: Send payload directly
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
            var error = result.Errors.FirstOrDefault(e => e.Type != ErrorType.Validation);

            var statusCode = error.Type switch
            {
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            // Return structured details retaining the real domain error description
            return response.SendAsync(
                new
                {
                    title = error.Code,
                    detail = error.Description,
                    status = statusCode
                },
                statusCode,
                cancellation: ct);
        }
    }
}
