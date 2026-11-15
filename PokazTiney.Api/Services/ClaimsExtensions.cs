using System.Security.Claims;

namespace PokazTiney.Api.Services;

public static class ClaimsExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    public static bool IsStaff(this ClaimsPrincipal principal) =>
        principal.IsInRole("Admin") || principal.IsInRole("Manager");
}
