namespace ERPPortal.Application.DTOs;

public record CreateRoleRequest(string Name, string? Description)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Name))
            errors["name"] = new[] { "Role name is required." };
        else if (Name.Length > 100)
            errors["name"] = new[] { "Role name must be 100 characters or fewer." };

        if (Description is { Length: > 500 })
            errors["description"] = new[] { "Description must be 500 characters or fewer." };

        return errors;
    }
}
