namespace Bulkivore.Api.Domain.Common.Resilience;

public sealed record RetryOptions
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(3);
    public double BackoffFactor { get; init; } = 2.0;
}

public interface IRetryService
{
    /// <summary>
    /// Retries an operation until the predicate returns false or max attempts are reached.
    /// </summary>
    Task<T> ExecuteUntilAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldRetry,
        RetryOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retries an operation if specific transient exceptions are thrown.
    /// </summary>
    Task<T> ExecuteOnExceptionAsync<T, TException>(
        Func<CancellationToken, Task<T>> operation,
        RetryOptions? options = null,
        CancellationToken ct = default) where TException : Exception;
}
