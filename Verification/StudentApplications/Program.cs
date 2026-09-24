using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using GloryLikeWebApp.Controllers;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Student;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// Real controllers, Razor and API client against local fixtures only; no hosted data is accessed.
var workspace = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(workspace, "GloryLikeWebApp.csproj"))) throw new Exception("Run from the WebApp directory.");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
    ApplicationName = typeof(StudentApplicationsController).Assembly.GetName().Name,
    ContentRootPath = AppContext.BaseDirectory, WebRootPath = Path.Combine(workspace, "wwwroot") });
builder.WebHost.UseUrls("http://127.0.0.1:5272");
builder.Logging.ClearProviders(); builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Warning);
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(StudentApplicationsController).Assembly)
    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new OnlyTestedControllers()));
var fixture = new ApplicationsFixture();
var profiles = new ProfilesFixture();
builder.Services.AddSingleton<IUserProfileDataApiService>(profiles);
builder.Services.AddSingleton<IVacancyApiService>(new VacancyApiService(
    new HttpClient(fixture) { BaseAddress = new Uri("https://fixture.invalid/") }, NullLogger<VacancyApiService>.Instance));
builder.Services.AddAuthentication("Preview").AddScheme<AuthenticationSchemeOptions, PreviewAuthentication>("Preview", _ => { });
builder.Services.AddAuthorization(options => {
    options.AddPolicy(PortalClaimTypes.StudentPolicy, policy => policy.RequireAuthenticatedUser()
        .RequireClaim(PortalClaimTypes.ClaimName, PortalClaimTypes.Student).RequireClaim("accountType", "student"));
    options.AddPolicy(PortalClaimTypes.EmployeePolicy, policy => policy.RequireAuthenticatedUser().RequireClaim("accountType", "student", "candidate"));
});
var app = builder.Build();
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/Candidate/Notifications", () => Results.Ok(new { success = true, notifications = Array.Empty<object>() }));
app.MapControllers();
if (args.Contains("--preview")) {
    app.MapGet("/preview/filled", () => { fixture.Seed(); return Results.Redirect("/Student/Applications"); });
    app.MapGet("/preview/empty", () => { fixture.Items.Clear(); fixture.Failed = false; return Results.Redirect("/Student/Applications"); });
    app.MapGet("/preview/error", () => { fixture.Failed = true; return Results.Redirect("/Student/Applications"); });
}
await app.StartAsync();
using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri("http://127.0.0.1:5272") };
foreach (var (role, status) in new[] { ("student", 200), ("candidate", 403), ("employer", 403), ("anonymous", 401) }) {
    foreach (var path in new[] { "/Student/Applications", "/Student/Applications/11" }) {
        using var request = new HttpRequestMessage(HttpMethod.Get, path); request.Headers.Add("X-Preview-Role", role);
        using var response = await client.SendAsync(request);
        Check((int)response.StatusCode == status, $"{path} authorization: {role} expected {status}");
    }
}
foreach (var (oldPath, newPath) in new[] { ("/Applications", "/Student/Applications"), ("/Applications?vacancyId=11", "/Student/Applications?vacancyId=11"), ("/Applications/11", "/Student/Applications/11") }) {
    using var response = await client.GetAsync(oldPath);
    Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location?.ToString() == newPath, "Legacy student link must redirect: " + oldPath);
}
using (var request = new HttpRequestMessage(HttpMethod.Get, "/Applications")) {
    request.Headers.Add("X-Preview-Role", "candidate"); using var response = await client.SendAsync(request);
    Check(response.IsSuccessStatusCode && (await response.Content.ReadAsStringAsync()).Contains("YOUR CANDIDATE JOURNEY"), "Candidate Applications page must remain available");
}
using (var response = await client.GetAsync("/Student/Applications?userId=999&candidateUserId=999")) {
    var html = await response.Content.ReadAsStringAsync();
    Check(fixture.LastUserId == 42 && profiles.LastUserId == 42, "Application requests must use the authenticated student identity");
    Check(response.Headers.CacheControl?.NoStore == true, "Current status pages must not be cached");
    Check(Regex.Matches(html, "data-vacancy-id=").Count == 6, "All submitted applications must be rendered");
    Check(html.Contains("Closed Research Role") && html.Contains("Reclassified Role") && html.Contains("Screening not passed") && html.Contains("Interview"), "Closed/reclassified history and correct screening/stage status must remain visible");
    Check(html.IndexOf("data-vacancy-id=\"16\"") < html.IndexOf("data-vacancy-id=\"11\""), "Latest applications must appear first");
    Check(html.Contains("href=\"/Student/Applications\" aria-current=\"page\""), "Applications must be active in student navigation");
}
foreach (var (filter, count, vacancy) in new[] { ("in-progress", 3, 11), ("hired", 1, 13), ("not-selected", 1, 12), ("withdrawn", 1, 16) }) {
    var html = await client.GetStringAsync("/Student/Applications?status=" + filter);
    Check(Regex.Matches(html, "data-vacancy-id=").Count == count && html.Contains($"data-vacancy-id=\"{vacancy}\""), "Incorrect status filter: " + filter);
}
Check((await client.GetStringAsync("/Student/Applications?search=Research")).Contains("Closed Research Role"), "Search must include archived roles");
Check((await client.GetStringAsync("/Student/Applications?status=unknown")).Contains("data-vacancy-id=\"11\""), "Unknown filters fall back to all");
Check((await client.GetStringAsync("/Student/Applications?search=missing")).Contains("No matching applications"), "Filtered empty state must be clear");
Check((await client.GetStringAsync("/Student/Applications?vacancyId=11")).Contains("student-application-card highlighted"), "Notification target must be highlighted");
var details = await client.GetStringAsync("/Student/Applications/13");
Check(details.Contains("Closed Research Role") && details.Contains("Hired") && details.Contains("600") && details.Contains("SQL") && details.Contains("UTC"), "Details must show archived vacancy, saved status, compensation, skills and dates");
using (var response = await client.GetAsync("/Student/Applications/999"))
    Check(response.StatusCode == HttpStatusCode.NotFound && (await response.Content.ReadAsStringAsync()).Contains("not found in your application history"), "Unknown or another student's vacancy must not expose details");
fixture.Items.Single(item => item.VacancyId == 11).FunnelStageName = "Final interview";
Check((await client.GetStringAsync("/Student/Applications/11")).Contains("Final interview"), "Employer updates must appear on the next read");
profiles.Failed = true;
Check((await client.GetStringAsync("/Student/Applications")).Contains("Final interview"), "SSI outage must not hide application history");
profiles.Failed = false;
fixture.Failed = true;
var failed = await client.GetStringAsync("/Student/Applications");
Check(failed.Contains("Applications could not be loaded") && !failed.Contains("No applications yet") && !failed.Contains("Total applications</dt><dd>0"), "API outage must not masquerade as no applications");
fixture.Failed = false; fixture.Items.Clear();
var empty = await client.GetStringAsync("/Student/Applications");
Check(empty.Contains("No applications yet") && !empty.Contains("data-vacancy-id=") && empty.Contains("/Student/Internships"), "Empty history must contain no demo applications");
Console.WriteLine("PASS: Student-only history/details, authenticated identity, candidate compatibility, legacy links, truthful statuses, archived history, filters/search, refreshed employer stages and empty/error states.");
fixture.Seed();
if (args.Contains("--preview")) { Console.WriteLine("Preview http://127.0.0.1:5272/Student/Applications"); await app.WaitForShutdownAsync(); }
else await app.StopAsync();
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

sealed class OnlyTestedControllers : IApplicationFeatureProvider<ControllerFeature> {
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        foreach (var controller in feature.Controllers.Where(controller => controller.AsType() != typeof(StudentApplicationsController) && controller.AsType() != typeof(ApplicationsController)).ToArray()) feature.Controllers.Remove(controller);
    }
}
sealed class ProfilesFixture : IUserProfileDataApiService {
    public bool Failed; public int LastUserId;
    public Task<UserProfileDataApiResult> GetAsync(int id, CancellationToken ct = default) {
        LastUserId = id;
        return Task.FromResult(Failed ? UserProfileDataApiResult.Fail("Fixture unavailable") : UserProfileDataApiResult.Ok(new UserProfileDataResponse { Success = true, Skills = [] }));
    }
    public Task<UserProfileDataApiResult> SaveAsync(int id, UserJobInfo? job, IReadOnlyCollection<UserSkillInfo> skills, IReadOnlyCollection<UserWorkExperienceInfo> experiences, CancellationToken ct = default) => throw new NotSupportedException();
}
sealed class PreviewAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        var role = Request.Headers["X-Preview-Role"].FirstOrDefault() ?? "student";
        if (role == "anonymous") return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Name, "Student Preview"), new Claim("accountType", role), new Claim(PortalClaimTypes.ClaimName, AccountRouting.Portal(role)) }, "Preview"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Preview")));
    }
}
sealed class ApplicationsFixture : HttpMessageHandler {
    public List<CandidateApplicationApiItem> Items { get; } = [];
    public bool Failed; public int LastUserId;
    public ApplicationsFixture() => Seed();
    public void Seed() {
        Items.Clear(); Failed = false;
        for (var id = 11; id <= 16; id++) Items.Add(new() {
            ApplicationId = id + 100, VacancyId = id, CompanyOwnerUserId = 10, PlatformVacancyId = "TEST-" + id,
            VacancyType = id == 14 ? "Employee" : "Internship", RoleTitle = id switch { 11 => "Data Analyst Intern", 12 => "Marketing Intern", 13 => "Closed Research Role", 14 => "Reclassified Role", 15 => "Design Intern", _ => "Support Intern" },
            CompanyName = "Example Academy", LocationName = "Baku", EmploymentType = "Part-time", JobFamilyName = "Data",
            JobDescription = "Learn through practical team projects.", MinSalary = 600, Currency = "AZN", VacancyStatus = id is 13 or 14 ? "Closed" : "Published",
            ApplicationStatus = id == 12 ? "ScreeningFailed" : id == 16 ? "Withdrawn" : "ScreeningPassed",
            FunnelStageName = id == 11 ? "Interview" : id == 13 ? "Hired" : id == 16 ? "Withdrawn" : "Applied",
            FunnelStageIndex = id == 13 ? 3 : id == 11 ? 2 : 1, FunnelStageCount = 3,
            AppliedAtUtc = new DateTime(2026, 9, id, 10, 0, 0, DateTimeKind.Utc), FunnelStageUpdatedAtUtc = new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc),
            HiredAtUtc = id == 13 ? new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc) : null,
            Skills = [new() { SkillId = 1, SkillName = "SQL", RequirementType = "Required", Weight = 70 }]
        });
    }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
        var match = Regex.Match(request.RequestUri!.AbsolutePath, "^/api/Vacancies/candidate/(\\d+)/applications$");
        if (!match.Success) throw new InvalidOperationException("Unexpected API route: " + request.RequestUri.AbsolutePath);
        LastUserId = int.Parse(match.Groups[1].Value);
        return Task.FromResult(Failed ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new(HttpStatusCode.OK) { Content = JsonContent.Create(new CandidateApplicationListApiResponse { Success = true, CandidateUserId = LastUserId, Applications = Items }) });
    }
}
