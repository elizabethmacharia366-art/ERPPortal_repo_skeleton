namespace ERPPortal.Application.Interfaces;

/// <summary>
/// Resolves the authenticated caller's identity and permission set, backed by
/// OUR Users/UserRoles/RolePermissions tables — not by claims baked into the
/// JWT. This means permission changes take effect immediately, without
/// waiting for the user's token to expire and be reissued.
/// </summary>
public interface ICurrentUserService
{
    string? KeycloakSubjectId { get; }
    bool IsAuthenticated { get; }

    Task<IReadOnlySet<string>> GetPermissionsAsync(CancellationToken ct = default);
    Task<bool> HasPermissionAsync(string permissionName, CancellationToken ct = default);
}
