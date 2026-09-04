using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ERPPortal.Application.DTOs;
using ERPPortal.Application.Interfaces;
using ERPPortal.Domain.Entities;
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

// --- Admin: role & permission management -----------------------------------

app.MapGet("/api/v1/admin/roles", async (ICurrentUserService currentUser, AppDbContext db) =>
{
    if (!await currentUser.HasPermissionAsync("Roles.Manage"))
        return Results.Forbid();

    var roles = await db.Roles
        .Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Description,
            r.RolePermissions.Select(rp => rp.Permission.Name).ToList()))
        .ToListAsync();

    return Results.Ok(roles);
}).RequireAuthorization();

app.MapPost("/api/v1/admin/users/{userId:guid}/roles/{roleId:guid}", async (
    Guid userId, Guid roleId, ICurrentUserService currentUser, AppDbContext db) =>
{
    if (!await currentUser.HasPermissionAsync("Roles.Manage"))
        return Results.Forbid();

    var userExists = await db.Users.AnyAsync(u => u.Id == userId);
    var roleExists = await db.Roles.AnyAsync(r => r.Id == roleId);
    if (!userExists || !roleExists)
        return Results.NotFound();

    var alreadyAssigned = await db.UserRoles
        .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
    if (alreadyAssigned)
        return Results.Ok(new { message = "Role already assigned." });

    db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Role assigned successfully." });
}).RequireAuthorization();

app.Run();
