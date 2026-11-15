namespace PokazTiney.Api.Services;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PokazTiney.Api";
    public string Audience { get; set; } = "PokazTiney.Web";
    public int ExpiresMinutes { get; set; } = 480;
}
