namespace Fintech.Api.Auth;

public sealed class AuthOptions
{
    public AuthMode Mode { get; set; } = AuthMode.Development;
    public string Authority { get; set; } = "http://localhost:8080/realms/fintech-demo";
    public string Audience { get; set; } = "investment-api";
    public bool RequireHttpsMetadata { get; set; }
}
