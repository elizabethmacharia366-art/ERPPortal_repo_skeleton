namespace ERPPortal.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}

