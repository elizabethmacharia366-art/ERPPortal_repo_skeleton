using System.Security.Claims;
using ERPPortal.Application.Interfaces;
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
