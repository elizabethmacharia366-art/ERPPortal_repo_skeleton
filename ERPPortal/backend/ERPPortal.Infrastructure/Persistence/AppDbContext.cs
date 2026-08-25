using ERPPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPPortal.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
}
