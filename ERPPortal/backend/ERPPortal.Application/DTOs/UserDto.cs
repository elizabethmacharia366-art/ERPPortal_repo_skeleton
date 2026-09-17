namespace ERPPortal.Application.DTOs;

public record UserDto(Guid Id, string Email, string DisplayName, bool IsActive, List<string> Roles);
