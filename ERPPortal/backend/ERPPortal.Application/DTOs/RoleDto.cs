namespace ERPPortal.Application.DTOs;

public record RoleDto(Guid Id, string Name, string? Description, List<string> Permissions);
