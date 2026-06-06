using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace eCommerce.BLL.Policies;

public interface IGlobalPolicies
{
    /// <summary>
    /// Implements a Wait-and-Retry policy that automatically resends failed requests after a delay.
    /// This policy is useful for handling transient failures (temporary network issues, momentary service unavailability).
    /// It uses exponential backoff by default, meaning wait times increase with each retry attempt.
    /// </summary>
    /// <param name="retryCount">Number of retry attempts (default: 3). Total attempts = 1 initial + retryCount retries.</param>
    /// <returns>An async policy that retries failed HTTP requests.</returns>
    /// <example>
    /// Example: With retryCount = 3
    /// Attempt 1: Fails immediately
    /// Wait: ~1 second (exponential backoff)
    /// Attempt 2: Fails
    /// Wait: ~2 seconds
    /// Attempt 3: Fails
    /// Wait: ~4 seconds
    /// Attempt 4: Succeeds or final failure
    /// 
    /// Use case: Calling an API that occasionally times out or returns 500 errors
    /// </example>
    AsyncPolicy<HttpResponseMessage> WaitAndRetryPolicy(int retryCount = 3);

    /// <summary>
    /// Implements a Circuit Breaker policy that stops making requests after detecting repeated failures.
    /// Acts like an electrical circuit breaker: when too many failures occur, it "opens" the circuit and blocks further requests.
    /// After the cooldown duration, it enters "half-open" state and tests if the service has recovered.
    /// </summary>
    /// <param name="duration">The cooldown period before attempting to close the circuit and test recovery.</param>
    /// <param name="threshold">Number of consecutive failures needed to open the circuit (default: 5).</param>
    /// <returns>An async circuit breaker policy for HTTP responses.</returns>
    /// <example>
    /// Example: With threshold = 5 and duration = 30 seconds
    /// Requests 1-5: All fail
    /// Circuit OPENS: Block all requests immediately
    /// Next 30 seconds: All requests rejected with BrokenCircuitException (fail fast, no HTTP call)
    /// After 30 seconds: Circuit enters HALF-OPEN state
    /// Next request: Allowed through to test if service recovered
    /// If succeeds: Circuit CLOSES, normal operation resumes
    /// If fails: Circuit OPENS again for another 30 seconds
    /// 
    /// Use case: Protecting your service from hammering a database or API that's completely down
    /// </example>
    AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreaker(TimeSpan duration, int threshold = 5);

    /// <summary>
    /// Implements a Timeout policy that cancels requests if they exceed a specified duration.
    /// Prevents your application from hanging indefinitely on slow or unresponsive services.
    /// Throws a TimeoutRejectedException when the timeout is exceeded.
    /// </summary>
    /// <param name="duration">Maximum time to wait for a response before timing out (e.g., TimeSpan.FromSeconds(5)).</param>
    /// <returns>An async timeout policy for HTTP responses.</returns>
    /// <example>
    /// Example: With duration = 5 seconds
    /// Request sent at: 0:00
    /// No response at: 4:50 (still waiting)
    /// 5:00 - TIMEOUT: Request is cancelled, TimeoutRejectedException thrown
    /// 
    /// Use case: Calling an external API with known performance issues. Instead of waiting 2 minutes for a hung response,
    /// fail fast after 5 seconds and allow the application to try an alternative approach or retry
    /// </example>
    AsyncTimeoutPolicy<HttpResponseMessage> TimeoutPolicy(TimeSpan duration);

    /// <summary>
    /// Implements a Bulkhead Isolation policy that limits concurrent requests to protect system resources.
    /// It queues excess requests and rejects them only when the queue is full.
    /// Prevents one slow dependency from consuming all available threads/resources.
    /// </summary>
    /// <param name="maxParallelReqs">Maximum number of requests that can execute simultaneously.</param>
    /// <param name="maxReqsInQueue">Maximum number of requests allowed to wait in the queue. Excess requests throw BulkheadRejectedException.</param>
    /// <returns>An async bulkhead isolation policy for HTTP responses.</returns>
    /// <example>
    /// Example: With maxParallelReqs = 10 and maxReqsInQueue = 50
    /// Requests 1-10: Execute immediately (parallel slots full)
    /// Requests 11-60: Queued and wait for a slot to become available
    /// Request 61 onwards: REJECTED with BulkheadRejectedException (queue full)
    /// 
    /// When Request 1 completes: Request 11 (first in queue) moves to available slot
    /// 
    /// Use case: Protecting your API from overwhelming a downstream database service.
    /// Bulkhead ensures only 10 concurrent queries run, preventing resource exhaustion while fair-queuing other requests
    /// </example>
    AsyncBulkheadPolicy<HttpResponseMessage> BulkHeadIsolationPolicy(int maxParallelReqs, int maxReqsInQueue);
}