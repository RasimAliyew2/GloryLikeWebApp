namespace GloryLikeWebApp.Security;

public static class AccountRouting
{
    public static string Normalize(string? accountType) => accountType?.Trim().ToLowerInvariant() ?? "";
    public static bool IsSupported(string? accountType) => Normalize(accountType) is "candidate" or "student" or "employer";
    public static string Portal(string? accountType) => Normalize(accountType) switch
    {
        "student" => PortalClaimTypes.Student,
        "employer" => PortalClaimTypes.Employer,
        "candidate" => PortalClaimTypes.Employee,
        _ => string.Empty
    };
    public static string HomePath(string? accountType) => Normalize(accountType) switch
    {
        "student" => "/Student",
        "employer" => "/EmployerHome",
        "candidate" => "/dashboard",
        _ => "/SignIn"
    };
}
