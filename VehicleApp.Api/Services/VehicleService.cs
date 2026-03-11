using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services;

/// <summary>
/// Сервис для получения информации о транспортном средстве
/// </summary>
public class VehicleService(IDistributedCache cache, IConfiguration configuration,
                ILogger<VehicleService> logger) : IVehicleService
{
    private readonly int _expirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 15);
    /// <inheritdoc />
    public async Task<Vehicle> GetVehicle(int id)
    {
        var cacheKey = $"vehicle-{id}";
        logger.LogInformation("Requesting vehicle {VehicleId} from cache", id);
        var cachedData = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedVehicle = JsonSerializer.Deserialize<Vehicle>(cachedData);

                if (cachedVehicle != null)
                {
                    logger.LogInformation("Vehicle {VehicleId} retrieved from cache", id);
                    return cachedVehicle;
                }
                logger.LogWarning("Vehicle {VehicleId} found in cache but deserialization returned null", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize vehicle {VehicleId} from cache", id);
            }
        }

        logger.LogInformation("Vehicle {VehicleId} not found in cache. Generating", id);

        var vehicle = VehicleGenerator.GenerateVehicle(id);

        try
        {  
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expirationMinutes)
            };
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(vehicle), cacheOptions);
            logger.LogInformation("Vehicle {VehicleId} generated and cached", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache vehicle {VehicleId}. Continuing without cache.", id);
        }
        return vehicle;
    }
}
