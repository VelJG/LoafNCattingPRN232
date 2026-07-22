using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.Contracts;

public sealed record RegisterRequest(
    [property: Required, MaxLength(255)] string Name,
    [property: Required, EmailAddress, MaxLength(255)] string Email,
    [property: Required, MinLength(8)] string Password,
    [property: Required, MaxLength(20)] string PhoneNumber,
    string? Address);

public sealed record LoginRequest(
    [property: Required, EmailAddress, MaxLength(255)] string Email,
    [property: Required] string Password);

public sealed record CreateStaffRequest(
    [property: Required, MaxLength(255)] string Name,
    [property: Required, EmailAddress, MaxLength(255)] string Email,
    [property: Required, MinLength(8)] string Password,
    [property: Required, MaxLength(20)] string PhoneNumber,
    string? Address);

public sealed record UserDto(
    int UserId,
    string Name,
    string Email,
    string PhoneNumber,
    string? Address,
    string Role,
    bool IsActive,
    bool IsEmailVerified);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserDto User);

public sealed record TokenVerificationResponse(UserDto User, DateTime ExpiresAtUtc);

public sealed record JwtTokenResult(string AccessToken, DateTime ExpiresAtUtc);
