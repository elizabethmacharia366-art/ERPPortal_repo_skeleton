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
    await currentUser.EnsureUserProvisionedAsync();
    
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

app.MapPost("/api/v1/admin/roles/{roleId:guid}/permissions/{permissionId:guid}", async (
    Guid roleId, Guid permissionId, ICurrentUserService currentUser, AppDbContext db) =>
{
    if (!await currentUser.HasPermissionAsync("Roles.Manage"))
        return Results.Forbid();

    var roleExists = await db.Roles.AnyAsync(r => r.Id == roleId);
    var permissionExists = await db.Permissions.AnyAsync(p => p.Id == permissionId);
    if (!roleExists || !permissionExists)
        return Results.NotFound();

    var alreadyLinked = await db.RolePermissions
        .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
    if (alreadyLinked)
        return Results.Ok(new { message = "Permission already granted to this role." });

    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Permission granted to role successfully." });
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
app.MapPost("/api/v1/admin/permissions", async (
    CreatePermissionRequest request, ICurrentUserService currentUser, AppDbContext db) =>
{
    if (!await currentUser.HasPermissionAsync("Roles.Manage"))
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Module))
        return Results.BadRequest(new { error = "Permission name and module are required." });

    var exists = await db.Permissions.AnyAsync(p => p.Name == request.Name);
    if (exists)
        return Results.Conflict(new { error = $"A permission named '{request.Name}' already exists." });

    var permission = new Permission
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Module = request.Module,
        Description = request.Description
    };
    db.Permissions.Add(permission);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/admin/permissions/{permission.Id}", permission);
}).RequireAuthorization();

app.MapPost("/api/v1/admin/roles", async (
    CreateRoleRequest request, ICurrentUserService currentUser, AppDbContext db) =>
{
    if (!await currentUser.HasPermissionAsync("Roles.Manage"))
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Role name is required." });

    var exists = await db.Roles.AnyAsync(r => r.Name == request.Name);
    if (exists)
        return Results.Conflict(new { error = $"A role named '{request.Name}' already exists." });

    var role = new Role { Id = Guid.NewGuid(), Name = request.Name, Description = request.Description };
    db.Roles.Add(role);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/admin/roles/{role.Id}", new RoleDto(role.Id, role.Name, role.Description, new List<string>()));
}).RequireAuthorization();

app.MapGet("/api/v1/admin/users", async (ICurrentUserService currentUser, AppDbContext db) =>
{
    if (!await currentUser.HasPermissionAsync("Roles.Manage"))
        return Results.Forbid();

    var users = await db.Users
        .Select(u => new UserDto(
            u.Id,
            u.Email,
            u.DisplayName,
            u.IsActive,
            u.UserRoles.Select(ur => ur.Role.Name).ToList()))
        .ToListAsync();

    return Results.Ok(users);
}).RequireAuthorization();

app.MapDelete("/api/v1/admin/users/{userId:guid}/roles/{roleId:guid}", async (
    Guid userId, Guid roleId, ICurrentUserService currentUser, AppDbContext db) =>
{
    if (!await currentUser.HasPermissionAsync("Roles.Manage"))
        return Results.Forbid();

    var userRole = await db.UserRoles
        .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

    if (userRole is null)
        return Results.NotFound(new { error = "User does not have this role." });

    db.UserRoles.Remove(userRole);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Role removed from user successfully." });
}).RequireAuthorization();

app.Run();

