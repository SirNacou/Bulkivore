using Bulkivore.Api.Domain.Common.Resilience;

namespace Bulkivore.Api.Infrastructure.Resilience;

public partial class RetryService(ILogger<RetryService> logger) : IRetryService
{
    public async Task<T> ExecuteUntilAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldRetry,
        RetryOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new RetryOptions();
        var attempt = 0;
        while (true)
        {
            attempt++;
            var result = await operation(ct);

            if (!shouldRetry(result) || attempt >= options.MaxAttempts)
            {
                return result;
            }

            var delay = CalculateJitteredDelay(attempt, options);
            LogConditionMetForRetryAttemptAttemptMaxWaitingDelayMs(
                attempt,
                options.MaxAttempts,
                delay.TotalMilliseconds
            );

            await Task.Delay(delay, ct);
        }
    }

    public async Task<T> ExecuteOnExceptionAsync<T, TException>(
        Func<CancellationToken, Task<T>> operation,
        RetryOptions? options = null,
        CancellationToken ct = default) where TException : Exception
    {
        options ??= new RetryOptions();
        var attempt = 0;

        while (true)
        {
            try
            {
                attempt++;
                return await operation(ct);
            }
            catch (TException ex) when (attempt < options.MaxAttempts)
            {
                var delay = CalculateJitteredDelay(attempt, options);
                logger.LogWarning(
                    ex,
                    "Transient error on attempt {Attempt}/{Max}. Retrying in {Delay}ms",
                    attempt,
                    options.MaxAttempts,
                    delay.TotalMilliseconds
                );

                await Task.Delay(delay, ct);
            }
        }
    }

    private static TimeSpan CalculateJitteredDelay(int attempt, RetryOptions options)
    {
        // Exponential backoff: baseDelay * (factor ^ (attempt - 1))
        var exponentialMs = options.InitialDelay.TotalMilliseconds * Math.Pow(options.BackoffFactor, attempt - 1);
        var cappedMs = Math.Min(exponentialMs, options.MaxDelay.TotalMilliseconds);

        // Full jitter: random range between 0 and capped backoff
        var jitteredMs = Random.Shared.NextDouble() * cappedMs;

        return TimeSpan.FromMilliseconds(jitteredMs);
    }

    [LoggerMessage(LogLevel.Debug, "Condition met for retry. Attempt {Attempt}/{Max}. Waiting {Delay}ms")]
    partial void LogConditionMetForRetryAttemptAttemptMaxWaitingDelayMs(int attempt, int max, double delay);
}
