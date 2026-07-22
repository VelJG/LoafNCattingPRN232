using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LoafNCatting.Tests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "LoafNCatting.Tests";
    public const string Audience = "LoafNCatting.TestClient";
    public const string SigningKey = "integration-test-signing-key-at-least-32-characters";

    private readonly string _databaseName = $"auth-api-{Guid.NewGuid():N}";
    private readonly string _environment;

    public AuthApiFactory(string environment = "Testing")
    {
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-in-memory-tests");
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Jwt:AccessTokenMinutes", "30");
        builder.UseSetting("BootstrapAdmin:Enabled", "false");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LoafNcattingPrn232Context>();
            services.RemoveAll<DbContextOptions<LoafNcattingPrn232Context>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LoafNcattingPrn232Context>>();
            services.AddDbContext<LoafNcattingPrn232Context>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task SeedRolesAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        if (await context.Roles.AnyAsync())
        {
            return;
        }

        context.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Staff" },
            new Role { RoleId = 3, RoleName = "Customer" });
        await context.SaveChangesAsync();
    }

    public async Task CreateAdminAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        await accounts.EnsureAdminExistsAsync(new BootstrapAdminSettings
        {
            Enabled = true,
            Name = "System Admin",
            Email = "admin@gmail.com",
            Password = "12345",
            PhoneNumber = "0900000000"
        });
    }

    public async Task CreateStaffAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        await accounts.CreateStaffAsync(new CreateStaffRequest(
            "Staff Member",
            "staff@example.com",
            "Password1",
            "0900000002",
            null));
    }
}
