using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ERPPortal.Application.Interfaces;
using ERPPortal.Infrastructure.Identity;
using ERPPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Serilog;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/erpportal-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "ERPPortal API is running");

app.MapHealthChecks("/health");

app.MapGet("/api/v1/me", async (ClaimsPrincipal claims, ICurrentUserService currentUser) =>
{
    var username = claims.FindFirst("preferred_username")?.Value;
    var email = claims.FindFirst("email")?.Value;
    var permissions = await currentUser.GetPermissionsAsync();
    return Results.Ok(new { username, email, permissions });
}).RequireAuthorization();

app.MapPost("/api/v1/leave/{id}/approve", async (string id, ICurrentUserService currentUser) =>
{
    if (!await currentUser.HasPermissionAsync("Leave.Approve"))
        return Results.Forbid();

    return Results.Ok(new { message = $"Leave request {id} approved." });
}).RequireAuthorization();

app.Run();
