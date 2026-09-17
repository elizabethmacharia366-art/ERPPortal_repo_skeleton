using System.Security.Claims;
using ERPPortal.Application.Interfaces;
using ERPPortal.Domain.Entities;
using ERPPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ERPPortal.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    // Keycloak puts the user's unique ID in the standard "sub" claim.
    public string? KeycloakSubjectId => User?.FindFirst("sub")?.Value;

    /// <summary>
    /// "Just-in-time" provisioning: if this is the first time we've seen this
    /// Keycloak identity, create a matching row in our own Users table using
    /// claims already present in their JWT. Safe to call on every request —
    /// it's a no-op once the user already exists.
    /// </summary>
    public async Task EnsureUserProvisionedAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated || KeycloakSubjectId is null)
            return;

        var exists = await _db.Users.AnyAsync(u => u.KeycloakSubjectId == KeycloakSubjectId, ct);
        if (exists)
            return;

        var email = User?.FindFirst("email")?.Value ?? string.Empty;
        var displayName = User?.FindFirst("name")?.Value
            ?? User?.FindFirst("preferred_username")?.Value
            ?? email;

        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            KeycloakSubjectId = KeycloakSubjectId,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated || KeycloakSubjectId is null)
            return new HashSet<string>();

        var permissions = await _db.Users
            .Where(u => u.KeycloakSubjectId == KeycloakSubjectId && u.IsActive)
            .SelectMany(u => u.UserRoles)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(ct);

        return permissions.ToHashSet();
    }

    public async Task<bool> HasPermissionAsync(string permissionName, CancellationToken ct = default)
    {
        var permissions = await GetPermissionsAsync(ct);
        return permissions.Contains(permissionName);
    }
}
