using System.Security.Claims;

namespace Fintech.Api.Auth;

public sealed record UserContext(string UserId, string UserName)
{
    public static UserContext From(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? "unknown";
        var name = user.FindFirstValue("preferred_username")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? id;

        return new UserContext(id, name);
    }
}
