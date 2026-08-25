using ERPPortal.Domain.Entities;

namespace ERPPortal.Application.Services;

public class EmployeeSummaryService
{
    public string Describe(Employee employee)
    {
        return $"{employee.FullName} (active: {employee.IsActive})";
    }
}
