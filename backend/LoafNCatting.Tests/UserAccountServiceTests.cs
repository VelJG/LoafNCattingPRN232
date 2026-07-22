using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class UserAccountServiceTests
{
    [TestMethod]
    public async Task CreateCustomerAsync_PersistsSecureCustomerAccount()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var hasher = new PasswordHasher<User>();
        var service = new UserAccountService(data.UnitOfWork, hasher);

        var dto = await service.CreateCustomerAsync(new RegisterRequest(
            "  Cat Lover  ",
            "  CUSTOMER@EXAMPLE.COM  ",
            "Password1",
            "0900000001",
            "  HCMC  "));

        var saved = await data.DbContext.Users
            .Include(user => user.Role)
            .SingleAsync();
        Assert.AreEqual("Customer", dto.Role);
        Assert.AreEqual("customer@example.com", saved.Email);
        Assert.AreEqual("Cat Lover", saved.Name);
        Assert.AreEqual("HCMC", saved.Address);
        Assert.AreNotEqual("Password1", saved.Password);
        Assert.AreEqual(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(saved, saved.Password, "Password1"));
        Assert.IsTrue(saved.IsActive);
        Assert.IsFalse(saved.IsEmailVerified);
        Assert.IsNull(saved.EmailVerificationOtpHash);
        Assert.IsNull(saved.EmailVerificationOtpExpiresAt);
    }

    [TestMethod]
    public async Task CreateCustomerAsync_RejectsDuplicateEmail()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var service = new UserAccountService(data.UnitOfWork, new PasswordHasher<User>());
        var request = new RegisterRequest(
            "One",
            "one@example.com",
            "Password1",
            "0900000001",
            null);
        await service.CreateCustomerAsync(request);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateCustomerAsync(request with { PhoneNumber = "0900000002" }));
    }

    [TestMethod]
    public async Task CreateCustomerAsync_RejectsDuplicatePhoneNumber()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var service = new UserAccountService(data.UnitOfWork, new PasswordHasher<User>());
        await service.CreateCustomerAsync(new RegisterRequest(
            "One",
            "one@example.com",
            "Password1",
            "0900000001",
            null));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateCustomerAsync(new RegisterRequest(
                "Two",
                "two@example.com",
                "Password1",
                "0900000001",
                null)));
    }

    [TestMethod]
    public async Task CreateStaffAsync_AssignsStaffRole()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var service = new UserAccountService(data.UnitOfWork, new PasswordHasher<User>());

        var dto = await service.CreateStaffAsync(new CreateStaffRequest(
            "Staff",
            "staff@example.com",
            "Password1",
            "0900000002",
            null));

        Assert.AreEqual("Staff", dto.Role);
    }

    [TestMethod]
    public async Task EnsureAdminExistsAsync_IsIdempotent()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var service = new UserAccountService(data.UnitOfWork, new PasswordHasher<User>());
        var settings = new BootstrapAdminSettings
        {
            Enabled = true,
            Name = "System Admin",
            Email = "admin@gmail.com",
            Password = "12345",
            PhoneNumber = "0900000000"
        };

        Assert.IsTrue(await service.EnsureAdminExistsAsync(settings));
        Assert.IsFalse(await service.EnsureAdminExistsAsync(settings));
        Assert.AreEqual(1, await data.DbContext.Users.CountAsync());
    }

    [TestMethod]
    public async Task EnsureAdminExistsAsync_RejectsBootstrapEmailOwnedByCustomer()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var service = new UserAccountService(data.UnitOfWork, new PasswordHasher<User>());
        await service.CreateCustomerAsync(new RegisterRequest(
            "Not Admin",
            "admin@gmail.com",
            "Password1",
            "0900000009",
            null));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.EnsureAdminExistsAsync(AdminSettings()));

        StringAssert.Contains(exception.Message, "active Admin");
    }

    [TestMethod]
    public async Task EnsureAdminExistsAsync_RejectsInactiveAdmin()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        var service = new UserAccountService(data.UnitOfWork, new PasswordHasher<User>());
        var settings = AdminSettings();
        await service.EnsureAdminExistsAsync(settings);
        var admin = await data.DbContext.Users.SingleAsync();
        admin.IsActive = false;
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.EnsureAdminExistsAsync(settings));
    }

    private static BootstrapAdminSettings AdminSettings() => new()
    {
        Enabled = true,
        Name = "System Admin",
        Email = "admin@gmail.com",
        Password = "12345",
        PhoneNumber = "0900000000"
    };
}
