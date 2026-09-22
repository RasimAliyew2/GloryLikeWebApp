using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GloryLikeWebApp.Controllers;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// Run from the WebApp directory. All backend data is local test data; no hosted API is contacted.
var source = new DirectoryInfo(Directory.GetCurrentDirectory());
while (source is not null && !File.Exists(Path.Combine(source.FullName, "GloryLikeWebApp.csproj"))) source = source.Parent;
var workspace = source?.FullName ?? throw new InvalidOperationException("Run from the WebApp repository.");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
    ApplicationName = typeof(StudentController).Assembly.GetName().Name,
    ContentRootPath = AppContext.BaseDirectory, WebRootPath = Path.Combine(workspace, "wwwroot") });
builder.WebHost.UseUrls("http://127.0.0.1:5267");
builder.Logging.ClearProviders();
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Warning);
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(StudentController).Assembly)
    .AddApplicationPart(typeof(FormPreviewController).Assembly)
    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new TestControllers())).AddControllersAsServices();
var api = new VacancyApiService(new HttpClient(new FixtureHandler()) { BaseAddress = new Uri("https://fixture.invalid/") }, NullLogger<VacancyApiService>.Instance);
builder.Services.AddSingleton<IVacancyApiService>(api);
builder.Services.AddTransient<StudentController>(_ => new StudentController(null!, new EmptyProfiles(), api));
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
    app.MapGet("/preview/filled", () => { FixtureHandler.Mode = "filled"; return Results.Redirect("/Student/Internships"); });
    app.MapGet("/preview/empty", () => { FixtureHandler.Mode = "empty"; return Results.Redirect("/Student/Internships"); });
}
await app.StartAsync();
using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri("http://127.0.0.1:5267") };
foreach (var (role, status) in new[] { ("student", 200), ("candidate", 403), ("employer", 403), ("anonymous", 401) }) {
    using var request = new HttpRequestMessage(HttpMethod.Get, "/Student/Internships"); request.Headers.Add("X-Preview-Role", role);
    using var response = await client.SendAsync(request);
    Check((int)response.StatusCode == status, $"{role}: expected {status}, got {(int)response.StatusCode}");
}
var empty = await client.GetStringAsync("/Student/Internships");
Check((await client.GetAsync("/css/student-home.css")).IsSuccessStatusCode && (await client.GetAsync("/js/opportunities-page.js")).IsSuccessStatusCode, "Preview must serve production CSS and JavaScript");
Check(empty.Contains("No internships yet") && !empty.Contains("data-opportunity-card"), "Fresh Student page must have no demo vacancies");
FixtureHandler.Mode = "failed";
var error = await client.GetStringAsync("/Student/Internships");
Check(error.Contains("role=\"alert\"") && !error.Contains("No internships yet"), "API outage must not look like empty data");
FixtureHandler.Mode = "filled";
var html = await client.GetStringAsync("/Student/Internships");
Check(html.Contains("Data Analyst Intern") && html.Contains("Design Intern") && !html.Contains("Employee Analyst"), "Student category separation failed");
Check(html.Contains("/Opportunities/11/Apply") && html.Contains("/Applications/12"), "Shared application links missing");
Check(html.Contains("data-bookmark") && html.Contains("data-toggle-opportunity") && html.Contains("data-score-filter"), "Shared opportunity controls missing");
var sql = await client.GetStringAsync("/Student/Internships?filter=sql");
Check(sql.Contains("Data Analyst Intern") && !sql.Contains("Design Intern"), "Skill filtering failed");
var search = await client.GetStringAsync("/Student/Internships?search=Design");
Check(!search.Contains("Data Analyst Intern") && search.Contains("Design Intern"), "Search filtering failed");
Check((await client.GetStringAsync("/Student/Internships?filter=unknown")).Contains("Data Analyst Intern"), "Unknown filter must fall back to All");
Check((await client.GetStringAsync("/Student/Internships?vacancyId=13")).Contains("No internships yet"), "Employee deep link leaked into Student feed");
using (var redirect = await client.GetAsync("/Opportunities?vacancyId=11"))
    Check(redirect.StatusCode == HttpStatusCode.Redirect && redirect.Headers.Location!.ToString().Contains("/Student/Internships?vacancyId=11"), "Old Student links must redirect");
using (var request = new HttpRequestMessage(HttpMethod.Get, "/Opportunities")) {
    request.Headers.Add("X-Preview-Role", "candidate"); using var response = await client.SendAsync(request);
    var employee = await response.Content.ReadAsStringAsync();
    Check(response.IsSuccessStatusCode && employee.Contains("Employee Analyst") && !employee.Contains("Data Analyst Intern"), "Employee opportunities must retain only Employee roles");
}
Check((await client.GetStringAsync("/Opportunities/11/Apply")).Contains("Data Analyst Intern"), "Student must reach existing screening flow");
foreach (var type in new[] { "Employee", "Internship" }) {
    var form = await client.GetStringAsync("/preview/employer?type=" + type);
    Check(form.Contains("name=\"Input.VacancyType\"") && form.Contains("value=\"" + type + "\" selected=\"selected\""), "Employer category selection not retained: " + type);
    using var post = await client.PostAsync("/preview/bind", new FormUrlEncodedContent(new Dictionary<string, string> { ["Input.VacancyType"] = type }));
    Check((await post.Content.ReadAsStringAsync()).Contains(type), "Employer category form binding failed");
}
using (var invalid = await client.PostAsync("/preview/bind", new FormUrlEncodedContent(new Dictionary<string, string> { ["Input.VacancyType"] = "Other" })))
    Check(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid employer category must fail validation");
Console.WriteLine("PASS: Student-only route, empty/error states, category isolation, filters/search, shared controls, screening access and employer form rendering/binding.");
FixtureHandler.Mode = "empty";
if (args.Contains("--preview")) { Console.WriteLine("Preview: http://127.0.0.1:5267/Student/Internships"); await app.WaitForShutdownAsync(); }
else await app.StopAsync();
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

public sealed class FormPreviewController : Controller {
    [HttpGet("/preview/employer")]
    public IActionResult Form(string type = "Employee") => View("~/Views/EmployerVacancies/CreateVacancy.cshtml", new CreateVacancyPageViewModel { Input = new() { VacancyType = type, EditingVacancyId = type == "Internship" ? 11 : null } });
    [HttpPost("/preview/bind")]
    public IActionResult Bind(CreateVacancyPageViewModel model) => ModelState.TryGetValue("Input.VacancyType", out var state) && state.Errors.Count > 0 ? BadRequest() : Json(new { model.Input.VacancyType });
}
sealed class TestControllers : IApplicationFeatureProvider<ControllerFeature> {
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        foreach (var c in feature.Controllers.Where(c => c.AsType() != typeof(StudentController) && c.AsType() != typeof(OpportunitiesController) && c.AsType() != typeof(FormPreviewController)).ToArray()) feature.Controllers.Remove(c);
    }
}
sealed class FixtureHandler : HttpMessageHandler {
    public static string Mode = "empty";
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
        if (Mode == "failed") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new CandidateVacancyListApiResponse {
            Success = true, CandidateJobFamilyIds = [10], CandidateJobFamilyNames = ["Data"], Vacancies = Mode == "empty" ? [] : [
                new() { VacancyId = 11, VacancyType = "Internship", RoleTitle = "Data Analyst Intern", EmployerName = "Example Academy", JobFamilyName = "Data", SeniorityName = "Junior", EmploymentType = "Full-time", LocationName = "Baku", MatchScore = 75, MinSalary = 600, Currency = "AZN", JobDescription = "Build reporting skills with a team of analysts.", Skills = [new() { SkillId = 1, SkillName = "SQL", Weight = 100, IsMatched = true }] },
                new() { VacancyId = 12, VacancyType = "Internship", RoleTitle = "Design Intern", EmployerName = "Example Studio", JobFamilyName = "Design", SeniorityName = "Entry", EmploymentType = "Part-time", LocationName = "Remote", MatchScore = 90, HasApplied = true, ApplicationStatus = "ScreeningPassed", Skills = [new() { SkillId = 2, SkillName = "Figma", Weight = 100 }] },
                new() { VacancyId = 13, VacancyType = "Employee", RoleTitle = "Employee Analyst", SeniorityName = "Junior", MatchScore = 95 }
            ] }) });
    }
}
sealed class EmptyProfiles : IUserProfileDataApiService {
    public Task<UserProfileDataApiResult> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(UserProfileDataApiResult.Ok(new UserProfileDataResponse { Success = true, Skills = [] }));
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
