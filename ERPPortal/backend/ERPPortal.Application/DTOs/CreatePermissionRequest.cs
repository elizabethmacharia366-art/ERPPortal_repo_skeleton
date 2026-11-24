using System.Text.RegularExpressions;

namespace ERPPortal.Application.DTOs;

public record CreatePermissionRequest(string Name, string Module, string? Description)
{
    // Permission names follow the "Module.Action" convention, e.g. "Leave.Approve".
    private static readonly Regex NamePattern = new(@"^[A-Za-z]+\.[A-Za-z]+$");

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Name))
            errors["name"] = new[] { "Permission name is required." };
        else if (!NamePattern.IsMatch(Name))
            errors["name"] = new[] { "Permission name must look like 'Module.Action', e.g. 'Leave.Approve'." };

        if (string.IsNullOrWhiteSpace(Module))
            errors["module"] = new[] { "Module is required." };

        if (Description is { Length: > 500 })
            errors["description"] = new[] { "Description must be 500 characters or fewer." };

        return errors;
    }
}
