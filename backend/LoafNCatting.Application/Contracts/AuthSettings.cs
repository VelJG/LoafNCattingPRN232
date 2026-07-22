namespace LoafNCatting.Application.Contracts;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;
}

public sealed class BootstrapAdminSettings
{
    public const string SectionName = "BootstrapAdmin";

    public bool Enabled { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}

public static class AuthClaimTypes
{
    public const string Role = "role";
}
