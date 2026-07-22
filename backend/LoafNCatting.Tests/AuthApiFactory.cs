using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LoafNCatting.Tests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "LoafNCatting.Tests";
    public const string Audience = "LoafNCatting.TestClient";
    public const string SigningKey = "integration-test-signing-key-at-least-32-characters";

    private readonly string _databaseName = $"api-tests-{Guid.NewGuid():N}";
    private readonly TestTimeProvider _clock = new(
        ReservationTestData.VietnamTime(2026, 7, 22, 7, 0));
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
                options
                    .UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(warnings => warnings.Ignore(
                        InMemoryEventId.TransactionIgnoredWarning)));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_clock);
        });
    }

    internal void SetVietnamTime(int hour, int minute = 0)
        => _clock.SetUtcNow(
            ReservationTestData.VietnamTime(2026, 7, 22, hour, minute));

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

    public async Task SeedReservationDataAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        if (await context.ReservationStatuses.AnyAsync())
        {
            return;
        }

        context.ReservationStatuses.AddRange(
            new ReservationStatus { StatusId = 1, StatusName = "Đang chờ" },
            new ReservationStatus { StatusId = 2, StatusName = "Đã xác nhận" },
            new ReservationStatus { StatusId = 3, StatusName = "Đã hủy" },
            new ReservationStatus { StatusId = 4, StatusName = "Hoàn thành" },
            new ReservationStatus { StatusId = 5, StatusName = "Không đến" },
            new ReservationStatus { StatusId = 6, StatusName = "Đã đến" },
            new ReservationStatus { StatusId = 7, StatusName = "Hết hạn" });
        context.TableStatuses.AddRange(
            new TableStatus { TableStatusId = 1, StatusName = "Trống" },
            new TableStatus { TableStatusId = 2, StatusName = "Đã đặt" },
            new TableStatus { TableStatusId = 3, StatusName = "Đang sử dụng" },
            new TableStatus { TableStatusId = 4, StatusName = "Bảo trì" });
        context.RestaurantTables.AddRange(
            new RestaurantTable
            {
                TableId = 1,
                TableName = "A1",
                Capacity = 2,
                Area = "Window",
                TableStatusId = 1
            },
            new RestaurantTable
            {
                TableId = 2,
                TableName = "A2",
                Capacity = 4,
                Area = "Center",
                TableStatusId = 1
            });
        await context.SaveChangesAsync();
    }
}
