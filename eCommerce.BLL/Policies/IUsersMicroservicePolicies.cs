using eCommerce.BLL.HttpClients;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace eCommerce.BLL.Policies;

public interface IUsersMicroservicePolicies
{
    // THESE POLICIES ARE ALREADY IMPLEMENTED IN THE GLOBAL POLICIES,
    // BUT I ADDED THIS INTERFACE TO SHOW HOW TO CREATE SERVICE-SPECIFIC POLICIES
    
    //AsyncPolicy<HttpResponseMessage> GetWaitAndRetryPolicy();
    //AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
    //AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy();
    //AsyncBulkheadPolicy<HttpResponseMessage> GetBulkHeadPolicy();
    AsyncPolicy<HttpResponseMessage> WrapUpPolicies();
}
