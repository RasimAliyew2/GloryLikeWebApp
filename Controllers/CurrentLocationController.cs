using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeWebApp.Controllers;

[Authorize(Policy = PortalClaimTypes.EmployeePolicy)]
public sealed class CurrentLocationController : Controller
{
    private readonly ILocationLookupService _locationLookupService;

    public CurrentLocationController(ILocationLookupService locationLookupService)
    {
        _locationLookupService = locationLookupService;
    }

    [HttpGet("/Candidate/CurrentLocation")]
    public async Task<IActionResult> Get(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        CancellationToken cancellationToken)
    {
        if (!double.IsFinite(latitude)
            || !double.IsFinite(longitude)
            || latitude is < -90 or > 90
            || longitude is < -180 or > 180)
        {
            return BadRequest(new
            {
                success = false,
                message = "Location coordinates are invalid."
            });
        }

        var result = await _locationLookupService.ReverseAsync(
            latitude,
            longitude,
            cancellationToken);

        return result.Success
            ? Ok(new
            {
                success = true,
                city = result.City,
                countryCode = result.CountryCode,
                country = result.Country,
                displayName = result.DisplayName
            })
            : StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    success = false,
                    message = "Location could not be resolved."
                });
    }
}
