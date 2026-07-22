using LoafNCatting.Entity.Models;
using LoafNCatting.Infrastructure.Context;
using LoafNCatting.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LoafNCatting.Tests;

public sealed class TestDataContext : IAsyncDisposable
{
    public LoafNcattingPrn232Context DbContext { get; }

    public UnitOfWork UnitOfWork { get; }

    public TestDataContext(params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LoafNcattingPrn232Context>()
            .UseInMemoryDatabase($"auth-tests-{Guid.NewGuid():N}");
        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        var options = optionsBuilder.Options;
        DbContext = new LoafNcattingPrn232Context(options);
        var factory = new DbFactoryContext(() => DbContext);
        UnitOfWork = new UnitOfWork(new ApplicationDbContext(factory));
    }

    public async Task SeedRolesAsync()
    {
        DbContext.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Staff" },
            new Role { RoleId = 3, RoleName = "Customer" });
        await DbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await UnitOfWork.DisposeAsync();
        await DbContext.DisposeAsync();
    }
}
