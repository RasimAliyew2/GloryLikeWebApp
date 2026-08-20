using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GloryLikeWebApp.Services;

public sealed class NominatimLocationLookupService : ILocationLookupService
{
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTimeOffset _lastRequestUtc = DateTimeOffset.MinValue;

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NominatimLocationLookupService> _logger;

    public NominatimLocationLookupService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<NominatimLocationLookupService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LocationLookupResult> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = FormattableString.Invariant(
            $"candidate-location:{Math.Round(latitude, 3)}:{Math.Round(longitude, 3)}");

        if (_cache.TryGetValue(cacheKey, out LocationLookupResult? cached)
            && cached is not null)
        {
            return cached;
        }

        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached)
                && cached is not null)
            {
                return cached;
            }

            var elapsed = DateTimeOffset.UtcNow - _lastRequestUtc;
            if (elapsed < TimeSpan.FromSeconds(1.1))
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(1.1) - elapsed,
                    cancellationToken);
            }

            var latitudeText = latitude.ToString("0.######", CultureInfo.InvariantCulture);
            var longitudeText = longitude.ToString("0.######", CultureInfo.InvariantCulture);
            var url = $"reverse?format=jsonv2&lat={latitudeText}&lon={longitudeText}&zoom=10&addressdetails=1&accept-language=en";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            _lastRequestUtc = DateTimeOffset.UtcNow;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Reverse location lookup returned HTTP {StatusCode}.",
                    (int)response.StatusCode);
                return new LocationLookupResult();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("address", out var address)
                || address.ValueKind != JsonValueKind.Object)
            {
                return new LocationLookupResult();
            }

            var city = FirstAddressValue(
                address,
                "city",
                "town",
                "village",
                "municipality",
                "county",
                "state");
            var countryCode = FirstAddressValue(address, "country_code")
                .ToUpperInvariant();
            var country = FirstAddressValue(address, "country");

            if (string.IsNullOrWhiteSpace(city)
                && string.IsNullOrWhiteSpace(country))
            {
                return new LocationLookupResult();
            }

            var displayName = string.Join(
                ", ",
                new[]
                {
                    city,
                    string.IsNullOrWhiteSpace(countryCode) ? country : countryCode
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

            var result = new LocationLookupResult
            {
                Success = true,
                City = city,
                CountryCode = countryCode,
                Country = country,
                DisplayName = displayName
            };

            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new LocationLookupResult();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Reverse location lookup failed.");
            return new LocationLookupResult();
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private static string FirstAddressValue(
        JsonElement address,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (address.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString()!.Trim();
            }
        }

        return string.Empty;
    }
}
