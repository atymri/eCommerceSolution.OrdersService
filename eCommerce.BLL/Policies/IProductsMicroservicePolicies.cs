using Polly;
using Polly.Bulkhead;
using Polly.Fallback;
namespace eCommerce.BLL.Policies;

public interface IProductsMicroservicePolicies
{
    // Except FallBackPolicy, others (bulkhead isolation, circuit breaker, wait & retry, timeout) are in global policies.
    public AsyncFallbackPolicy<HttpResponseMessage> GetProductFallback();
    // public AsyncBulkheadPolicy<HttpResponseMessage> GetBulkheadPolicy();
    public AsyncPolicy<HttpResponseMessage> WrappedUpPolicies();
}
