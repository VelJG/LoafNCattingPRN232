using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class AuthServiceTests
{
    [TestMethod]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndCurrentUser()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var hasher = new PasswordHasher<User>();
        var accounts = new UserAccountService(data.UnitOfWork, hasher);
        await accounts.CreateCustomerAsync(CustomerRegistration());
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);
        var service = new AuthService(
            data.UnitOfWork,
            hasher,
            accounts,
            new StubJwtTokenService("signed-token", expiresAtUtc));

        var response = await service.LoginAsync(
            new LoginRequest(" CUSTOMER@EXAMPLE.COM ", "Password1"));

        Assert.AreEqual("signed-token", response.AccessToken);
        Assert.AreEqual("Bearer", response.TokenType);
        Assert.AreEqual(expiresAtUtc, response.ExpiresAtUtc);
        Assert.AreEqual("customer@example.com", response.User.Email);
        Assert.AreEqual("Customer", response.User.Role);
    }

    [TestMethod]
    public async Task LoginAsync_WithWrongPassword_ReturnsGenericUnauthorizedError()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var hasher = new PasswordHasher<User>();
        var accounts = new UserAccountService(data.UnitOfWork, hasher);
        await accounts.CreateCustomerAsync(CustomerRegistration());
        var service = new AuthService(
            data.UnitOfWork,
            hasher,
            accounts,
            new StubJwtTokenService("unused", DateTime.UtcNow));

        var exception = await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest("customer@example.com", "wrong")));

        Assert.AreEqual("Invalid email or password.", exception.Message);
    }

    [TestMethod]
    public async Task LoginAsync_WithInactiveAccount_ReturnsSameGenericUnauthorizedError()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var hasher = new PasswordHasher<User>();
        var accounts = new UserAccountService(data.UnitOfWork, hasher);
        await accounts.CreateCustomerAsync(CustomerRegistration());
        var user = await data.DbContext.Users.SingleAsync();
        user.IsActive = false;
        await data.DbContext.SaveChangesAsync();
        var service = new AuthService(
            data.UnitOfWork,
            hasher,
            accounts,
            new StubJwtTokenService("unused", DateTime.UtcNow));

        var exception = await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest("customer@example.com", "Password1")));

        Assert.AreEqual("Invalid email or password.", exception.Message);
    }

    [TestMethod]
    public async Task LoginAsync_WhenPasswordHashNeedsRehash_PersistsUpgradedHash()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        data.DbContext.Users.Add(new User
        {
            Name = "Customer",
            Email = "customer@example.com",
            Password = "legacy-hash",
            PhoneNumber = "0900000001",
            RoleId = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false
        });
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var hasher = new RehashingPasswordHasher();
        var accounts = new UserAccountService(data.UnitOfWork, hasher);
        var service = new AuthService(
            data.UnitOfWork,
            hasher,
            accounts,
            new StubJwtTokenService("signed-token", DateTime.UtcNow.AddMinutes(30)));

        await service.LoginAsync(
            new LoginRequest("customer@example.com", "Password1"));

        var saved = await data.DbContext.Users.SingleAsync();
        Assert.AreEqual("upgraded-hash", saved.Password);
        Assert.IsNotNull(saved.UpdatedAt);
    }

    [TestMethod]
    public async Task VerifyAsync_ReloadsActiveUserThroughUnitOfWork()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var hasher = new PasswordHasher<User>();
        var accounts = new UserAccountService(data.UnitOfWork, hasher);
        var created = await accounts.CreateCustomerAsync(CustomerRegistration());
        var service = new AuthService(
            data.UnitOfWork,
            hasher,
            accounts,
            new StubJwtTokenService("unused", DateTime.UtcNow));
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(12);

        var response = await service.VerifyAsync(created.UserId, expiresAtUtc);

        Assert.AreEqual(created.UserId, response.User.UserId);
        Assert.AreEqual(expiresAtUtc, response.ExpiresAtUtc);
    }

    [TestMethod]
    public async Task VerifyAsync_RejectsInactiveUser()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var hasher = new PasswordHasher<User>();
        var accounts = new UserAccountService(data.UnitOfWork, hasher);
        var created = await accounts.CreateCustomerAsync(CustomerRegistration());
        var user = await data.DbContext.Users.SingleAsync();
        user.IsActive = false;
        await data.DbContext.SaveChangesAsync();
        var service = new AuthService(
            data.UnitOfWork,
            hasher,
            accounts,
            new StubJwtTokenService("unused", DateTime.UtcNow));

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            service.VerifyAsync(created.UserId, DateTime.UtcNow.AddMinutes(10)));
    }

    private static RegisterRequest CustomerRegistration() => new(
        "Customer",
        "customer@example.com",
        "Password1",
        "0900000001",
        null);

    private sealed class StubJwtTokenService(
        string accessToken,
        DateTime expiresAtUtc) : IJwtTokenService
    {
        public JwtTokenResult CreateToken(User user, string roleName)
            => new(accessToken, expiresAtUtc);
    }

    private sealed class RehashingPasswordHasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password)
            => "upgraded-hash";

        public PasswordVerificationResult VerifyHashedPassword(
            User user,
            string hashedPassword,
            string providedPassword)
            => PasswordVerificationResult.SuccessRehashNeeded;
    }
}
