using System.Security.Claims;
using GloryLikeWebApp.Models.Employer;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GloryLikeWebApp.Controllers;
[Authorize(Policy=PortalClaimTypes.EmployerPolicy)]
public sealed class CompanyAutomationsController(ICompanyAutomationApiService api) : Controller
{
    private int Actor => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    [HttpGet("/Employer/Company/Templates/Automations")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if(Actor<=0) return Challenge();
        var result=await api.GetAsync(Actor,ct);
        return View(new CompanyAutomationsPageViewModel { DisplayName=User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "Employer",
            Email=User.FindFirstValue(ClaimTypes.Email) ?? "", Data=result.Data ?? new(), ErrorMessage=result.Success?"":result.Message });
    }
    [HttpPost("/Employer/Company/Templates/Automations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaveCompanyAutomationInput input,CancellationToken ct)
    {
        if(Actor<=0) return Unauthorized();
        if(!ModelState.IsValid) return InvalidModel();
        return Result(await api.CreateAsync(Actor,input,ct));
    }
    [HttpPost("/Employer/Company/Templates/Automations/{id:guid}/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id,SaveCompanyAutomationInput input,CancellationToken ct)
    {
        if(Actor<=0) return Unauthorized();
        if(!ModelState.IsValid) return InvalidModel();
        return Result(await api.UpdateAsync(Actor,id,input,ct));
    }
    [HttpPost("/Employer/Company/Templates/Automations/{id:guid}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id,CancellationToken ct)
    {
        if(Actor<=0) return Unauthorized();
        return Result(await api.DeleteAsync(Actor,id,ct));
    }
    [HttpPost("/Employer/Company/Templates/Automations/Deliveries/{id:guid}/Retry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(Guid id,CancellationToken ct)
    {
        if(Actor<=0) return Unauthorized();
        return Result(await api.RetryAsync(Actor,id,ct));
    }
    private IActionResult InvalidModel() => BadRequest(new { success=false,message=ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Check the rule fields." });
    private IActionResult Result(CompanyAutomationApiResult result) => StatusCode(result.Success?200:400,new {success=result.Success,message=result.Message});
}
