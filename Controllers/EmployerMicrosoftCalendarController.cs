using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployerPolicy)]
public sealed class EmployerMicrosoftCalendarController : Controller
{
    private const string FlowCookieName = "BothFind.MicrosoftCalendar.Flow";

    private readonly IMicrosoftCalendarApiService _calendarApiService;
    private readonly IDataProtector _flowProtector;
    private readonly ILogger<EmployerMicrosoftCalendarController> _logger;

    public EmployerMicrosoftCalendarController(
        IMicrosoftCalendarApiService calendarApiService,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<EmployerMicrosoftCalendarController> logger)
    {
        _calendarApiService = calendarApiService;
        _flowProtector = dataProtectionProvider.CreateProtector(
            "BothFind.MicrosoftCalendar.OAuthFlow.v1");
        _logger = logger;
    }

    [HttpGet("/Employer/Outlook/Connect")]
    public async Task<IActionResult> Connect(
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
            return Challenge();

        var safeReturnUrl = NormalizeReturnUrl(returnUrl);
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var codeVerifier = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(64));
        var codeChallenge = WebEncoders.Base64UrlEncode(
            SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier)));
        var redirectUri = Url.Action(
            nameof(Callback),
            "EmployerMicrosoftCalendar",
            values: null,
            protocol: Request.Scheme,
            host: Request.Host.Value);

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            TempData["CalendarErrorMessage"] = "Microsoft callback URL yaradıla bilmədi.";
            return LocalRedirect(safeReturnUrl);
        }

        var result = await _calendarApiService.GetAuthorizationUrlAsync(
            new MicrosoftCalendarAuthorizationUrlApiRequest
            {
                EmployerUserId = employerUserId,
                RedirectUri = redirectUri,
                State = state,
                CodeChallenge = codeChallenge
            },
            cancellationToken);
        if (!result.Success
            || string.IsNullOrWhiteSpace(result.Data?.AuthorizationUrl))
        {
            TempData["CalendarErrorMessage"] = result.Message;
            return LocalRedirect(safeReturnUrl);
        }

        var flow = new CalendarOAuthFlow
        {
            EmployerUserId = employerUserId,
            State = state,
            CodeVerifier = codeVerifier,
            RedirectUri = redirectUri,
            ReturnUrl = safeReturnUrl,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };
        var protectedFlow = _flowProtector.Protect(
            JsonSerializer.Serialize(flow));
        Response.Cookies.Append(
            FlowCookieName,
            protectedFlow,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(10)
            });

        return Redirect(result.Data.AuthorizationUrl);
    }

    [AllowAnonymous]
    [HttpGet("/Employer/Outlook/Callback")]
    public async Task<IActionResult> Callback(
        string? code,
        string? state,
        string? error,
        string? errorDescription,
        CancellationToken cancellationToken)
    {
        var flow = ReadAndDeleteFlowCookie();
        var returnUrl = flow?.ReturnUrl ?? "/Employer/Vacancies";
        var expectedStateBytes = System.Text.Encoding.UTF8.GetBytes(
            flow?.State ?? string.Empty);
        var suppliedStateBytes = System.Text.Encoding.UTF8.GetBytes(
            state ?? string.Empty);
        if (flow is null
            || flow.ExpiresAtUtc < DateTime.UtcNow
            || string.IsNullOrWhiteSpace(state)
            || expectedStateBytes.Length != suppliedStateBytes.Length
            || !CryptographicOperations.FixedTimeEquals(
                expectedStateBytes,
                suppliedStateBytes))
        {
            TempData["CalendarErrorMessage"] =
                "Microsoft bağlantı sessiyası etibarsızdır və ya vaxtı bitib.";
            return LocalRedirect(returnUrl);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning(
                "Microsoft Calendar OAuth error {Error}: {Description}",
                error,
                errorDescription);
            TempData["CalendarErrorMessage"] =
                "Microsoft hesabı qoşulmadı və ya icazə ləğv edildi.";
            return LocalRedirect(returnUrl);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["CalendarErrorMessage"] = "Microsoft authorization code qaytarmadı.";
            return LocalRedirect(returnUrl);
        }

        var result = await _calendarApiService.CompleteConnectionAsync(
            new CompleteMicrosoftCalendarConnectionApiRequest
            {
                EmployerUserId = flow.EmployerUserId,
                Code = code,
                CodeVerifier = flow.CodeVerifier,
                RedirectUri = flow.RedirectUri
            },
            cancellationToken);
        if (!result.Success || result.Data?.IsConnected != true)
            TempData["CalendarErrorMessage"] = result.Message;
        else
            TempData["CalendarSuccessMessage"] = result.Data.Message;

        return LocalRedirect(returnUrl);
    }

    [HttpPost("/Employer/Outlook/Disconnect")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disconnect(
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
            return Challenge();

        var result = await _calendarApiService.DisconnectAsync(
            employerUserId,
            cancellationToken);
        TempData[result.Success
            ? "CalendarSuccessMessage"
            : "CalendarErrorMessage"] = result.Message;
        return LocalRedirect(NormalizeReturnUrl(returnUrl));
    }

    [HttpPost("/Employer/Outlook/Meetings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMeeting(
        int vacancyId,
        int applicationId,
        string? subject,
        string? agenda,
        DateTimeOffset startAtUtc,
        int durationMinutes,
        bool createTeamsMeeting,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
            return Challenge();

        var returnUrl = $"/Employer/Vacancies/{vacancyId}";
        var result = await _calendarApiService.CreateMeetingAsync(
            new CreateInterviewMeetingApiRequest
            {
                EmployerUserId = employerUserId,
                VacancyId = vacancyId,
                ApplicationId = applicationId,
                Subject = subject?.Trim() ?? string.Empty,
                Agenda = agenda?.Trim() ?? string.Empty,
                StartAtUtc = startAtUtc,
                DurationMinutes = durationMinutes,
                CreateTeamsMeeting = createTeamsMeeting
            },
            cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["CalendarErrorMessage"] = result.Message;
            return LocalRedirect(returnUrl);
        }

        TempData["CalendarSuccessMessage"] = result.Data.Message;
        TempData["CalendarMeetingWebLink"] = result.Data.WebLink;
        TempData["CalendarMeetingJoinUrl"] = result.Data.JoinUrl;
        return LocalRedirect(returnUrl);
    }

    [HttpPost("/Employer/Outlook/Availability")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Availability(
        [FromBody] InterviewAvailabilityBrowserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Employer session is not available."
            });
        }

        var result = await _calendarApiService.GetAvailabilityAsync(
            new InterviewAvailabilityApiRequest
            {
                EmployerUserId = employerUserId,
                VacancyId = request.VacancyId,
                ApplicationId = request.ApplicationId,
                RangeStartUtc = request.RangeStartUtc,
                RangeEndUtc = request.RangeEndUtc
            },
            cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(result.Data);
    }

    private CalendarOAuthFlow? ReadAndDeleteFlowCookie()
    {
        if (!Request.Cookies.TryGetValue(FlowCookieName, out var protectedFlow)
            || string.IsNullOrWhiteSpace(protectedFlow))
        {
            return null;
        }

        Response.Cookies.Delete(FlowCookieName);
        try
        {
            var json = _flowProtector.Unprotect(protectedFlow);
            return JsonSerializer.Deserialize<CalendarOAuthFlow>(json);
        }
        catch (Exception exception) when (
            exception is CryptographicException or JsonException)
        {
            _logger.LogWarning(exception, "Microsoft Calendar OAuth cookie oxunmadı.");
            return null;
        }
    }

    private string NormalizeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/Employer/Vacancies";

    private bool TryGetEmployerUserId(out int userId) =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId)
        && userId > 0;

    private sealed class CalendarOAuthFlow
    {
        public int EmployerUserId { get; set; }
        public string State { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
