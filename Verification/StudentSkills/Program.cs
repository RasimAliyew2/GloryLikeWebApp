using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using GloryLikeWebApp.Controllers;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// Local, stateful API fixture: never calls a hosted API, database or AI provider.
var source = new DirectoryInfo(Directory.GetCurrentDirectory());
while (source is not null && !File.Exists(Path.Combine(source.FullName, "GloryLikeWebApp.csproj"))) source = source.Parent;
var workspace = source?.FullName ?? throw new InvalidOperationException("Run from the WebApp repository.");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
    ApplicationName = typeof(SkillsController).Assembly.GetName().Name,
    ContentRootPath = AppContext.BaseDirectory, WebRootPath = Path.Combine(workspace, "wwwroot") });
builder.WebHost.UseUrls("http://127.0.0.1:5271");
builder.Logging.ClearProviders();
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Warning);
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SkillsController).Assembly)
    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new TestControllers()));
var fixture = new StudentSkillsFixture();
var taxonomy = new TaxonomyFixture();
var fixtureClient = new HttpClient(fixture) { BaseAddress = new Uri("https://fixture.invalid/") };
builder.Services.AddSingleton(new StudentSkillsApiService(fixtureClient, NullLogger<StudentSkillsApiService>.Instance));
builder.Services.AddSingleton<ISkillAndJobApiService>(taxonomy);
builder.Services.AddSingleton<IUserProfileDataApiService>(new EmptyProfiles());
builder.Services.AddSingleton<ISkillAssessmentApiService>(new CandidateAssessmentGuard());
builder.Services.AddAuthentication("Preview").AddScheme<AuthenticationSchemeOptions, PreviewAuthentication>("Preview", _ => { });
builder.Services.AddAuthorization(options => {
    options.AddPolicy(PortalClaimTypes.StudentPolicy, policy => policy.RequireAuthenticatedUser()
        .RequireClaim(PortalClaimTypes.ClaimName, PortalClaimTypes.Student).RequireClaim("accountType", "student"));
    options.AddPolicy(PortalClaimTypes.EmployeePolicy, policy => policy.RequireAuthenticatedUser()
        .RequireClaim("accountType", "student", "candidate"));
});
var app = builder.Build();
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/Candidate/Notifications", () => Results.Ok(new { success = true, notifications = Array.Empty<object>() }));
app.MapControllers();
if (args.Contains("--preview")) {
    app.MapGet("/preview/empty", () => { fixture.Reset(); return Results.Redirect("/Student/Skills"); });
    app.MapGet("/preview/filled", () => { fixture.Seed(); return Results.Redirect("/Student/Skills"); });
    app.MapGet("/preview/error", () => { fixture.Failure = "list"; return Results.Redirect("/Student/Skills"); });
    app.MapGet("/preview/quiz-error", () => { fixture.Failure = "generate"; return Results.Ok("Quiz generation is now unavailable in this local fixture."); });
    app.MapGet("/preview/recover", () => { fixture.Failure = null; return Results.Ok("The local skill fixture is available again."); });
}
await app.StartAsync();
using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true }) {
    BaseAddress = new Uri("http://127.0.0.1:5271") };

foreach (var (role, status) in new[] { ("student", 200), ("candidate", 403), ("employer", 403), ("anonymous", 401) }) {
    using var request = new HttpRequestMessage(HttpMethod.Get, "/Student/Skills"); request.Headers.Add("X-Preview-Role", role);
    using var response = await client.SendAsync(request);
    Check((int)response.StatusCode == status, $"Student page authorization: {role} expected {status}, got {(int)response.StatusCode}");
}
using (var response = await client.GetAsync("/Skills"))
    Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString() == "/Student/Skills", "Old student Skills URL must redirect to the student page");
using (var request = new HttpRequestMessage(HttpMethod.Get, "/Skills")) {
    request.Headers.Add("X-Preview-Role", "candidate");
    using var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();
    Check(response.IsSuccessStatusCode && body.Contains("/Skills/AddJob") && body.Contains("/Skills/AddExperience"), "Candidate Skills page must retain career and work experience controls");
}
foreach (var route in new[] { "/Skills/AddJob", "/Skills/AddSkill", "/Skills/AddExperience" }) {
    using var response = await PostForm(client, route, new());
    Check(response.StatusCode == HttpStatusCode.Forbidden, "Students must not write through legacy candidate form " + route);
}
foreach (var route in new[] { "/Skills/Assessment/Generate", "/Skills/Assessment/Submit" }) {
    using var response = await PostJson(client, route, new { skillId = 1, skillName = "SQL", language = "en" });
    Check(response.StatusCode == HttpStatusCode.Forbidden, "Students must not use the candidate experience assessment " + route);
}
foreach (var role in new[] { "candidate", "employer", "anonymous" }) {
    foreach (var route in new[] { "/Student/Skills/Add", "/Student/Skills/Remove", "/Student/Skills/Assessment/Generate", "/Student/Skills/Assessment/Submit" }) {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(new { skillId = 1, skillName = "SQL" }) };
        request.Headers.Add("X-Preview-Role", role);
        using var response = await client.SendAsync(request);
        Check((int)response.StatusCode == (role == "anonymous" ? 401 : 403), "Student mutation authorization failed: " + role + " " + route);
    }
}

var empty = await client.GetStringAsync("/Student/Skills");
Check(empty.Contains("Add a skill") && empty.Contains("My Skills") && empty.Contains("Total Skills") && empty.Contains("Average Knowledge Score"), "Student Skills overview and add form must render");
Check(empty.Contains("SQL") && empty.Contains("Excel") && empty.Contains("Python"), "Students without jobs must receive the skill taxonomy");
Check(!empty.Contains("/Skills/AddJob") && !empty.Contains("/Skills/AddExperience") && !empty.Contains("name=\"AddExperience."), "Student page must not ask for employment or work history");
Check((await client.GetAsync("/css/student-skills.css")).IsSuccessStatusCode && (await client.GetAsync("/js/student-skills.js")).IsSuccessStatusCode, "Preview must serve production Skills assets");

var beforeInvalid = fixture.Requests.Count;
using (var noToken = await client.PostAsync("/Student/Skills/Add", new FormUrlEncodedContent(new Dictionary<string, string> { ["SkillId"] = "1" })))
    Check(noToken.StatusCode == HttpStatusCode.BadRequest && fixture.Requests.Count == beforeInvalid, "Add must enforce anti-forgery before contacting the API");

var added = await PostForm(client, "/Student/Skills/Add", new() { ["SkillId"] = "1", ["UserId"] = "999", ["JobFamilyId"] = "0" });
Check(added.StatusCode == HttpStatusCode.Redirect, "Add should redirect after saving");
added.Dispose();
Check(fixture.Skills.Single().SkillId == 1 && fixture.Skills.Single().KnowledgeScore == 0 && !fixture.Skills.Single().IsVerified, "Added skill starts unverified without requiring a job or work experience");
var afterAdd = await client.GetStringAsync("/Student/Skills");
Check(afterAdd.Contains("data-auto-skill-id=\"1\"") && afterAdd.Contains("data-auto-skill-name=\"SQL\""), "Adding a skill must launch its student quiz on redirect");

using (var forbidden = new HttpRequestMessage(HttpMethod.Post, "/Student/Skills/Assessment/Generate")) {
    forbidden.Headers.Add("X-Preview-Role", "candidate");
    forbidden.Content = JsonContent.Create(new { skillId = 1, skillName = "SQL", language = "en" });
    var count = fixture.Requests.Count;
    using var response = await client.SendAsync(forbidden);
    Check(response.StatusCode == HttpStatusCode.Forbidden && fixture.Requests.Count == count, "Candidate must not call student quiz endpoints");
}
using (var missingToken = await client.PostAsJsonAsync("/Student/Skills/Assessment/Generate", new { skillId = 1, skillName = "SQL", language = "en" }))
    Check(missingToken.StatusCode == HttpStatusCode.BadRequest, "Quiz generation must enforce anti-forgery");

using var generatedResponse = await PostJson(client, "/Student/Skills/Assessment/Generate", new { skillId = 1, skillName = "Forged name", language = "en", userId = 999 });
Check(generatedResponse.IsSuccessStatusCode, "Student quiz generation failed");
using var generated = JsonDocument.Parse(await generatedResponse.Content.ReadAsStringAsync());
Check(generated.RootElement.GetProperty("success").GetBoolean(), "Generation must report success");
var questionnaire = generated.RootElement.GetProperty("questionnaire");
Check(questionnaire.GetProperty("skillName").GetString() == "SQL" && questionnaire.GetProperty("questions").GetArrayLength() == 2, "Quiz must use the saved student skill and expose question choices");
Check(!questionnaire.ToString().Contains("correctOption") && !questionnaire.ToString().Contains("weights"), "Quiz payload must not include answers or score weights");
Check(fixture.LastLanguage == "en", "Selected quiz language must reach the student API");

using var submitResponse = await PostJson(client, "/Student/Skills/Assessment/Submit", new {
    skillId = 1, skillName = "SQL", questionnaireId = fixture.QuestionnaireId, userId = 999,
    answers = new[] { new { questionId = "q1", selectedOptionIds = new[] { "b" } }, new { questionId = "q2", selectedOptionIds = new[] { "a" } } }
});
Check(submitResponse.IsSuccessStatusCode, "Student quiz submission failed");
using var submitted = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync());
Check(submitted.RootElement.GetProperty("success").GetBoolean() && submitted.RootElement.GetProperty("score").GetInt32() == 80, "Result should use the server-assessed knowledge score");
var afterQuiz = await client.GetStringAsync("/Student/Skills");
Check(afterQuiz.Contains("SSI 80") && afterQuiz.Contains("Knowledge Score 80") && afterQuiz.Contains("Verified"), "Saved quiz result must reload as verified knowledge and update SSI");
Check(fixture.Skills.Single().ExperienceScore == 0, "Student verification must not invent work experience scores");

using (var duplicate = await PostForm(client, "/Student/Skills/Add", new() { ["SkillId"] = "1" }))
    Check(duplicate.StatusCode == HttpStatusCode.Redirect && fixture.Skills.Count == 1, "Duplicate add must preserve one skill");
_ = await client.GetStringAsync("/Student/Skills"); // Consume the duplicate's TempData message.

fixture.Failure = "generate";
using (var failedQuiz = await PostJson(client, "/Student/Skills/Assessment/Generate", new { skillId = 1, skillName = "SQL", language = "en" })) {
    var body = await failedQuiz.Content.ReadAsStringAsync();
    Check(!failedQuiz.IsSuccessStatusCode && body.Contains("success") && body.Contains("false"), "Quiz provider failure must be returned as an error, never as successful verification");
}
fixture.Failure = null;
Check(fixture.Skills.Single().IsVerified && fixture.Skills.Single().KnowledgeScore == 80, "Failed retry must preserve the saved quiz result");
taxonomy.Failed = true;
var taxonomyUnavailable = await client.GetStringAsync("/Student/Skills");
Check(taxonomyUnavailable.Contains("role=\"alert\"") && taxonomyUnavailable.Contains("Retake quiz") && !taxonomyUnavailable.Contains("data-student-add-skill"), "Taxonomy outage must preserve saved skill quizzes while disabling addition");
taxonomy.Failed = false;

using (var removed = await PostForm(client, "/Student/Skills/Remove", new() { ["SkillId"] = "1", ["UserId"] = "999" }))
    Check(removed.StatusCode == HttpStatusCode.Redirect && fixture.Skills.Count == 0, "Remove must delete the current student's selected skill");
var afterRemove = await client.GetStringAsync("/Student/Skills");
Check(afterRemove.Contains("SSI 0") && !afterRemove.Contains("Knowledge Score 80"), "Removed skill and SSI must reload consistently");

fixture.Failure = "list";
var unavailable = await client.GetStringAsync("/Student/Skills");
Check(unavailable.Contains("role=\"alert\""), "A skill API failure must display an error");
fixture.Failure = null;
Check(fixture.Requests.All(path => path.StartsWith("/api/StudentSkills/42", StringComparison.OrdinalIgnoreCase)), "All student mutations and assessments must use the authenticated session ID");
Check(fixture.Requests.Any(path => path.EndsWith("/quiz/submit", StringComparison.Ordinal)), "Expected real API service assessment request was not sent");
Console.WriteLine("PASS: Student-only routes, candidate compatibility, taxonomy without a job, anti-forgery, identity isolation, add/auto-quiz, saved knowledge/SSI, removal and API error states.");
fixture.Reset();
if (args.Contains("--preview")) { Console.WriteLine("Preview: http://127.0.0.1:5271/Student/Skills (use /preview/filled for saved skills)"); await app.WaitForShutdownAsync(); }
else await app.StopAsync();

static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static async Task<string> Token(HttpClient client) {
    var page = await client.GetStringAsync("/Student/Skills");
    var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
    Check(match.Success, "Student page must render an anti-forgery token");
    return WebUtility.HtmlDecode(match.Groups[1].Value);
}
static async Task<HttpResponseMessage> PostForm(HttpClient client, string path, Dictionary<string, string> fields) {
    fields["__RequestVerificationToken"] = await Token(client);
    return await client.PostAsync(path, new FormUrlEncodedContent(fields));
}
static async Task<HttpResponseMessage> PostJson(HttpClient client, string path, object value) {
    using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(value) };
    request.Headers.Add("RequestVerificationToken", await Token(client));
    return await client.SendAsync(request);
}

sealed class TestControllers : IApplicationFeatureProvider<ControllerFeature> {
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        foreach (var controller in feature.Controllers.Where(c => c.AsType() != typeof(StudentSkillsController) && c.AsType() != typeof(SkillsController)).ToArray())
            feature.Controllers.Remove(controller);
    }
}
sealed class TaxonomyFixture : ISkillAndJobApiService {
    public bool Failed;
    public Task<SkillAndJobApiResult> GetJobFamiliesAsync(CancellationToken ct = default) => Task.FromResult(SkillAndJobApiResult.Ok([]));
    public Task<SkillLookupApiResult> GetAllSkillsAsync(CancellationToken ct = default) => Task.FromResult(Failed ? SkillLookupApiResult.Fail("Taxonomy unavailable") : SkillLookupApiResult.Ok([
        new() { Id = 1, SkillName = "SQL", AssessmentType = "TP", JobFamilyId = 10, JobFamilyName = "Data", PositionId = 1, PositionName = "Data Analyst" },
        new() { Id = 2, SkillName = "Excel", AssessmentType = "P", JobFamilyId = 20, JobFamilyName = "Finance", PositionId = 2, PositionName = "Financial Analyst" },
        new() { Id = 3, SkillName = "Python", AssessmentType = "T", JobFamilyId = 10, JobFamilyName = "Data", PositionId = 3, PositionName = "Developer" },
        new() { Id = 3, SkillName = "Python", AssessmentType = "T", JobFamilyId = 30, JobFamilyName = "Software", PositionId = 4, PositionName = "Student Developer" }
    ]));
}
sealed class EmptyProfiles : IUserProfileDataApiService {
    public Task<UserProfileDataApiResult> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(UserProfileDataApiResult.Ok(new UserProfileDataResponse { Success = true, Skills = [], Experiences = [] }));
    public Task<UserProfileDataApiResult> SaveAsync(int id, UserJobInfo? job, IReadOnlyCollection<UserSkillInfo> skills, IReadOnlyCollection<UserWorkExperienceInfo> experiences, CancellationToken ct = default) => throw new InvalidOperationException("Student must not write through the candidate profile API.");
}
sealed class CandidateAssessmentGuard : ISkillAssessmentApiService {
    public Task<SkillAssessmentApiResult<SkillQuestionnaireResponse>> GenerateAsync(GenerateSkillQuestionnaireRequest request, CancellationToken ct = default) => throw new InvalidOperationException("Student must not request a candidate work-experience assessment.");
    public Task<SkillAssessmentApiResult<SkillDepthAssessmentResult>> SubmitAsync(SubmitSkillDepthAssessmentRequest request, CancellationToken ct = default) => throw new InvalidOperationException("Student must not submit a candidate work-experience assessment.");
}
sealed class PreviewAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        var role = Request.Headers["X-Preview-Role"].FirstOrDefault() ?? "student";
        if (role == "anonymous") return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Name, "Leyla Ahmadova"), new Claim("accountType", role), new Claim(PortalClaimTypes.ClaimName, AccountRouting.Portal(role)) }, "Preview"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Preview")));
    }
}

sealed class StudentSkillsFixture : HttpMessageHandler {
    public readonly List<string> Requests = [];
    public readonly List<UserSkillInfo> Skills = [];
    public string? Failure;
    public string LastLanguage = "";
    public Guid QuestionnaireId = Guid.Parse("b1c2fd17-e010-4bfc-97f8-800b44941117");
    public void Reset() { Skills.Clear(); Failure = null; }
    public void Seed() {
        Reset();
        Skills.Add(new() { SkillId = 1, SkillName = "SQL", Status = "verified", IsVerified = true, KnowledgeScore = 84, CredibilityScore = 84 });
        Skills.Add(new() { SkillId = 2, SkillName = "Excel", Status = "verified", IsVerified = true, KnowledgeScore = 90, CredibilityScore = 90 });
        Skills.Add(new() { SkillId = 3, SkillName = "Python" });
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
        var path = request.RequestUri!.AbsolutePath;
        Requests.Add(path);
        if (!path.StartsWith("/api/StudentSkills/42", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected student API URL: " + path);
        var parts = path.Trim('/').Split('/');
        var operation = parts.Length == 3 ? request.Method == HttpMethod.Get ? "list" : "add"
            : parts.Length == 4 ? "remove" : parts.Length == 5 ? "generate" : "submit";
        if (Failure == operation) return Json(HttpStatusCode.ServiceUnavailable, new { message = "Student skill service unavailable. Please try again." });
        if (operation == "list") return Json(HttpStatusCode.OK, Skills);
        if (operation == "remove") { Skills.RemoveAll(s => s.SkillId == int.Parse(parts[3])); return new(HttpStatusCode.NoContent); }
        using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
        if (operation == "add") {
            var id = payload.RootElement.GetProperty("skillId").GetInt32();
            if (Skills.Any(s => s.SkillId == id)) return Json(HttpStatusCode.Conflict, new { message = "This skill is already added." });
            if (id is < 1 or > 3) return Json(HttpStatusCode.BadRequest, new { message = "Select a skill from the taxonomy." });
            var skill = new UserSkillInfo { SkillId = id, SkillName = id == 1 ? "SQL" : id == 2 ? "Excel" : "Python" };
            Skills.Add(skill);
            return Json(HttpStatusCode.OK, skill);
        }
        var selected = Skills.SingleOrDefault(s => s.SkillId == int.Parse(parts[3]));
        if (selected is null) return Json(HttpStatusCode.NotFound, new { message = "Add this skill before taking its quiz." });
        if (operation == "generate") {
            LastLanguage = payload.RootElement.GetProperty("language").GetString() ?? "";
            return Json(HttpStatusCode.OK, new { questionnaireId = QuestionnaireId, skillId = selected.SkillId, skillName = selected.SkillName, questions = new[] {
                new { id = "q1", text = "Which SQL statement retrieves rows from a class project table?", options = new[] { new { id = "a", label = "DELETE" }, new { id = "b", label = "SELECT" }, new { id = "c", label = "DROP" }, new { id = "d", label = "TRUNCATE" } } },
                new { id = "q2", text = "Which clause filters the rows returned by a SELECT query?", options = new[] { new { id = "a", label = "WHERE" }, new { id = "b", label = "ORDER BY" }, new { id = "c", label = "LIMIT" }, new { id = "d", label = "UNION" } } }
            } });
        }
        selected.KnowledgeScore = 80;
        selected.CredibilityScore = 80;
        selected.IsVerified = true;
        selected.Status = "verified";
        return Json(HttpStatusCode.OK, new { skill = selected, answeredQuestionCount = 2 });
    }
    private static HttpResponseMessage Json(HttpStatusCode status, object data) => new(status) { Content = JsonContent.Create(data) };
}
