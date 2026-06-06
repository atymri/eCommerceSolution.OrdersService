using DnsClient.Internal;
using eCommerce.BusinessLogicLayer.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace eCommerce.BLL.HttpClients;

public class ProductsHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductsHttpClient> _logger;
    private readonly IDistributedCache _cache;
    public ProductsHttpClient(HttpClient client, IDistributedCache cache, ILogger<ProductsHttpClient> logger)
    {
        _httpClient = client;
        _cache = cache;
        _logger = logger;
    }

    #region comment

    //public async Task<ProductValidationResult> ValidateProductIDsAsync(List<Guid> productIDs)
    //{
    //    // saving serviceRes to cache
    //    // data gets stored in key-value pairs like this:
    //    // key: product:(id)
    //    // value: { "ProductName":"...", ...}

    //    var cached = new List<Task<string?>>();
    //    var uncached = new List<Guid>();
    //    var existing = new List<ProductSummery>();

    //    foreach (var id in productIDs.Distinct().ToArray())
    //    {
    //        if (_cache.GetStringAsync($"product:{id}") is Task<string> data)
    //            cached.Add(data);
    //        else
    //            uncached.Add(id);
    //    }

    //    var results = await Task.WhenAll(cached);
    //    foreach (var res in results)
    //        existing.Add(JsonSerializer.Deserialize<ProductSummery>(res!)!);

    //    if (!uncached.Any())
    //        return new ProductValidationResult(
    //            true, "all from cache!", [], existing);

    //    var serviceRes = await _httpClient
    //        .PostAsJsonAsync("/api/products/validate", uncached);

    //    var result = await serviceRes.Content.ReadFromJsonAsync<ProductValidationResult>()
    //         ?? throw new InvalidOperationException("Invalid serviceRes from product service");

    //    return new ProductValidationResult(
    //        result.IsSuccess,
    //        result.Message,
    //        result.InvalidIDs,
    //        existing.Concat(result.Products).ToList()
    //        );
    //}

    #endregion comment

    public async Task<ProductSummery?> ValidateProductIDAsync(Guid productId)
    {
        var cacheKey = $"product:{productId}";
        try
        {
            if (await _cache.GetStringAsync(cacheKey) is string cached)
                return JsonSerializer.Deserialize<ProductSummery>(cached);
            
            var response = await _httpClient.GetAsync($"/products/validate/{productId}");

            if (response is null)
                throw new ArgumentException(nameof(productId));

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                
                else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    return JsonSerializer.Deserialize<ProductSummery>(await response.Content.ReadAsStringAsync())
                        ?? throw new NotImplementedException("fallback policy is not implemented");
                
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                    throw new HttpRequestException("bad request", null, HttpStatusCode.BadRequest);
                
                else
                    throw new HttpRequestException($"failed with status code: {response.StatusCode}");
            }

            var serialized = await response.Content.ReadFromJsonAsync<ProductSummery>();
            
            var toCache = JsonSerializer.Serialize(serialized);
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)) // maximum time for cache to be deleted
                .SetSlidingExpiration(TimeSpan.FromMinutes(3)); // if data didn't fetched from cache for this much time, it will be deleted

            await _cache.SetStringAsync(cacheKey, toCache, cacheOptions);

            return serialized;
        }
        catch (BulkheadRejectedException ex)
        {
            _logger.LogWarning(ex, "Bulkhead isolation blocks the request since the request queue is full");

            return new ProductSummery(
              ProductID: Guid.NewGuid(),
              ProductName: "Temporarily Unavailable (Bulkhead)",
              Category: "Temporarily Unavailable (Bulkhead)",
              UnitPrice: 0);
        }
    }
}
