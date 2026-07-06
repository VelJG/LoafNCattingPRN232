using LoafNCatting.Application.Interfaces.Common;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Caching.Extensions;
using LoafNCatting.Infrastructure.Context;
using LoafNCatting.Infrastructure.Repositories;
using LoafNCatting.Persistence.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<LoafNcattingPrn232Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<Func<LoafNcattingPrn232Context>>(provider =>
    () => provider.GetRequiredService<LoafNcattingPrn232Context>());
builder.Services.AddScoped<DbFactoryContext>();
builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddCacheServices();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
