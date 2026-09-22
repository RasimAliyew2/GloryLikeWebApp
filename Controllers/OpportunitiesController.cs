using System.Security.Claims;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public sealed class OpportunitiesController : Controller
{
    private readonly ILogger<OpportunitiesController> _logger;
    private readonly IVacancyApiService _vacancyApiService;

    public OpportunitiesController(
        ILogger<OpportunitiesController> logger,
        IVacancyApiService vacancyApiService)
    {
        _logger = logger;
        _vacancyApiService = vacancyApiService;
    }

    [HttpGet("/Opportunities")]
    public async Task<IActionResult> OpportunitiesPage(
        string? search,
        int? vacancyId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Challenge();

        if (User.FindFirstValue("accountType") == "student")
            return RedirectToAction("Internships", "Student", new { search, vacancyId });

        var model = CreateBaseModel(candidateUserId, search);
        model.SuccessMessage = TempData["ApplicationSuccessMessage"] as string;

        try
        {
            var result = await _vacancyApiService.GetCandidateVacanciesAsync(
                candidateUserId,
                cancellationToken);

            if (!result.Success || result.Data is null)
            {
                model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Vacancies SQL datası yüklənmədi."
                    : result.Message;

                return View("OpportunitiesPage", model);
            }

            var data = result.Data;
            model.CurrentJobName = ResolveCurrentJobName(data);
            var opportunities = OpportunityCardBuilder.Build(data.Vacancies
                .Where(vacancy => vacancy.VacancyType == "Employee").ToList());

            if (!string.IsNullOrWhiteSpace(model.SearchText))
            {
                opportunities = opportunities
                    .Where(opportunity => opportunity.SearchText.Contains(
                        model.SearchText,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (vacancyId.HasValue)
                opportunities = opportunities.Where(o => o.Id == vacancyId.Value).ToList();
            model.Opportunities = opportunities;

            if (model.Opportunities.Count == 0)
            {
                model.EmptyMessage = string.IsNullOrWhiteSpace(model.SearchText)
                    ? string.IsNullOrWhiteSpace(data.Message)
                        ? "JobFamilyId-nizə uyğun aktiv vacancy tapılmadı."
                        : data.Message
                    : $"“{model.SearchText}” axtarışına uyğun vacancy tapılmadı.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Candidate vacancies could not be loaded for user {UserId}.",
                candidateUserId);

            model.ErrorMessage =
                "Vacancies SQL datası yüklənmədi: " + exception.Message;
        }

        return View("OpportunitiesPage", model);
    }

    [HttpGet("/Opportunities/{vacancyId:int}/Apply")]
    public async Task<IActionResult> ApplyScreening(
        int vacancyId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Challenge();

        var model = await BuildScreeningApplicationModelAsync(
            candidateUserId,
            vacancyId,
            cancellationToken);

        if (model.Vacancy?.HasApplied == true)
        {
            TempData["ApplicationSuccessMessage"] =
                "Bu vakansiya üçün müraciətiniz artıq mövcuddur.";
            return User.FindFirstValue("accountType") == "student"
                ? RedirectToAction("Internships", "Student")
                : RedirectToAction(nameof(OpportunitiesPage));
        }

        return View("ApplyScreening", model);
    }

    [HttpPost("/Opportunities/{vacancyId:int}/Apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitScreening(
        int vacancyId,
        ScreeningApplicationSubmissionModel input,
        CancellationToken cancellationToken)
    {
        if (!TryGetCandidateUserId(out var candidateUserId))
            return Challenge();

        input.Answers ??= new List<ScreeningApplicationAnswerInput>();

        var result = await _vacancyApiService.ApplyAsync(
            vacancyId,
            candidateUserId,
            input.Answers.Select(answer => new CandidateScreeningAnswerApiInput
            {
                QuestionId = answer.QuestionId,
                AnswerText = answer.AnswerText?.Trim() ?? string.Empty,
                SelectedChoiceIds = answer.SelectedChoiceIds?
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList() ?? new List<int>()
            }).ToList(),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            var model = await BuildScreeningApplicationModelAsync(
                candidateUserId,
                vacancyId,
                cancellationToken);
            model.Input = input;
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Müraciət SQL-də saxlanmadı."
                : result.Message;

            return View("ApplyScreening", model);
        }

        TempData["ApplicationSuccessMessage"] =
            result.Data.Status.Equals("ScreeningFailed", StringComparison.OrdinalIgnoreCase)
                ? "Müraciət göndərildi və screening cavabları qeydə alındı."
                : "Müraciət və screening cavabları uğurla göndərildi.";

        return User.FindFirstValue("accountType") == "student"
            ? RedirectToAction("Internships", "Student")
            : RedirectToAction(nameof(OpportunitiesPage));
    }

    private async Task<ScreeningApplicationPageViewModel>
        BuildScreeningApplicationModelAsync(
            int candidateUserId,
            int vacancyId,
            CancellationToken cancellationToken)
    {
        var identity = CreateBaseModel(candidateUserId, null);
        var model = new ScreeningApplicationPageViewModel
        {
            CandidateUserId = candidateUserId,
            DisplayName = identity.DisplayName
        };
        var result = await _vacancyApiService.GetCandidateVacanciesAsync(
            candidateUserId,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Vakansiya screening məlumatları yüklənmədi."
                : result.Message;
            return model;
        }

        model.Vacancy = result.Data.Vacancies.FirstOrDefault(
            vacancy => vacancy.VacancyId == vacancyId);

        if (model.Vacancy is null)
        {
            model.ErrorMessage =
                "Vakansiya tapılmadı və ya artıq müraciət üçün aktiv deyil.";
            return model;
        }

        model.Vacancy.ScreeningQuestions = model.Vacancy.ScreeningQuestions
            .OrderBy(question => question.SortOrder)
            .ToList();
        model.Input.Answers = model.Vacancy.ScreeningQuestions
            .Select(question => new ScreeningApplicationAnswerInput
            {
                QuestionId = question.QuestionId
            })
            .ToList();

        return model;
    }

    private bool TryGetCandidateUserId(out int candidateUserId)
    {
        return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out candidateUserId)
            && candidateUserId > 0;
    }

    private OpportunitiesPageViewModel CreateBaseModel(
        int userId,
        string? search)
    {
        var firstName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var surname = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var userName = User.FindFirstValue("username") ?? string.Empty;
        var displayName = string.Join(
            " ",
            new[] { firstName, surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.IsNullOrWhiteSpace(userName)
                ? "Candidate"
                : userName;
        }

        return new OpportunitiesPageViewModel
        {
            UserId = userId,
            DisplayName = displayName,
            UserName = userName,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            SearchText = search?.Trim() ?? string.Empty
        };
    }

    private static string ResolveCurrentJobName(
        CandidateVacancyListApiResponse response)
    {
        var names = response.CandidateJobFamilyNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        if (names.Count == 0)
        {
            names = response.Vacancies
                .Where(vacancy =>
                    !string.IsNullOrWhiteSpace(vacancy.JobFamilyName))
                .Select(vacancy => vacancy.JobFamilyName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();
        }

        return string.Join(", ", names);
    }

}
