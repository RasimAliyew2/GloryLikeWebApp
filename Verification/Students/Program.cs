using System.Security.Claims;
using GloryLikeWebApp.Controllers.Account;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Auth;
using GloryLikeWebApp.Models.Student;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

static void Check(bool value, string message) { if (!value) throw new Exception(message); }

foreach (var (type, expected) in new[] { ("student", "/Student"), ("candidate", "/dashboard"), ("employer", "/EmployerHome") })
{
    foreach (var method in new[] { "password", "verification", "google", "apple" })
    {
        var backend = new FakeBackend(type);
        var auth = new FakeAuthentication(method);
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider() };
        var context = new ControllerContext(new ActionContext(http, new RouteData(), new ControllerActionDescriptor()));
        var controller = new AccountController(backend, null!, new ConfigurationBuilder().Build()) { ControllerContext = context, Url = new UrlHelper(context) };
        IActionResult result = method switch
        {
            "password" => await controller.SignIn(new LoginViewModel { Login = "student@example.test", Password = "test-only-password", ReturnUrl = "/dashboard" }, default),
            "verification" => await controller.ConfirmRegistrationCode(new VerifyRegistrationViewModel { VerificationId = Guid.NewGuid(), Code = "123456", ReturnUrl = "/dashboard" }, default),
            _ => await controller.ExternalLoginCallback(default)
        };
        Check(result is LocalRedirectResult redirect && redirect.Url == expected, $"{method}/{type}: wrong destination");
        Check(auth.SignedIn?.FindFirstValue("accountType") == type, $"{method}/{type}: account type lost");
        Check(auth.SignedIn?.FindFirstValue(PortalClaimTypes.ClaimName) == AccountRouting.Portal(type), $"{method}/{type}: portal claim lost");
        if (method is "google" or "apple")
        {
            Check(backend.SocialRequest?.RegistrationAccountType == "student", "OAuth registration selection was lost");
            Check(backend.SocialRequest?.Provider == method, "OAuth provider was lost");
        }
    }
}
Check(!AccountRouting.IsSupported("admin"), "Unknown roles must not gain access");
Check(AccountRouting.HomePath(" STUDENT ") == "/Student", "Normalize Student role");

var model = new StudentDashboardViewModel { SkillsAvailable = true, ApplicationsAvailable = true, OpportunitiesAvailable = true,
    Education = new() { Success = true, University = "University", Specialty = "Computer Science", StudyYear = 2 } };
StudentDashboardBuilder.Populate(model,
[
    new() { SkillId = 1, SkillName = "SQL", IsVerified = true, CredibilityScore = 80 },
    new() { SkillId = 1, SkillName = "SQL", CredibilityScore = 90 },
    new() { SkillId = 2, SkillName = "Python", CredibilityScore = 90 },
    new() { SkillId = 3, SkillName = "Excel", Status = "absent", CredibilityScore = 90 }
],
[
    new() { VacancyId = 1, VacancyType = "Internship", EmploymentType = "Full-time", MatchScore = 75 },
    new() { VacancyId = 2, SeniorityName = "Junior", MatchScore = 95 },
    new() { VacancyId = 3, SeniorityName = "Senior", MatchScore = 100 },
    new() { VacancyId = 1, VacancyType = "Internship", EmploymentType = "Full-time", MatchScore = 75 }
],
[
    new() { VacancyId = 1, VacancyType = "Internship", EmploymentType = "Full-time", HiredAtUtc = DateTime.UtcNow },
    new() { VacancyId = 4, EmploymentType = "Full-time", HiredAtUtc = DateTime.UtcNow }
]);
Check(model.Ssi == 40 && model.Skills.Count == 3, "SSI must average distinct skill signals: (80 + 40 + 0) / 3");
Check(model.VerifiedSkills == 1, "Self-declared skills must not count as verified");
Check(model.Opportunities.Select(v => v.VacancyId).SequenceEqual(new[] { 1 }), "Only explicitly categorized internships belong in the Student feed; junior Employee roles stay out");
Check(model.InternshipApplications.Count == 1 && model.Achievements.Single(a => a.Name == "First internship").Earned, "Internship achievement requires hired internship");
foreach (var (score, level) in new[] { (0, "Getting started"), (41, "Building momentum"), (61, "Job-Ready"), (81, "Standout") })
{ model.Ssi = score; Check(model.Level == level, "Incorrect SSI milestone"); }
var empty = new StudentDashboardViewModel { SkillsAvailable = true, ApplicationsAvailable = true, OpportunitiesAvailable = true };
StudentDashboardBuilder.Populate(empty, [], [], []);
Check(empty.Ssi == 0 && empty.Opportunities.Count == 0 && empty.Achievements.All(a => !a.Earned), "Fresh Student must not contain demo data");
empty.SkillsAvailable = false;
Check(empty.SsiText == "—" && empty.Level == "Unavailable", "Failed skills load must not look like zero progress");
Check(StudentDashboardBuilder.RoundSignal(double.NaN) == 0 && StudentDashboardBuilder.RoundSignal(56.5) == 57, "Finite half-up rounding");
Console.WriteLine("PASS: 12 authentication flows, persisted roles, OAuth selection and Student dashboard rules.");

sealed class FakeBackend(string accountType) : IBackendAuthApiService
{
    private AuthUserDto User => new() { Id = 42, AccountType = accountType, Name = "Test", Surname = "Student", UserName = "test-student", Email = "student@example.test" };
    public SocialLoginRequestDto? SocialRequest { get; private set; }
    public Task<AuthResponseDto> LoginAsync(string login, string password, CancellationToken cancellationToken = default) => Task.FromResult(new AuthResponseDto { Success = true, User = User });
    public Task<AuthResponseDto> SocialLoginAsync(SocialLoginRequestDto request, CancellationToken cancellationToken = default)
    { SocialRequest = request; return Task.FromResult(new AuthResponseDto { Success = true, User = User }); }
    public Task<EmailRegistrationResponseDto> VerifyEmailRegistrationAsync(Guid id, string code, CancellationToken cancellationToken = default) => Task.FromResult(new EmailRegistrationResponseDto { Success = true, User = User });
    public Task<EmailRegistrationResponseDto> StartEmailRegistrationAsync(RegistrationViewModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<EmailRegistrationResponseDto> GetEmailRegistrationStatusAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<EmailRegistrationResponseDto> ResendEmailRegistrationCodeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
sealed class FakeAuthentication(string provider) : IAuthenticationService
{
    public ClaimsPrincipal? SignedIn { get; private set; }
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "external-subject"), new Claim(ClaimTypes.Email, "student@example.test"), new Claim("email_verified", "true") }, "external"));
        var properties = new AuthenticationProperties();
        properties.Items["LoginProvider"] = provider == "apple" ? "Apple" : "Google";
        properties.Items["RegistrationAccountType"] = "student";
        properties.Items["ReturnUrl"] = "/dashboard";
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, scheme!)));
    }
    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) { SignedIn = principal; return Task.CompletedTask; }
    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
}
