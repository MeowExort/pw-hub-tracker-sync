using System.Text.Json;
using Prometheus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Pw.Hub.Tracker.Sync.Web.Clients;
using Pw.Hub.Tracker.Sync.Web.Data;
using Pw.Hub.Tracker.Sync.Web.Models;
using Pw.Hub.Tracker.Sync.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Метрики ASP.NET Core
builder.Services.AddHealthChecks(); // Хорошая практика

// Configuration
builder.Services.Configure<EventChannelOptions>(builder.Configuration.GetSection("EventProcessor"));
builder.Services.Configure<EventProcessorOptions>(builder.Configuration.GetSection("EventProcessor"));

// DI
builder.Services.AddSingleton<EventChannel>();
builder.Services.AddSingleton<IEventRepository>(sp =>
    new EventRepository(builder.Configuration.GetConnectionString("DefaultConnection")!));
builder.Services.AddHostedService<EventProcessor>();

// Authentication (OpenID Connect / JWT Bearer)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Authorization Policies
builder.Services.AddAuthorizationBuilder()
    // Authorization Policies
    .AddPolicy("TrackerSyncPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            if (string.IsNullOrEmpty(scopeClaim)) return false;
            var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains("tracker:sync");
        }));

var app = builder.Build();

app.UseMetricServer();
app.UseHttpMetrics();

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
    await repository.EnsureTableExistsAsync(CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMetricServer(); // Endpoint /metrics
app.UseHttpMetrics();  // Стандартные метрики ASP.NET Core

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Minimal API Endpoint
app.MapPost("/api/events", async (EventDto eventDto, EventChannel channel, CancellationToken ct) =>
    {
        // Быстро ставим в очередь и возвращаем 202 Accepted
        await channel.AddEventAsync(eventDto, ct);
        return Results.Accepted();
    })
    .WithName("PostEvent")
    .WithSummary("Принимает и сохраняет события в пакетном режиме")
    .RequireAuthorization("TrackerSyncPolicy");

app.Run();