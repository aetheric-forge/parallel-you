using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;

namespace ParallelYou.Host.Hosting;

public sealed class MongoDbHealthCheck([Microsoft.Extensions.DependencyInjection.FromKeyedServices("ParallelYou")] IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await database.RunCommandAsync<BsonDocument>(
            new BsonDocument("ping", 1),
            cancellationToken: cancellationToken);

        return HealthCheckResult.Healthy();
    }
}

public sealed class RedisHealthCheck([Microsoft.Extensions.DependencyInjection.FromKeyedServices("Workbench")] IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await redis.GetDatabase().PingAsync();
        cancellationToken.ThrowIfCancellationRequested();

        return HealthCheckResult.Healthy();
    }
}
