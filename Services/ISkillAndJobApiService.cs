using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public interface ISkillAndJobApiService
{
    Task<SkillAndJobApiResult> GetJobFamiliesAsync(
        CancellationToken cancellationToken = default);
}
