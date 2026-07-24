namespace LoafNCatting.Application.Contracts;

public sealed class AdminUserDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class AdminUserUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; }
    public string? Password { get; set; }
}

public sealed class AdminUserOptionsDto
{
    public IReadOnlyList<string> Roles { get; set; } = [];
}
