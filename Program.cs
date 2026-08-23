using System.Security.Claims;
using System.Text.Json;
using GloryLikeWebApp.Security;
using GloryLikeWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAppleClientSecretGenerator,
    AppleClientSecretGenerator>();

builder.Services.AddHttpClient<IBackendAuthApiService, BackendAuthApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICompanyTeamApiService, CompanyTeamApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICompanyProfileApiService, CompanyProfileApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddHttpClient<ICompanyHiringPlanApiService, CompanyHiringPlanApiService>((sp, client) =>
{
    var backendUrl = sp.GetRequiredService<IConfiguration>()["Backend:BaseUrl"]
        ?? throw new InvalidOperationException("Backend:BaseUrl is not configured.");
    client.BaseAddress = new Uri(backendUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<ICompanyStructureApiService, CompanyStructureApiService>((sp, client) =>
{
    var backendUrl = sp.GetRequiredService<IConfiguration>()["Backend:BaseUrl"]
        ?? throw new InvalidOperationException("Backend:BaseUrl is not configured.");
    client.BaseAddress = new Uri(backendUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<IOrganizationReportsApiService, OrganizationReportsApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IUserProfileDataApiService, UserProfileDataApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IUserPersonalProfileApiService, UserPersonalProfileApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ILocationLookupService, NominatimLocationLookupService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["LocationServices:NominatimBaseUrl"]
        ?? "https://nominatim.openstreetmap.org/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "BothFind/1.0 (+https://bothfind.com)");
    client.DefaultRequestHeaders.Referrer = new Uri("https://bothfind.com/");
});

builder.Services.AddHttpClient<ISkillAndJobApiService, SkillAndJobApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ISkillAssessmentApiService, SkillAssessmentApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});


builder.Services.AddHttpClient<IVacancyApiService, VacancyApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ITalentRadarApiService, TalentRadarApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<
    IEmployerCandidateMessagingApiService,
    EmployerCandidateMessagingApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Backend:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "Backend:BaseUrl appsettings.json daxilində təyin edilməyib.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        PortalClaimTypes.EmployeePolicy,
        policy => policy
            .RequireClaim(
                PortalClaimTypes.ClaimName,
                PortalClaimTypes.Employee)
            .RequireClaim("accountType", "candidate"));

    options.AddPolicy(
        PortalClaimTypes.EmployerPolicy,
        policy => policy
            .RequireClaim(
                PortalClaimTypes.ClaimName,
                PortalClaimTypes.Employer)
            .RequireClaim("accountType", "employer"));
});

var googleAuthenticationEnabled =
    !string.IsNullOrWhiteSpace(
        builder.Configuration[
            "Authentication:Google:ClientId"])
    && !string.IsNullOrWhiteSpace(
        builder.Configuration[
            "Authentication:Google:ClientSecret"])
    && !string.IsNullOrWhiteSpace(
        builder.Configuration[
            "SocialAuth:BackendSharedSecret"]);

var appleAuthenticationEnabled =
    !string.IsNullOrWhiteSpace(
        builder.Configuration[
            "Authentication:Apple:ClientId"])
    && !string.IsNullOrWhiteSpace(
        builder.Configuration[
            "Authentication:Apple:TeamId"])
    && !string.IsNullOrWhiteSpace(
        builder.Configuration[
            "Authentication:Apple:KeyId"])
    && (!string.IsNullOrWhiteSpace(
            builder.Configuration[
                "Authentication:Apple:PrivateKey"])
        || !string.IsNullOrWhiteSpace(
            builder.Configuration[
                "Authentication:Apple:PrivateKeyBase64"]))
    && !string.IsNullOrWhiteSpace(
        builder.Configuration[
            "SocialAuth:BackendSharedSecret"]);

var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/SignIn";
        options.AccessDeniedPath = "/Portal/Home";
        options.Cookie.Name = "GloryLikeWebApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                var userIdValue = principal?.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                var rawAccountType = principal?.FindFirstValue("accountType")
                    ?.Trim();
                var accountType = rawAccountType?.ToLowerInvariant();
                var portalType = principal?.FindFirstValue(
                    PortalClaimTypes.ClaimName);

                var validUserId = int.TryParse(userIdValue, out var userId)
                    && userId > 0;
                var validAccountType = rawAccountType is "candidate" or "employer";
                var expectedPortal = accountType == "employer"
                    ? PortalClaimTypes.Employer
                    : PortalClaimTypes.Employee;
                var validPortal = string.Equals(
                    portalType,
                    expectedPortal,
                    StringComparison.OrdinalIgnoreCase);

                if (validUserId && validAccountType && validPortal)
                    return;

                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.Redirect("/Portal/Home");
                return Task.CompletedTask;
            }
        };
    })
    .AddCookie(
        ExternalAuthenticationDefaults.ExternalCookieScheme,
        options =>
        {
            options.Cookie.Name =
                "GloryLikeWebApp.External";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;
            options.ExpireTimeSpan =
                TimeSpan.FromMinutes(10);
        });

if (googleAuthenticationEnabled)
{
    authenticationBuilder.AddGoogle(
        ExternalAuthenticationDefaults.GoogleScheme,
        "Google",
        options =>
        {
            options.SignInScheme =
                ExternalAuthenticationDefaults
                    .ExternalCookieScheme;
            options.ClientId =
                builder.Configuration[
                    "Authentication:Google:ClientId"]
                ?? string.Empty;
            options.ClientSecret =
                builder.Configuration[
                    "Authentication:Google:ClientSecret"]
                ?? string.Empty;
            options.CallbackPath = "/signin-google";
            options.SaveTokens = false;
            options.CorrelationCookie.SameSite =
                SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy =
                CookieSecurePolicy.Always;
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect(
                    "/SignIn?externalError=1");
                return Task.CompletedTask;
            };
        });
}

if (appleAuthenticationEnabled)
{
    authenticationBuilder.AddOpenIdConnect(
        ExternalAuthenticationDefaults.AppleScheme,
        "Apple",
        options =>
        {
            options.SignInScheme =
                ExternalAuthenticationDefaults
                    .ExternalCookieScheme;
            options.Authority =
                "https://appleid.apple.com";
            options.ClientId =
                builder.Configuration[
                    "Authentication:Apple:ClientId"]
                ?? string.Empty;
            options.CallbackPath = "/signin-apple";
            options.ResponseType =
                OpenIdConnectResponseType.Code;
            options.ResponseMode =
                OpenIdConnectResponseMode.FormPost;
            options.UsePkce = false;
            options.MapInboundClaims = false;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("name");
            options.TokenValidationParameters.NameClaimType =
                "name";
            options.CorrelationCookie.SameSite =
                SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy =
                CookieSecurePolicy.Always;
            options.NonceCookie.SameSite =
                SameSiteMode.None;
            options.NonceCookie.SecurePolicy =
                CookieSecurePolicy.Always;
            options.Events = new OpenIdConnectEvents
            {
                OnAuthorizationCodeReceived = context =>
                {
                    var generator = context.HttpContext
                        .RequestServices
                        .GetRequiredService<
                            IAppleClientSecretGenerator>();

                    context.TokenEndpointRequest.ClientSecret =
                        generator.CreateClientSecret();

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    if (context.Principal?.Identity
                            is not ClaimsIdentity identity
                        || !context.HttpContext.Request
                            .HasFormContentType)
                    {
                        return;
                    }

                    var form = await context.HttpContext.Request
                        .ReadFormAsync(
                            context.HttpContext
                                .RequestAborted);
                    var appleUserJson =
                        form["user"].ToString();

                    if (string.IsNullOrWhiteSpace(
                            appleUserJson))
                    {
                        return;
                    }

                    JsonDocument document;

                    try
                    {
                        document = JsonDocument.Parse(
                            appleUserJson);
                    }
                    catch (JsonException)
                    {
                        return;
                    }

                    using (document)
                    {
                        var root = document.RootElement;

                        AddAppleClaim(
                            identity,
                            root,
                            "email",
                            ClaimTypes.Email);

                        if (root.TryGetProperty(
                                "name",
                                out var name))
                        {
                            AddAppleClaim(
                                identity,
                                name,
                                "firstName",
                                ClaimTypes.GivenName);
                            AddAppleClaim(
                                identity,
                                name,
                                "lastName",
                                ClaimTypes.Surname);
                        }
                    }
                },
                OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    context.Response.Redirect(
                        "/SignIn?externalError=1");
                    return Task.CompletedTask;
                }
            };
        });
}

static void AddAppleClaim(
    ClaimsIdentity identity,
    JsonElement source,
    string propertyName,
    string claimType)
{
    if (identity.HasClaim(
            claim => claim.Type == claimType)
        || !source.TryGetProperty(
            propertyName,
            out var value))
    {
        return;
    }

    var textValue = value.GetString()?.Trim();

    if (!string.IsNullOrWhiteSpace(textValue))
        identity.AddClaim(new Claim(claimType, textValue));
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
