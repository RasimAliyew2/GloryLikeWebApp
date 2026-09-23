using System.Reflection;
using GloryLikeWebApp.Controllers;
using GloryLikeWebApp.Models;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Services;
using Microsoft.Extensions.Logging.Abstractions;

// The production create controller receives local fixture data only.
static class EmployerCreationFixtures
{
    public static EmployerVacanciesController Create(IVacancyApiService vacancies) => new(
        Stub<ISkillAndJobApiService>("GetJobFamiliesAsync", SkillAndJobApiResult.Ok([new JobFamily { Id = 1, JobName = "Data" }])),
        vacancies,
        Stub<ICompanyFunnelApiService>("GetAsync", CompanyFunnelApiResult.From(new() { Success = true })),
        Stub<ICompanyAutomationApiService>("GetAsync", CompanyAutomationApiResult.From(new() { Success = true })),
        Stub<ICompanyHiringPlanApiService>("GetByIdAsync", CompanyHiringPlanApiResult.From(new() {
            Success = true, Plan = new() { Id = 37, CanCreateVacancy = true, PositionName = "Planned Analyst", EmploymentType = "Part-time" }
        })),
        Stub<ICompanyProfileApiService>("GetAsync", CompanyProfileApiResult.From(new() {
            Success = true, Profile = new() { Locations = [new() { Id = 1, DisplayName = "Baku" }] }
        })),
        null!, NullLogger<EmployerVacanciesController>.Instance);

    static T Stub<T>(string method, object result) where T : class
    {
        var proxy = DispatchProxy.Create<T, EmployerReadFixture>();
        ((EmployerReadFixture)(object)proxy).Configure(method, result);
        return proxy;
    }
}

public class EmployerReadFixture : DispatchProxy
{
    string method = string.Empty;
    object result = null!;
    public void Configure(string method, object result) { this.method = method; this.result = result; }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name != method) throw new InvalidOperationException("Unexpected fixture call: " + targetMethod?.Name);
        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(result.GetType()).Invoke(null, [result]);
    }
}
