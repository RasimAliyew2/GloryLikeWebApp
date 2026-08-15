using GloryLikeWebApp.Models;

namespace GloryLikeWebApp.Services;

public interface ISkillAssessmentApiService
{
    Task<SkillAssessmentApiResult<SkillQuestionnaireResponse>>
        GenerateAsync(
            GenerateSkillQuestionnaireRequest request,
            CancellationToken cancellationToken = default);

    Task<SkillAssessmentApiResult<SkillDepthAssessmentResult>>
        SubmitAsync(
            SubmitSkillDepthAssessmentRequest request,
            CancellationToken cancellationToken = default);
}
