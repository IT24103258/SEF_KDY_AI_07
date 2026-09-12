using System.Text.Json;
using FixFlow.Api.Interfaces;

namespace FixFlow.Api.Services;

public class ExternalDistanceMatrixService : IDistanceMatrixService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalDistanceMatrixService> _logger;
    private readonly string _baseUrl;
    private readonly bool _useFallback;

    public ExternalDistanceMatrixService(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalDistanceMatrixService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["DistanceMatrix:BaseUrl"] ?? "http://router.project-osrm.org";
        _useFallback = bool.Parse(configuration["DistanceMatrix:UseFallback"] ?? "true");
    }

    public async Task<double> GetDistanceInKmAsync(double originLat, double originLng, double destLat, double destLng)
    {
        try
        {
            // OSRM API expects format: /route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false
            var requestUrl = $"{_baseUrl}/route/v1/driving/{originLng},{originLat};{destLng},{destLat}?overview=false";
            
            var response = await _httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                {
                    var distanceMeters = routes[0].GetProperty("distance").GetDouble();
                    return Math.Round(distanceMeters / 1000.0, 2);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External Distance Matrix Service (OSRM) failed. Falling back to Haversine formula.");
        }

        if (_useFallback)
        {
            return CalculateHaversineDistance(originLat, originLng, destLat, destLng);
        }

        return 0.0;
    }

    /// <summary>
    /// Fallback Haversine formula to compute great-circle distance between two points in km.
    /// </summary>
    public static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var rLat1 = ToRadians(lat1);
        var rLat2 = ToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(rLat1) * Math.Cos(rLat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(EarthRadiusKm * c, 2);
    }

    private static double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}
