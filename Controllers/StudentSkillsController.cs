using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Student;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.StudentPolicy)]
public sealed class StudentSkillsController(StudentSkillsApiService skills, ISkillAndJobApiService taxonomy) : Controller
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("/Student/Skills")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        var skillsTask = skills.GetAsync(UserId, ct);
        var taxonomyTask = taxonomy.GetAllSkillsAsync(ct);
        await Task.WhenAll(skillsTask, taxonomyTask);
        var saved = await skillsTask;
        var catalogue = await taxonomyTask;
        var model = new StudentDashboardViewModel
        {
            Page = "Skills", DisplayName = User.FindFirstValue(ClaimTypes.Name) ?? "Student",
            SkillsAvailable = saved.Success && saved.Data is not null
        };
        StudentDashboardBuilder.Populate(model, saved.Success ? saved.Data ?? [] : [], [], []);
        var management = model.SkillManagement;
        management.TaxonomyAvailable = catalogue.Success;
        if (!model.SkillsAvailable) model.Errors.Add("Your skills and SSI could not be loaded. Please try again.");
        if (!catalogue.Success) management.ErrorMessage = "The skill catalogue is temporarily unavailable. You can still take quizzes for saved skills.";
        var names = model.Skills.Select(skill => skill.SkillName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        management.AvailableSkills = catalogue.Skills.Where(skill => skill.Id > 0 && !names.Contains(skill.SkillName))
            .GroupBy(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(skill => skill.Id).First()).OrderBy(skill => skill.SkillName).ToList();
        management.AssessmentTypes = catalogue.Skills.GroupBy(skill => skill.Id)
            .ToDictionary(group => group.Key, group => group.First().AssessmentType);
        management.SuccessMessage = TempData["StudentSkillsSuccess"] as string;
        management.ErrorMessage = TempData["StudentSkillsError"] as string ?? management.ErrorMessage;
        management.AutoAssessmentSkillId = TempData["StudentAssessmentSkillId"] is int skillId ? skillId : 0;
        management.AutoAssessmentSkillName = TempData["StudentAssessmentSkillName"] as string ?? "";
        return View("~/Views/Skills/StudentSkillsPage.cshtml", model);
    }

    [HttpPost("/Student/Skills/Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(StudentSkillSelection input, CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        if (!ModelState.IsValid) { TempData["StudentSkillsError"] = "Choose a skill from the catalogue."; return RedirectToAction(nameof(Index)); }
        var result = await skills.AddAsync(UserId, input.SkillId, ct);
        if (result.Success && result.Data is not null)
        {
            TempData["StudentSkillsSuccess"] = $"{result.Data.SkillName} added. Take the quiz to check your knowledge.";
            TempData["StudentAssessmentSkillId"] = result.Data.SkillId;
            TempData["StudentAssessmentSkillName"] = result.Data.SkillName;
        }
        else TempData["StudentSkillsError"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Student/Skills/Remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(StudentSkillSelection input, CancellationToken ct)
    {
        if (UserId <= 0) return Challenge();
        if (!ModelState.IsValid) { TempData["StudentSkillsError"] = "Choose a saved skill to remove."; return RedirectToAction(nameof(Index)); }
        var result = await skills.RemoveAsync(UserId, input.SkillId, ct);
        TempData[result.Success ? "StudentSkillsSuccess" : "StudentSkillsError"] = result.Success ? "Skill removed from your profile." : result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Student/Skills/Assessment/Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([FromBody] StartSkillAssessmentRequest input, CancellationToken ct)
    {
        if (UserId <= 0) return Unauthorized();
        if (!ModelState.IsValid || input.SkillId <= 0) return BadRequest(new { success = false, message = "Choose a saved skill and a quiz language." });
        var result = await skills.GenerateAsync(UserId, input.SkillId, input.Language, ct);
        return result.Success && result.Data is not null ? Json(new { success = true, questionnaire = result.Data })
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpPost("/Student/Skills/Assessment/Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([FromBody] CompleteSkillAssessmentRequest input, CancellationToken ct)
    {
        if (UserId <= 0) return Unauthorized();
        if (!ModelState.IsValid || input.SkillId <= 0 || input.QuestionnaireId == Guid.Empty)
            return BadRequest(new { success = false, message = "Answer every quiz question before submitting." });
        var result = await skills.SubmitAsync(UserId, input, ct);
        return result.Success && result.Data is not null ? Json(new { success = true, score = result.Data.Skill.KnowledgeDisplay,
            message = $"Your {result.Data.Skill.SkillName} knowledge score has been saved." })
            : BadRequest(new { success = false, message = result.Message });
    }
}
