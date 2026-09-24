using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using GloryLikeWebApp.Controllers;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Student;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// Real MVC, Razor, antiforgery and API client; all persisted data is an isolated in-memory fixture.
var workspace = Directory.GetCurrentDirectory();
var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
    ApplicationName = typeof(StudentProfileController).Assembly.GetName().Name,
    ContentRootPath = AppContext.BaseDirectory, WebRootPath = Path.Combine(workspace, "wwwroot") });
builder.WebHost.UseUrls("http://127.0.0.1:5273");
builder.Logging.ClearProviders(); builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Warning);
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(StudentProfileController).Assembly)
    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new TestedControllers()));
var fixture = new ProfileFixture(); var skills = new SkillsFixture();
var apiClient = new HttpClient(fixture) { BaseAddress = new Uri("https://fixture.invalid/") };
builder.Services.AddSingleton(new StudentProfileApiService(apiClient, NullLogger<StudentProfileApiService>.Instance));
builder.Services.AddSingleton<IUserProfileDataApiService>(skills);
builder.Services.AddSingleton<IUserPersonalProfileApiService>(new PersonalFixture());
builder.Services.AddSingleton<IEmployerCandidateMessagingApiService>(new EmployerCandidateMessagingApiService(apiClient, NullLogger<EmployerCandidateMessagingApiService>.Instance));
builder.Services.AddAuthentication("Preview")
    .AddScheme<AuthenticationSchemeOptions, PreviewAuthentication>("Preview", _ => { })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
builder.Services.AddAuthorization(options => {
    options.AddPolicy(PortalClaimTypes.StudentPolicy, policy => policy.RequireAuthenticatedUser()
        .RequireClaim(PortalClaimTypes.ClaimName, PortalClaimTypes.Student).RequireClaim("accountType", "student"));
    options.AddPolicy(PortalClaimTypes.EmployerPolicy, policy => policy.RequireAuthenticatedUser().RequireClaim("accountType", "employer"));
});
var app = builder.Build();
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/Candidate/Notifications", () => Results.Ok(new { success = true, notifications = Array.Empty<object>() }));
app.MapGet("/preview/claims", async (HttpContext context) => {
    var auth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Json(new { name = auth.Principal?.FindFirstValue(ClaimTypes.Name), surname = auth.Principal?.FindFirstValue(ClaimTypes.Surname) });
});
app.MapControllers();
if (args.Contains("--preview")) {
    app.MapGet("/preview/error", () => { fixture.Failed = true; return Results.Redirect("/Student/Profile"); });
    app.MapGet("/preview/filled", () => { fixture.Seed(); return Results.Redirect("/Student/Profile"); });
}
await app.StartAsync();
using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true }) { BaseAddress = new Uri("http://127.0.0.1:5273") };
foreach (var (role, status) in new[] { ("student", 200), ("candidate", 403), ("employer", 403), ("anonymous", 401) }) {
    using var request = new HttpRequestMessage(HttpMethod.Get, "/Student/Profile"); request.Headers.Add("X-Preview-Role", role);
    using var response = await client.SendAsync(request);
    Check((int)response.StatusCode == status, $"Profile authorization for {role}: {(int)response.StatusCode}");
    if (role != "student") {
        using var post = new HttpRequestMessage(HttpMethod.Post, "/Student/Profile") { Content = new FormUrlEncodedContent(Fields()) };
        post.Headers.Add("X-Preview-Role", role);
        using var rejected = await client.SendAsync(post);
        Check((int)rejected.StatusCode == status, "Unauthorized save must be rejected");
    }
}
using (var response = await client.GetAsync("/Profile"))
    Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location?.ToString() == "/Student/Profile", "Legacy student profile redirect");
foreach (var role in new[] { "candidate", "employer" }) {
    using var request = new HttpRequestMessage(HttpMethod.Get, "/Profile"); request.Headers.Add("X-Preview-Role", role);
    using var response = await client.SendAsync(request);
    Check(response.IsSuccessStatusCode && !(await response.Content.ReadAsStringAsync()).Contains("studentProfileForm"), role + " retains existing profile");
}
var html = await client.GetStringAsync("/Student/Profile?userId=999");
Check(fixture.LastUserId == 42 && skills.LastUserId == 42, "Identity must come from authenticated claims");
Check(html.Contains("name=\"Input.FirstName\"") && html.Contains("value=\"Leyla\"") && html.Contains("href=\"/Student/Profile\" aria-current=\"page\""), "Bound fields and active student navigation");
Check(html.Contains("SQL") && html.Contains("Knowledge score 84 out of 100") && !html.Contains("Unverified Skill"), "Only verified skills and saved knowledge scores are shown");
Check(!html.Contains("work experience", StringComparison.OrdinalIgnoreCase), "Student profile must not require work experience");
using (var response = await client.PostAsync("/Student/Profile", new FormUrlEncodedContent(Fields())))
    Check(response.StatusCode == HttpStatusCode.BadRequest && fixture.Saves == 0, "Save requires antiforgery");
using (var response = await PostForm("/Profile", Fields()))
    Check(response.StatusCode == HttpStatusCode.Forbidden, "Students cannot update through candidate profile endpoint");
var invalid = Fields(); invalid["Input.FirstName"] = ""; invalid["Input.Goal"] = "Keep my unsaved goal";
using (var response = await PostForm("/Student/Profile", invalid)) {
    var body = await response.Content.ReadAsStringAsync();
    Check(response.IsSuccessStatusCode && fixture.Saves == 0 && body.Contains("Keep my unsaved goal") && body.Contains("First name"), "Invalid form retains draft without writing API");
}
var oversized = Fields(); oversized["Input.FirstName"] = new string('a', 81);
using (var response = await PostForm("/Student/Profile", oversized)) Check(fixture.Saves == 0, "Names match database length validation");
var valid = Fields(); valid["userId"] = "999"; valid["Input.FirstName"] = "Aysel"; valid["Input.OpenToInternships"] = "false";
valid["Input.Ssi"] = "100"; valid["Input.KnowledgeScore"] = "100";
using (var response = await PostForm("/Student/Profile", valid))
    Check(response.StatusCode == HttpStatusCode.Redirect && fixture.Saves == 1 && fixture.LastUserId == 42, "Valid save targets authenticated student");
Check(fixture.Data.FirstName == "Aysel" && !fixture.Data.OpenToInternships && fixture.Data.GraduationYear == 2028, "All student profile fields persist");
html = await client.GetStringAsync("/Student/Profile");
Check(html.Contains("Your profile has been saved.") && html.Contains("value=\"Aysel\"") && html.Contains("Knowledge score 84 out of 100"), "Saved details reload and score cannot be overwritten");
Check((await client.GetStringAsync("/preview/claims")).Contains("Aysel"), "Updated name must refresh authentication cookie");
Check(!(await client.GetStringAsync("/Student/Profile")).Contains("Your profile has been saved."), "Save notice only follows successful save");
fixture.ValidationError = true;
using (var response = await PostForm("/Student/Profile", valid))
    Check((await response.Content.ReadAsStringAsync()).Contains("Choose a valid JPG or PNG photo."), "Backend validation reaches form");
fixture.ValidationError = false;
fixture.Failed = true;
html = await client.GetStringAsync("/Student/Profile");
Check(html.Contains("Your profile is temporarily unavailable") && !html.Contains("data-profile-completion-label") && !html.Contains("SQL password"), "Outage does not fake profile data or leak upstream error");
// Post directly with the already fetched form token to model an outage between loading and saving.
fixture.Failed = false; var draftToken = Token(await client.GetStringAsync("/Student/Profile")); fixture.Failed = true;
var draft = Fields(); draft["__RequestVerificationToken"] = draftToken; draft["Input.About"] = "My draft survives the outage";
using (var response = await client.PostAsync("/Student/Profile", new FormUrlEncodedContent(draft)))
    Check((await response.Content.ReadAsStringAsync()).Contains("My draft survives the outage"), "Outage during save retains editable draft");
fixture.Failed = false; skills.Failed = true;
html = await client.GetStringAsync("/Student/Profile");
Check(html.Contains("Your skills are temporarily unavailable") && html.Contains("name=\"Input.FirstName\""), "Skills outage does not prevent editing personal details");
skills.Failed = false;
foreach (var student in new[] { true, false }) {
    fixture.EmployerStudent = student;
    using var request = new HttpRequestMessage(HttpMethod.Get, "/Employer/Candidates/42"); request.Headers.Add("X-Preview-Role", "employer");
    using var response = await client.SendAsync(request); var body = await response.Content.ReadAsStringAsync();
    Check(response.IsSuccessStatusCode, "Employer profile renders");
    Check(student ? body.Contains("STUDENT PROFILE") && body.Contains("Expected graduation: 2028") && body.Contains("Internship goal") && !body.Contains("No work experience")
        : body.Contains("CANDIDATE PROFILE") && body.Contains("No work experience"), "Employer sees student education; candidate career view remains unchanged");
}
fixture.Seed();
Console.WriteLine("PASS: student role isolation, identity, CSRF, validation/draft retention, save/reload, name cookie refresh, verified scores, outage states, legacy candidate/employer compatibility and student employer profile.");
if (args.Contains("--preview")) { Console.WriteLine("Preview http://127.0.0.1:5273/Student/Profile"); await app.WaitForShutdownAsync(); }
else await app.StopAsync();

async Task<HttpResponseMessage> PostForm(string path, Dictionary<string,string> fields) {
    fields["__RequestVerificationToken"] = Token(await client.GetStringAsync("/Student/Profile"));
    return await client.PostAsync(path, new FormUrlEncodedContent(fields));
}
static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
static Dictionary<string,string> Fields() => new() {
    ["Input.FirstName"] = "Leyla", ["Input.LastName"] = "Ahmadova", ["Input.University"] = "Baku Engineering University",
    ["Input.Specialty"] = "Information Technology", ["Input.StudyYear"] = "3", ["Input.GraduationYear"] = "2028",
    ["Input.About"] = "Learning through data projects.", ["Input.Goal"] = "Find a data analysis internship", ["Input.OpenToInternships"] = "true" };
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

sealed class TestedControllers : IApplicationFeatureProvider<ControllerFeature> {
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        var allowed = new[] { typeof(StudentProfileController), typeof(ProfileController), typeof(EmployerCandidatesController) };
        foreach (var controller in feature.Controllers.Where(controller => !allowed.Contains(controller.AsType())).ToArray()) feature.Controllers.Remove(controller);
    }
}
sealed class SkillsFixture : IUserProfileDataApiService {
    public bool Failed; public int LastUserId;
    public Task<UserProfileDataApiResult> GetAsync(int id, CancellationToken ct = default) {
        LastUserId = id;
        return Task.FromResult(Failed ? UserProfileDataApiResult.Fail("Fixture unavailable") : UserProfileDataApiResult.Ok(new UserProfileDataResponse { Success = true, Skills = [
            new() { SkillId = 1, SkillName = "SQL", IsVerified = true, Status = "verified", KnowledgeScore = 84, CredibilityScore = 84 },
            new() { SkillId = 2, SkillName = "Unverified Skill", Status = "self_declared", KnowledgeScore = 0, CredibilityScore = 0 }] }));
    }
    public Task<UserProfileDataApiResult> SaveAsync(int id, UserJobInfo? job, IReadOnlyCollection<UserSkillInfo> skills, IReadOnlyCollection<UserWorkExperienceInfo> experiences, CancellationToken ct = default) => throw new NotSupportedException();
}
sealed class PersonalFixture : IUserPersonalProfileApiService {
    public Task<UserPersonalProfileApiResult> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(UserPersonalProfileApiResult.Ok(new() { Success = true, FirstName = "Existing", LastName = "User" }));
    public Task<UserPersonalProfileApiResult> UpdateAsync(int id, UserPersonalProfileInput input, CancellationToken ct = default) => throw new Exception("Student cannot write legacy personal endpoint.");
}
sealed class PreviewAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        var role = Request.Headers["X-Preview-Role"].FirstOrDefault() ?? "student";
        if (role == "anonymous") return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Name, "Leyla"), new Claim(ClaimTypes.Surname, "Ahmadova"), new Claim("accountType", role), new Claim(PortalClaimTypes.ClaimName, AccountRouting.Portal(role)) }, "Preview"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Preview")));
    }
}
sealed class ProfileFixture : HttpMessageHandler {
    public StudentProfileResponse Data { get; private set; } = new();
    public bool Failed, ValidationError, EmployerStudent = true; public int LastUserId, Saves;
    public ProfileFixture() => Seed();
    public void Seed() { Failed = false; ValidationError = false; Data = new() { Success = true, FirstName = "Leyla", LastName = "Ahmadova", DisplayName = "Leyla Ahmadova", University = "Baku Engineering University", Specialty = "Information Technology", StudyYear = 3, GraduationYear = 2028, About = "Third-year student exploring data analysis with SQL and Python. I enjoy turning class projects into useful tools.", Goal = "Find a data analysis internship", OpenToInternships = true }; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
        var path = request.RequestUri!.AbsolutePath;
        if (path.Contains("/candidate-profiles/") || path.Contains("/candidates/")) return new(HttpStatusCode.OK) { Content = JsonContent.Create(new EmployerCandidateProfileApiResponse { Success = true, Candidate = new() {
            UserId = 42, DisplayName = Data.DisplayName, AccountType = EmployerStudent ? "student" : "candidate", University = Data.University, Specialty = Data.Specialty, StudyYear = 3, GraduationYear = 2028, Goal = Data.Goal ?? "", About = Data.About ?? "", OpenToInternships = Data.OpenToInternships,
            Skills = [new() { SkillId = 1, SkillName = "SQL", IsVerified = true, KnowledgeScore = 84, CredibilityScore = 84 }] } }) };
        var match = Regex.Match(path, "^/api/students/(\\d+)/profile(?:/details)?$");
        if (!match.Success) throw new InvalidOperationException("Unexpected API path: " + path);
        LastUserId = int.Parse(match.Groups[1].Value);
        if (Failed) return new(HttpStatusCode.InternalServerError) { Content = new StringContent("SQL password private details") };
        if (request.Method == HttpMethod.Put) {
            if (ValidationError) return new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new StudentProfileResponse { Message = "Choose a valid JPG or PNG photo." }) };
            var input = await request.Content!.ReadFromJsonAsync<StudentProfileInput>(cancellationToken: ct) ?? throw new Exception("Missing input");
            Saves++;
            Data = new() { Success = true, FirstName = input.FirstName, LastName = input.LastName, DisplayName = input.FirstName + " " + input.LastName,
                University = input.University, Specialty = input.Specialty, StudyYear = input.StudyYear, GraduationYear = input.GraduationYear,
                About = input.About, Goal = input.Goal, OpenToInternships = input.OpenToInternships, ProfileImageDataUrl = input.ProfileImageDataUrl };
        }
        return new(HttpStatusCode.OK) { Content = JsonContent.Create(Data) };
    }
}
