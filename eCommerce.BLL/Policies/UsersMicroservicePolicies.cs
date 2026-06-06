using Microsoft.Extensions.Logging;
using Polly;

namespace eCommerce.BLL.Policies;

internal class UsersMicroservicePolicies : IUsersMicroservicePolicies
{
    private readonly ILogger<UsersMicroservicePolicies> _logger;
    private readonly IGlobalPolicies _globalPolicies;
    public UsersMicroservicePolicies(ILogger<UsersMicroservicePolicies> logger,
        IGlobalPolicies globalPolicies)
    {
        _logger = logger;
        _globalPolicies = globalPolicies;
    }

    #region comments
    //public AsyncBulkheadPolicy<HttpResponseMessage> GetBulkHeadPolicy()
    //{
    //    var policy = Policy.BulkheadAsync<HttpResponseMessage>(
    //        maxParallelization: 3,
    //        maxQueuingActions: 6,
    //        async (context) =>
    //        {
    //            _logger.LogWarning("Bulkhead isolation triggered, QUEUE IS FULL!");

    //            throw new BulkheadRejectedException("QUEUE IS FULL!");
    //        });

    //    return policy;
    //}

    // NOTE: this policy implements CircuitBreakerDesignPattern: https://en.wikipedia.org/wiki/Circuit_breaker_design_pattern
    // it has three modes: open, closed and half-open
    // open: no request is allowed to the host for specific duration of time (in this case its set on durationOfBreak)
    // close: every request is allowed
    // half-open: after the duration of open state reaches,
    // one request is allowed and if it is successful, the mode changes to closed, otherwise, open again.
    //public AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    //{
    //    var policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    //        .CircuitBreakerAsync(
    //            handledEventsAllowedBeforeBreaking: 3,
    //            durationOfBreak: TimeSpan.FromMinutes(2),
    //            onBreak: (result, timespan) =>
    //            {
    //                _logger.LogWarning($"CIRCUIT BREAKER OPENED FOR {timespan.TotalSeconds} FOR 3 FAILED ATTEMPTS");
    //            },
    //            onReset: () =>
    //            {
    //                _logger.LogInformation($"CURCT BREAKER MOVING FROM HALF-OPENED TO CLOSED STATE");
    //            });
    //    return policy;
    //}

    //public AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy()
    //{
    //    var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1.5));
    //    return policy;
    //}

    // NOTE: this policy sends the same request if the response is not successful (anything other than 2XX responses)
    // that sleepDuration indicates that when it should resend the request, in this case its 2 ^ retryAttempt
    // like it resends the request again, then in 2 secs, it retries, and in 4, 8, and so on...
    //public AsyncPolicy<HttpResponseMessage> GetWaitAndRetryPolicy()
    //{
    //    var policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    //        .WaitAndRetryAsync(
    //            retryCount: 3,S
    //            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
    //            onRetry: (outcome, timespan, retryAttempt, context) =>
    //            {
    //                _logger.LogWarning($"FAILED, RERTRYING (ATTEMPT #{retryAttempt}:{timespan.TotalSeconds})");
    //            }
    //        );

    //    return policy;
    //}
    #endregion

    public AsyncPolicy<HttpResponseMessage> WrapUpPolicies()
        => Policy.WrapAsync<HttpResponseMessage>(
            _globalPolicies.WaitAndRetryPolicy(retryCount: 3),
            _globalPolicies.CircuitBreaker(duration: TimeSpan.FromMinutes(2), threshold: 100),
            _globalPolicies.TimeoutPolicy(duration: TimeSpan.FromSeconds(10.5)),
            _globalPolicies.BulkHeadIsolationPolicy(maxParallelReqs: 5, maxReqsInQueue: 10));
}
