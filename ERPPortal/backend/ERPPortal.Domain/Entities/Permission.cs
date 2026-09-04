namespace ERPPortal.Domain.Entities;

public class Permission
{
    public Guid Id { get; set; }

    /// <summary>Matches the policy name used in [Authorize(Policy = "...")] — e.g. "Leave.Approve".</summary>
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
}
