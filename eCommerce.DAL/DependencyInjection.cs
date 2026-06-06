using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using eCommerce.DAL.RepositoryContracts;
using eCommerce.DAL.Repositories;

namespace eCommerce.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDALlayer(this IServiceCollection services, IConfiguration conf)
    {
        var connectionString = conf.GetConnectionString("Default")!
            .Replace("$MONGO_HOST", Environment.GetEnvironmentVariable("MONGO_HOST"))
            .Replace("$MONGO_PORT", Environment.GetEnvironmentVariable("MONGO_PORT"));

        services.AddSingleton<IMongoClient>(new MongoClient(connectionString)); // connecting to mongo service then adding it in ioc

        services.AddScoped<IMongoDatabase>(provider =>
            provider.GetRequiredService<IMongoClient>()
                .GetDatabase(Environment.GetEnvironmentVariable("MONGO_DB")));

        services.AddScoped<IOrdersRepository, OrdersRerpository>();


        return services;
    }
}
