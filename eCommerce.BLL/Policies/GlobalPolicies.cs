using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace eCommerce.BLL.Policies;

// read about each policy with examples in IGlobalPolicies interface,
// and you can customize the policies as per your needs.
public class GlobalPolicies : IGlobalPolicies
{
    private readonly ILogger<GlobalPolicies> _logger;
    public GlobalPolicies(ILogger<GlobalPolicies> logger)
    {
        _logger = logger;
    }

    public AsyncPolicy<HttpResponseMessage> WaitAndRetryPolicy(int retryCount = 3)
        => Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (response, timespan, retryCount, context) =>
                {
                    _logger.LogWarning($"Request failed with {response.Result.StatusCode}. " +
                        $"Waiting {timespan} before next retry. Retry attempt {retryCount}");
                });

    public AsyncTimeoutPolicy<HttpResponseMessage> TimeoutPolicy(TimeSpan duration)
        => Policy.TimeoutAsync<HttpResponseMessage>(duration,
            TimeoutStrategy.Optimistic);

    public AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreaker(TimeSpan duration, int threshold = 5)
        => Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: threshold,
                durationOfBreak: duration,
                onBreak: (response, timespan) =>
                    _logger.LogWarning($"Circuit broken due to {response.Result.StatusCode}. Circuit will stay open for {timespan}"),
                onReset: () =>
                    _logger.LogInformation("Circuit reset. Requests will be allowed through."));

    public AsyncBulkheadPolicy<HttpResponseMessage> BulkHeadIsolationPolicy(int maxParallelReqs, int maxReqsInQueue)
        => Policy.BulkheadAsync<HttpResponseMessage>(
                maxParallelization: maxParallelReqs,
                maxQueuingActions: maxReqsInQueue,
                onBulkheadRejectedAsync: (context) => {
                    _logger.LogWarning($"Queue is full, throwing exception");
                    throw new BulkheadRejectedException("Queue is full, request rejected.");
                }
            );

    // i didn't add the fallback policy in here, its specific for each service.
}