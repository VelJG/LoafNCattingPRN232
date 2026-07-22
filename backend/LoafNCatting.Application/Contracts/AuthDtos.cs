using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.Contracts;

public sealed record RegisterRequest(
    [param: Required, MaxLength(255)] string Name,
    [param: Required, EmailAddress, MaxLength(255)] string Email,
    [param: Required, MinLength(8)] string Password,
    [param: Required, MaxLength(20)] string PhoneNumber,
    string? Address);

public sealed record LoginRequest(
    [param: Required, EmailAddress, MaxLength(255)] string Email,
    [param: Required] string Password);

public sealed record CreateStaffRequest(
    [param: Required, MaxLength(255)] string Name,
    [param: Required, EmailAddress, MaxLength(255)] string Email,
    [param: Required, MinLength(8)] string Password,
    [param: Required, MaxLength(20)] string PhoneNumber,
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
