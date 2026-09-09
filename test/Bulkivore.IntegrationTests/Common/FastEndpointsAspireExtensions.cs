using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using Bulkivore.Api.Endpoints.Common;
using FastEndpoints;

namespace Bulkivore.IntegrationTests.Common;

public static class FastEndpointsAspireExtensions
{
    private static readonly JsonSerializerOptions WebOptions = JsonSerializerConfig.Default;

    extension(HttpClient client)
    {
        private async Task<(HttpResponseMessage Response, TResponse? Payload, string? ErrorMessage)> SendAsync<
            TEndpoint, TRequest, TResponse>(
            HttpMethod method,
            TRequest? request = default,
            CancellationToken ct = default)
            where TEndpoint : IEndpoint
        {
            var url = request is not null
                ? client.GetTestUrlFor<TEndpoint>(request)
                : client.GetTestUrlFor<TEndpoint>(EmptyRequest.Instance);

            using var httpRequest = new HttpRequestMessage(method, url);

            if (request is not null && method != HttpMethod.Get && method != HttpMethod.Delete)
            {
                httpRequest.Content = JsonContent.Create(request, options: WebOptions);
            }

            var response = await client.SendAsync(httpRequest, ct);

            // 1. HTTP 4xx/5xx: Capture raw error body (ProblemDetails, validation errors, etc.)
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                return (response, default, errorContent);
            }

            // 2. Empty response check (e.g., 204 No Content or empty 200 OK)
            if (response.Content.Headers.ContentLength == 0)
            {
                return (response, default, null);
            }

            // 3. HTTP 200 OK: Safely deserialize payload
            try
            {
                var payload = await response.Content.ReadFromJsonAsync<TResponse>(WebOptions, cancellationToken: ct);
                return (response, payload, null);
            }
            catch (Exception ex)
            {
                var rawJson = await response.Content.ReadAsStringAsync(ct);
                var deserializationError =
                    $"Failed to deserialize [{typeof(TResponse).Name}]: {ex.Message}\nReceived JSON:\n{rawJson}";
                return (response, default, deserializationError);
            }
        }

        // ==========================================
        // 2. Verb Wrappers
        // ==========================================

        public Task<(HttpResponseMessage Response, TResponse? Payload, string? ErrorMessage)> PostAsync<TEndpoint,
            TRequest, TResponse>(
            TRequest request,
            CancellationToken ct = default)
            where TEndpoint : IEndpoint =>
            client.SendAsync<TEndpoint, TRequest, TResponse>(HttpMethod.Post, request, ct);

        public Task<(HttpResponseMessage Response, TResponse? Payload, string? ErrorMessage)> PutAsync<TEndpoint,
            TRequest, TResponse>(
            TRequest request,
            CancellationToken ct = default)
            where TEndpoint : IEndpoint =>
            client.SendAsync<TEndpoint, TRequest, TResponse>(HttpMethod.Put, request, ct);

        public Task<(HttpResponseMessage Response, TResponse? Payload, string? ErrorMessage)> PatchAsync<TEndpoint,
            TRequest, TResponse>(
            TRequest request,
            CancellationToken ct = default)
            where TEndpoint : IEndpoint =>
            client.SendAsync<TEndpoint, TRequest, TResponse>(HttpMethod.Patch, request, ct);

        public Task<(HttpResponseMessage Response, TResponse? Payload, string? ErrorMessage)> GetAsync<TEndpoint,
            TRequest, TResponse>(
            TRequest request,
            CancellationToken ct = default)
            where TEndpoint : IEndpoint =>
            client.SendAsync<TEndpoint, TRequest, TResponse>(HttpMethod.Get, request, ct);

        public Task<(HttpResponseMessage Response, TResponse? Payload, string? ErrorMessage)> GetAsync<TEndpoint,
            TResponse>(CancellationToken ct = default)
            where TEndpoint : IEndpoint =>
            client.SendAsync<TEndpoint, object, TResponse>(HttpMethod.Get, null, ct);

        public Task<(HttpResponseMessage Response, TResponse? Payload, string? ErrorMessage)> DeleteAsync<TEndpoint,
            TRequest, TResponse>(
            TRequest request,
            CancellationToken ct = default)
            where TEndpoint : IEndpoint =>
            client.SendAsync<TEndpoint, TRequest, TResponse>(HttpMethod.Delete, request, ct);
    }
}
