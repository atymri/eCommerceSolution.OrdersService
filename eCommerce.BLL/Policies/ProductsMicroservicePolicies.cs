
using DnsClient.Internal;
using eCommerce.BusinessLogicLayer.DTOs;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.Fallback;
using System.Net;
using System.Text;
using System.Text.Json;

namespace eCommerce.BLL.Policies;

internal class ProductsMicroservicePolicies : IProductsMicroservicePolicies
{
    private readonly ILogger<ProductsMicroservicePolicies> _logger;
    private readonly IGlobalPolicies _globalPolicies;
    public ProductsMicroservicePolicies(ILogger<ProductsMicroservicePolicies> logger, IGlobalPolicies globalPolicies)
    {
        _logger = logger;
        _globalPolicies = globalPolicies;
    }
    #region comments

    // NOTE: you can declare a certain amount of concurrent requests for this policy,
    // and after that amount of request reaches, it will store them in a queue, which you declare the size of it.
    // if queue is full and we get a new req to dependency service, it will throw a new BulkheadRejectedException()

    // EX: i have setted the number of cuncurrent requests to 2, means that only 2 requests can be send cuncurrently to this service
    // after the number rises up, the requests will be stored in a queue, and send in FIFO order,
    // if queu fills up (you can declare a max number of requests in a queu), it sends a new BulkheadRejectedException(), thats all.
    //public AsyncBulkheadPolicy<HttpResponseMessage> GetBulkheadPolicy()
    //{
    //    var bulkHeadPolicy = Policy.BulkheadAsync<HttpResponseMessage>(
    //        maxParallelization: 3,
    //        maxQueuingActions: 6,
    //        async (context) =>
    //    {
    //        _logger.LogWarning("Bulkhead isolation triggered, cant send any more requests, QUEUE IS FULL");
    //        throw new BulkheadRejectedException("Bulkhead QUEUE IS FULL");
    //    });

    //    return bulkHeadPolicy;
    //}

    // NOTE: YOU CAN NOT USE THIS POLICY ALONG WITH WAIT&RETRY AND CIRCUIT BREAKER POLICIES
    // this policy will check the response, if its unsuccessful, it will do something else (in this case it sends mocked data)
    // you can send request to other sources or do something else with current request with this
    // its an important policy in fault-tolerance 
    #endregion

    public AsyncFallbackPolicy<HttpResponseMessage> GetProductFallback()
    {
        var fallback = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .FallbackAsync(async (ct) =>
            {
                _logger.LogWarning("REQUEST FAILED, FALLING BACK...");

                var faultData = JsonSerializer.Serialize(new ProductValidationResult(
                        IsSuccess: false,
                        Message: "Product service is temporarily unavailable",
                        InvalidIDs: [],
                        Products: []
                    ));

                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                { Content = new StringContent(faultData, Encoding.UTF8, "application/json") };

            });

        return fallback;
    }

    public AsyncPolicy<HttpResponseMessage> WrappedUpPolicies()
    {
        var bulkHeadIsolation = _globalPolicies.BulkHeadIsolationPolicy(maxParallelReqs: 5, maxReqsInQueue: 10);
        var fallBack = GetProductFallback();
        return  Policy.WrapAsync<HttpResponseMessage>(bulkHeadIsolation, fallBack);
    }
}
