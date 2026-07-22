using LoafNCatting.Application.Interfaces.Common;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Infrastructure.Context;
using LoafNCatting.Infrastructure.Repositories;
using LoafNCatting.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Services.Extensions;

public static class LoafNCattingServiceExtensions
{
    public static IServiceCollection AddLoafNCattingDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LoafNcattingPrn232Context>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.CommandTimeout(60));
        });

        services.AddScoped<Func<LoafNcattingPrn232Context>>(
            provider => () => provider.GetRequiredService<LoafNcattingPrn232Context>());

        services.AddScoped<DbFactoryContext>();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddLoafNCattingServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICatService, CatService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}

