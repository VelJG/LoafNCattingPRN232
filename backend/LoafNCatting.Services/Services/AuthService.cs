using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUserAccountService _userAccountService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IUserAccountService userAccountService,
        IJwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _userAccountService = userAccountService;
        _jwtTokenService = jwtTokenService;
    }

    public Task<UserDto> RegisterCustomerAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
        => _userAccountService.CreateCustomerAsync(request, cancellationToken);

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw InvalidCredentials();
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.Password,
            request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw InvalidCredentials();
        }

        var token = _jwtTokenService.CreateToken(user, user.Role.RoleName);
        return new LoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            AuthDtoMapper.ToUserDto(user, user.Role.RoleName));
    }

    public async Task<TokenVerificationResponse> VerifyAsync(
        int userId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .Include(item => item.Role)
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.IsActive,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("The user account is no longer active.");

        return new TokenVerificationResponse(
            AuthDtoMapper.ToUserDto(user, user.Role.RoleName),
            expiresAtUtc);
    }

    private static UnauthorizedAccessException InvalidCredentials()
        => new(InvalidCredentialsMessage);
}
