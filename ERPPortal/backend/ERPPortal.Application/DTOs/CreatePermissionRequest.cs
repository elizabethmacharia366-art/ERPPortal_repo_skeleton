namespace ERPPortal.Application.DTOs;

public record CreatePermissionRequest(string Name, string Module, string? Description);
