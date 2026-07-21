using LoafNCatting.Caching.Extensions;
using LoafNCatting.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCacheServices();
builder.Services.AddLoafNCattingDatabase(builder.Configuration);
builder.Services.AddLoafNCattingServices();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy => policy
        .WithOrigins("http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

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
app.UseCors("FrontendDev");

app.UseCors("ReactDev");

app.UseAuthorization();

app.MapControllers();

app.Run();
