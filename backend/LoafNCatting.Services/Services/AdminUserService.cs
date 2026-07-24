using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class AdminUserService : IAdminUserService
{
    private static readonly EmailAddressAttribute EmailValidator = new();
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public AdminUserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAllAsync(
        string? search,
        string? role,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<User>().Entities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                user.Name.Contains(term) ||
                user.Email.Contains(term) ||
                user.PhoneNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            query = query.Where(user => user.Role.RoleName == roleName);
        }

        if (isActive is not null)
        {
            query = query.Where(user => user.IsActive == isActive);
        }

        return await query
            .OrderBy(user => user.Role.RoleName)
            .ThenBy(user => user.Name)
            .Select(Map)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminUserDto?> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => await _unitOfWork.Repository<User>().Entities
            .AsNoTracking()
            .Where(user => user.UserId == userId)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AdminUserOptionsDto> GetOptionsAsync(
        CancellationToken cancellationToken = default)
        => new()
        {
            Roles = await _unitOfWork.Repository<Role>().Entities
                .AsNoTracking()
                .OrderBy(role => role.RoleName)
                .Select(role => role.RoleName)
                .ToListAsync(cancellationToken)
        };

    public async Task<AdminUserDto> CreateAsync(
        AdminUserUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var prepared = await ValidateAndResolveAsync(
            request,
            userId: null,
            requirePassword: true,
            cancellationToken);

        var user = new User
        {
            Name = prepared.Name,
            Email = prepared.Email,
            PhoneNumber = prepared.PhoneNumber,
            Address = prepared.Address,
            AvatarUrl = prepared.AvatarUrl,
            RoleId = prepared.RoleId,
            IsActive = request.IsActive,
            IsEmailVerified = request.IsEmailVerified,
            CreatedAt = DateTime.UtcNow,
            EmailVerificationOtpHash = null,
            EmailVerificationOtpExpiresAt = null
        };
        user.Password = _passwordHasher.HashPassword(user, prepared.Password!);

        await _unitOfWork.Repository<User>().InsertAsync(user, saveChanges: false);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(user.UserId, cancellationToken))!;
    }

    public async Task<AdminUserDto?> UpdateAsync(
        int userId,
        AdminUserUpsertRequest request,
        int? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().Entities
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var prepared = await ValidateAndResolveAsync(
            request,
            userId,
            requirePassword: false,
            cancellationToken);

        if (actorUserId == userId &&
            (!request.IsActive ||
             !string.Equals(prepared.RoleName, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("You cannot remove your own admin access.");
        }

        await EnsureAdminRemainsAsync(
            user,
            prepared.RoleName,
            request.IsActive,
            cancellationToken);

        user.Name = prepared.Name;
        user.Email = prepared.Email;
        user.PhoneNumber = prepared.PhoneNumber;
        user.Address = prepared.Address;
        user.AvatarUrl = prepared.AvatarUrl;
        user.RoleId = prepared.RoleId;
        user.IsActive = request.IsActive;
        user.IsEmailVerified = request.IsEmailVerified;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(prepared.Password))
        {
            user.Password = _passwordHasher.HashPassword(user, prepared.Password);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int userId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == userId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await _unitOfWork.Repository<User>().Entities
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        await EnsureAdminRemainsAsync(
            user,
            nextRoleName: null,
            nextIsActive: false,
            cancellationToken);

        if (await HasUserDependenciesAsync(userId, cancellationToken))
        {
            throw new InvalidOperationException(
                "Cannot delete this user because orders, reservations, carts, messages, or notifications already reference it.");
        }

        await _unitOfWork.Repository<User>().DeleteAsync(user, saveChanges: false);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<PreparedUser> ValidateAndResolveAsync(
        AdminUserUpsertRequest request,
        int? userId,
        bool requirePassword,
        CancellationToken cancellationToken)
    {
        var name = RequiredTrimmed(request.Name, "User name");
        var email = RequiredTrimmed(request.Email, "Email").ToLowerInvariant();
        var phoneNumber = RequiredTrimmed(request.PhoneNumber, "Phone number");
        var roleName = RequiredTrimmed(request.Role, "Role");
        var address = Clean(request.Address);
        var avatarUrl = Clean(request.AvatarUrl);
        var password = request.Password;

        if (!EmailValidator.IsValid(email))
        {
            throw new ArgumentException("Email is invalid.");
        }

        if (phoneNumber.Length > 20)
        {
            throw new ArgumentException("Phone number must be 20 characters or fewer.");
        }

        if (requirePassword && string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.");
        }

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 8)
        {
            throw new ArgumentException("Password must contain at least 8 characters.");
        }

        var users = _unitOfWork.Repository<User>().Entities;
        if (await users
            .AsNoTracking()
            .AnyAsync(user => user.Email == email && user.UserId != userId, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        if (await users
            .AsNoTracking()
            .AnyAsync(user => user.PhoneNumber == phoneNumber && user.UserId != userId, cancellationToken))
        {
            throw new InvalidOperationException("Phone number is already registered.");
        }

        var role = await _unitOfWork.Repository<Role>().Entities
            .AsNoTracking()
            .Where(item => item.RoleName == roleName)
            .Select(item => new { item.RoleId, item.RoleName })
            .FirstOrDefaultAsync(cancellationToken);
        if (role is null)
        {
            throw new ArgumentException("Role does not exist.");
        }

        return new PreparedUser(
            name,
            email,
            phoneNumber,
            address,
            avatarUrl,
            role.RoleId,
            role.RoleName,
            string.IsNullOrWhiteSpace(password) ? null : password);
    }

    private async Task EnsureAdminRemainsAsync(
        User existing,
        string? nextRoleName,
        bool nextIsActive,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(existing.Role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var stillActiveAdmin = nextIsActive &&
            string.Equals(nextRoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        if (stillActiveAdmin)
        {
            return;
        }

        var hasOtherActiveAdmin = await _unitOfWork.Repository<User>().Entities
            .AsNoTracking()
            .AnyAsync(user =>
                user.UserId != existing.UserId &&
                user.IsActive &&
                user.Role.RoleName == "Admin",
                cancellationToken);

        if (!hasOtherActiveAdmin)
        {
            throw new InvalidOperationException("At least one active Admin account must remain.");
        }
    }

    private async Task<bool> HasUserDependenciesAsync(
        int userId,
        CancellationToken cancellationToken)
        => await _unitOfWork.Repository<Cart>().Entities
            .AsNoTracking()
            .AnyAsync(cart => cart.UserId == userId, cancellationToken) ||
        await _unitOfWork.Repository<Conversation>().Entities
            .AsNoTracking()
            .AnyAsync(conversation =>
                conversation.CustomerUserId == userId ||
                conversation.StaffUserId == userId,
                cancellationToken) ||
        await _unitOfWork.Repository<Message>().Entities
            .AsNoTracking()
            .AnyAsync(message => message.SenderUserId == userId, cancellationToken) ||
        await _unitOfWork.Repository<Notification>().Entities
            .AsNoTracking()
            .AnyAsync(notification => notification.UserId == userId, cancellationToken) ||
        await _unitOfWork.Repository<Order>().Entities
            .AsNoTracking()
            .AnyAsync(order =>
                order.CustomerUserId == userId ||
                order.StaffUserId == userId,
                cancellationToken) ||
        await _unitOfWork.Repository<Reservation>().Entities
            .AsNoTracking()
            .AnyAsync(reservation => reservation.UserId == userId, cancellationToken);

    private static readonly Expression<Func<User, AdminUserDto>> Map = user => new()
    {
        UserId = user.UserId,
        Name = user.Name,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Address = user.Address,
        AvatarUrl = user.AvatarUrl,
        Role = user.Role.RoleName,
        IsActive = user.IsActive,
        IsEmailVerified = user.IsEmailVerified,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string RequiredTrimmed(string value, string name)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{name} is required.")
            : value.Trim();

    private sealed record PreparedUser(
        string Name,
        string Email,
        string PhoneNumber,
        string? Address,
        string? AvatarUrl,
        int RoleId,
        string RoleName,
        string? Password);
}
