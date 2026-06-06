using Polly;
using eCommerce.BLL.HttpClients;
using eCommerce.BLL.Profiles;
using eCommerce.BLL.ServiceContracts;
using eCommerce.BLL.Services;
using eCommerce.BLL.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using eCommerce.BLL.Policies;

namespace eCommerce.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBLLlayer(this IServiceCollection services, IConfiguration config)
    {
        services.AddAutoMapper(cfg => { }, typeof(OrderProfile));

        services.AddValidatorsFromAssembly(typeof(OrderAddRequestValidator).Assembly);
       // services.AddHttpClient();

        services.AddScoped<IOrdersService, OrdersService>();

        #region comment
        // NOTE: this handler executes right when response is recieved from httpClient, 
        //       even before it gets save in the local variable
        //       that lambda expression in HandleResult is the trigger of retry, in this case
        //       if result is not success status code (200 or any status of 200 like 201) it retries for 3 times
        //       each time gets doubled:
        //              retryAttempt: 1, sleepDuration: 2 sec
        //              retryAttempt: 2, sleepDuration: 4 sec
        //              retryAttempt: 3, sleepDuration: 8 sec

        // NOTE: this is suitable only for transient errors in other services,
        //       like when the dependency service (this current service is dependent) 
        //       is still trying to connect to database or still booting up.
        //       (you can do the same thing in a class as a service, gives much more flexibility.)
        //var handler = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        //    .WaitAndRetryAsync(
        //        retryCount: 3,
        //        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        //        onRetry: (outcome, timespan, retryAttempt, context) =>
        //                {
        //                    // TODO: add some logging in here.
        //                    Console.WriteLine($"FAILED, RETRYING ATTEMPT: {retryAttempt}");
        //                });
        #endregion

        services.AddTransient<IUsersMicroservicePolicies, UsersMicroservicePolicies>(); // it doesn't have any state, so it better be transient.
        services.AddTransient<IProductsMicroservicePolicies, ProductsMicroservicePolicies>();
        services.AddTransient<IGlobalPolicies, GlobalPolicies>();

        // NOTE: for using Redis Centralized Caching you have to install this package:
        // Microsoft.Extensions.Caching.StackExchangeRedis
        services.AddStackExchangeRedisCache(cfg =>
        {
            // i could also use IConfiguration, but anyways
            var host = Environment.GetEnvironmentVariable("REDIS_HOST");
            var port = Environment.GetEnvironmentVariable("REDIS_PORT");
            cfg.Configuration = $"{host}:{port}";
        });

        var usersMicroservicePolicies = services.BuildServiceProvider()
            .GetRequiredService<IUsersMicroservicePolicies>();
        var productsMicroservicePolicies = services.BuildServiceProvider()
            .GetRequiredService<IProductsMicroservicePolicies>();

        services.AddHttpClient<UsersHttpClient>(client =>
        {
            //var host = config["USERSERVICE_HOST"];
            //var port = config["USERSERVICE_PORT"];
            var host = config["GATEWAY_HOST"];
            var port = config["GATEWAY_PORT"];

            client.BaseAddress = new Uri($"http://{host}:{port}");
            client.DefaultRequestHeaders.Add("x-req-from", "orders-service");

        })
        //.AddPolicyHandler(usersMicroservicePolicies.GetWaitAndRetryPolicy())
        //.AddPolicyHandler(usersMicroservicePolicies.GetCircuitBreakerPolicy())
        //.AddPolicyHandler(usersMicroservicePolicies.GetTimeoutPolicy());
        // you can add the handler with AddPolicyHandler method,
        // it gets a PolicyBuilder type as its argument, and its in Polly.
        .AddPolicyHandler(usersMicroservicePolicies.WrapUpPolicies()); // all in one!

        services.AddHttpClient<ProductsHttpClient>(client =>
        {
            //var host = config["PRODUCTSERVICE_HOST"];
            //var port = config["PRODUCTSERVICE_PORT"];
            var host = config["GATEWAY_HOST"];
            var port = config["GATEWAY_PORT"];

            client.BaseAddress = new Uri($"http://{host}:{port}");
            client.DefaultRequestHeaders.Add("x-req-from", "orders-service");

        }) //.AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        //        .WaitAndRetryAsync(3, sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
        //.AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        //        .CircuitBreakerAsync(3, TimeSpan.FromMinutes(2))); // you can not use fallback policy and wait/retry policy at the same time, even with circuit breaker 
        // .AddPolicyHandler(productsMicroservicePolicies.GetProductFallback());
        .AddPolicyHandler(productsMicroservicePolicies.WrappedUpPolicies());
        return services;
    }
}
