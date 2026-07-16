using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

/// <summary>
/// Same service naming and endpoint contract as Mobile App.
/// </summary>
public interface IJobOffersApiService
{
    Task<IReadOnlyList<JobOfferApiItem>> GetJobOffersAsync(
        CancellationToken cancellationToken = default);
}
