using System.Text;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Caching.Extensions;
using LoafNCatting.Services.Extensions;
using LoafNCatting.WebApi.OpenApi;
using LoafNCatting.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCacheServices();
builder.Services.AddLoafNCattingDatabase(builder.Configuration);
builder.Services.AddLoafNCattingServices();

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSettings = jwtSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
    Encoding.UTF8.GetByteCount(jwtSettings.SigningKey) < 32 ||
    jwtSettings.AccessTokenMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT configuration requires issuer, audience, a signing key of at least 32 bytes, and a positive lifetime.");
}

builder.Services.AddOptions<JwtSettings>()
    .Bind(jwtSection)
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Issuer) &&
            !string.IsNullOrWhiteSpace(settings.Audience) &&
            Encoding.UTF8.GetByteCount(settings.SigningKey) >= 32 &&
            settings.AccessTokenMinutes > 0,
        "JWT configuration is invalid.")
    .ValidateOnStart();
builder.Services.AddOptions<BootstrapAdminSettings>()
    .Bind(builder.Configuration.GetSection(BootstrapAdminSettings.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = AuthClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy => policy
        .WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:4173",
            "http://127.0.0.1:4173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<AdminBootstrapHostedService>();
}

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "LoafNCatting API";
        options.SwaggerEndpoint("/openapi/v1.json", "LoafNCatting API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("FrontendDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
