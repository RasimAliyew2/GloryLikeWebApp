namespace GloryLikeWebApp.Services;

public interface ILocationLookupService
{
    Task<LocationLookupResult> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}

public sealed class LocationLookupResult
{
    public bool Success { get; init; }
    public string City { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
