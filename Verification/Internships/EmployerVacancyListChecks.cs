using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GloryLikeWebApp.Models.Employer;

static class EmployerVacancyListChecks
{
    public static HttpResponseMessage Response() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new EmployerVacancyListApiResponse
        {
            Success = true,
            EmployerUserId = 42,
            Vacancies =
            [
                new() { VacancyId = 21, PlatformVacancyId = "SM-2026-10001", RoleTitle = "Core Skills", VacancyType = "Internship", JobFamilyName = "Administration & Office Management", Status = "Active", CreatedAtUtc = new DateTime(2026, 9, 22) },
                new() { VacancyId = 22, PlatformVacancyId = "SM-2026-10002", RoleTitle = "Office Assistant", VacancyType = "Employee", JobFamilyName = "Administration & Office Management", Status = "Draft", CreatedAtUtc = new DateTime(2026, 9, 22) }
            ]
        })
    };

    public static async Task RunAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Employer/Vacancies");
        request.Headers.Add("X-Preview-Role", "employer");
        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception("Employer vacancies list failed to render.");

        foreach (var (id, type, status) in new[] { (21, "Internship", "active"), (22, "Employee", "draft") })
        {
            var row = Regex.Matches(html, "<tr\\s+data-vacancy-row[\\s\\S]*?</tr>")
                .Select(match => match.Value)
                .Single(value => value.Contains($"data-vacancy-url=\"/Employer/Vacancies/{id}\""));
            if (!row.Contains($"class=\"vacancy-type-badge vacancy-type-{type.ToLowerInvariant()}\"")
                || !row.Contains($"aria-label=\"Vacancy type: {type}\">{type}</span>"))
                throw new Exception($"{type} badge did not preserve the API category.");
            var search = Regex.Match(row, "data-search-value=\"([^\"]*)\"").Groups[1].Value;
            if (!search.Contains(type.ToLowerInvariant()) || !row.Contains($"data-status=\"{status}\"")
                || !row.Contains("data-vacancy-menu-button"))
                throw new Exception("Category search, status filter or row actions were lost.");
        }
        Console.WriteLine("PASS: Employer vacancies load through the API with searchable Employee/Internship badges and existing filters/actions.");
    }
}
