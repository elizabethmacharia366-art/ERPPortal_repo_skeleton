namespace ERPPortal.Domain.Entities;

/// <summary>
/// Represents a person in OUR system. NOT the same as a Keycloak user record —
/// this row is linked to Keycloak via KeycloakSubjectId (the "sub" claim from
/// the JWT). Keycloak owns authentication; this table owns authorization
/// (which roles/permissions this person has within ERPPortal).
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>The "sub" claim from Keycloak's JWT — links this row to a real identity.</summary>
    public string KeycloakSubjectId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
