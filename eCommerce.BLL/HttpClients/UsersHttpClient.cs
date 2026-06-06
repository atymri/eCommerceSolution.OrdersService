using eCommerce.BLL.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace eCommerce.BLL.HttpClients;

public class UsersHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    public UsersHttpClient(HttpClient client, IDistributedCache cache)
    {
        _httpClient = client;
        _cache = cache;
    }

    public async Task<UserValidationResult> ValidateUsers(List<Guid> userIds)
    {
        var response = await _httpClient
            .PostAsJsonAsync("/users/validate", userIds);

        var validationRes = await response.Content.ReadFromJsonAsync<UserValidationResult>()
            ?? throw new InvalidOperationException("Invalid response from users service");

        return validationRes;
    }

    public async Task<UserRersponse> GetUserByIDAsync(Guid id)
    {
        var faultData = new UserRersponse()
        {
            Email = "Temporarily Unavailable",
            PersonName = "Temporarily Unavailable",
            Gender = "Temporarily Unavailable",
            userId = Guid.Empty
        };

        string cacheKey = $"user:{id}";

        try
        {
            if (await _cache.GetStringAsync(cacheKey) is string cachedUser)
                return JsonSerializer.Deserialize<UserRersponse>(cachedUser)!;
    
            var response = await _httpClient.GetAsync($"/api/users/{id}");
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                    throw new HttpRequestException("bad request", null, HttpStatusCode.BadRequest);
                else
                    //throw new HttpRequestException($"failed with status code: {response.StatusCode}", null, response.StatusCode);
                    return faultData;
            }

            var user = await response.Content.ReadFromJsonAsync<UserRersponse>();

            if (user is null)
                throw new ArgumentException("invalid user id", nameof(id));

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(3)));

            return user;
        }
        //catch (BrokenCircuitException ex)
        //{
        //    return faultData;
        //}
        //catch (TimeoutRejectedException ex)
        //{
        //    return faultData;
        //}
        //catch (BulkHeadRejectedException ex)
        //{
        //   return faultData;
        //}
        catch(Exception ex)
        {
            return faultData;
        }
    }
}
