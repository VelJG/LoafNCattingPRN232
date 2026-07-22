using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class UserAccountService : IUserAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserAccountService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public Task<UserDto> CreateCustomerAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
        => CreateAsync(
            request.Name,
            request.Email,
            request.Password,
            request.PhoneNumber,
            request.Address,
            "Customer",
            enforcePasswordPolicy: true,
            cancellationToken: cancellationToken);

    public Task<UserDto> CreateStaffAsync(
        CreateStaffRequest request,
        CancellationToken cancellationToken = default)
        => CreateAsync(
            request.Name,
            request.Email,
            request.Password,
            request.PhoneNumber,
            request.Address,
            "Staff",
            enforcePasswordPolicy: true,
            cancellationToken: cancellationToken);

    public async Task<bool> EnsureAdminExistsAsync(
        BootstrapAdminSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
        {
            return false;
        }

        ValidateBootstrap(settings);
        var email = NormalizeEmail(settings.Email);
        var existingUser = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
        if (existingUser is not null)
        {
            if (existingUser.IsActive && existingUser.Role.RoleName == "Admin")
            {
                return false;
            }

            throw new InvalidOperationException(
                "The bootstrap email must belong to an active Admin account.");
        }

        await CreateAsync(
            settings.Name,
            email,
            settings.Password,
            settings.PhoneNumber,
            address: null,
            roleName: "Admin",
            enforcePasswordPolicy: false,
            cancellationToken: cancellationToken);
        return true;
    }

    private async Task<UserDto> CreateAsync(
        string name,
        string email,
        string password,
        string phoneNumber,
        string? address,
        string roleName,
        bool enforcePasswordPolicy,
        CancellationToken cancellationToken)
    {
        name = RequiredTrimmed(name, nameof(name));
        email = NormalizeEmail(email);
        phoneNumber = RequiredTrimmed(phoneNumber, nameof(phoneNumber));
        address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        if (enforcePasswordPolicy && password.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.",
                nameof(password));
        }

        var users = _unitOfWork.Repository<User>().Entities;
        if (await users
            .AsNoTracking()
            .AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        if (await users
            .AsNoTracking()
            .AnyAsync(user => user.PhoneNumber == phoneNumber, cancellationToken))
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        var role = await _unitOfWork.Repository<Role>()
            .Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.RoleName == roleName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Required role '{roleName}' was not found.");

        var user = new User
        {
            Name = name,
            Email = email,
            PhoneNumber = phoneNumber,
            Address = address,
            RoleId = role.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            EmailVerificationOtpHash = null,
            EmailVerificationOtpExpiresAt = null
        };
        user.Password = _passwordHasher.HashPassword(user, password);

        await _unitOfWork.Repository<User>().InsertAsync(user, saveChanges: false);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AuthDtoMapper.ToUserDto(user, role.RoleName);
    }

    private static string NormalizeEmail(string value)
        => RequiredTrimmed(value, "email").ToLowerInvariant();

    private static string RequiredTrimmed(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName)
            : value.Trim();

    private static void ValidateBootstrap(BootstrapAdminSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name) ||
            string.IsNullOrWhiteSpace(settings.Email) ||
            string.IsNullOrWhiteSpace(settings.Password) ||
            string.IsNullOrWhiteSpace(settings.PhoneNumber))
        {
            throw new InvalidOperationException(
                "Enabled Admin bootstrap requires name, email, password, and phone number.");
        }
    }
}
