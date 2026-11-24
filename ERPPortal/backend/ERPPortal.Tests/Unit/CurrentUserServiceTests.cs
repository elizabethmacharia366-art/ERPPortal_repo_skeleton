using System.Security.Claims;
using ERPPortal.Domain.Entities;
using ERPPortal.Infrastructure.Identity;
using ERPPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ERPPortal.Tests.Unit;

public class CurrentUserServiceTests
{
    private const string Subject = "subject-123";

    private static DbContextOptions<AppDbContext> NewDatabase() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static CurrentUserService ServiceFor(
        DbContextOptions<AppDbContext> options, string subject, bool authenticated = true)
    {
        var identity = authenticated
            ? new ClaimsIdentity(new[]
              {
                  new Claim("sub", subject),
                  new Claim("email", "a@b.c"),
                  new Claim("name", "Test Person"),
              }, "TestAuth")
            : new ClaimsIdentity();

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return new CurrentUserService(accessor, new AppDbContext(options));
    }

    private static void Seed(DbContextOptions<AppDbContext> options, bool assignRole, bool isActive = true)
    {
        using var db = new AppDbContext(options);
        var user = new User
        {
            Id = Guid.NewGuid(), KeycloakSubjectId = Subject, Email = "a@b.c",
            DisplayName = "Test Person", IsActive = isActive,
        };
        var role = new Role { Id = Guid.NewGuid(), Name = "Manager" };
        var permission = new Permission { Id = Guid.NewGuid(), Name = "Leave.Approve", Module = "Leave" };

        db.AddRange(user, role, permission);
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        if (assignRole)
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.SaveChanges();
    }

    [Fact]
    public async Task UserWithRole_GetsThePermissionsOfThatRole()
    {
        var options = NewDatabase();
        Seed(options, assignRole: true);

        var permissions = await ServiceFor(options, Subject).GetPermissionsAsync();

        Assert.Contains("Leave.Approve", permissions);
    }

    [Fact]
    public async Task UserWithoutRole_GetsNoPermissions()
    {
        var options = NewDatabase();
        Seed(options, assignRole: false);

        var permissions = await ServiceFor(options, Subject).GetPermissionsAsync();

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task InactiveUser_GetsNoPermissions()
    {
        var options = NewDatabase();
        Seed(options, assignRole: true, isActive: false);

        var permissions = await ServiceFor(options, Subject).GetPermissionsAsync();

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task UnknownSubject_GetsNoPermissions()
    {
        var options = NewDatabase();
        Seed(options, assignRole: true);

        var permissions = await ServiceFor(options, "someone-else").GetPermissionsAsync();

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task UnauthenticatedCaller_GetsNoPermissions()
    {
        var options = NewDatabase();
        Seed(options, assignRole: true);

        var permissions = await ServiceFor(options, Subject, authenticated: false).GetPermissionsAsync();

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task HasPermission_IsTrueOnlyForGrantedPermissions()
    {
        var options = NewDatabase();
        Seed(options, assignRole: true);
        var service = ServiceFor(options, Subject);

        Assert.True(await service.HasPermissionAsync("Leave.Approve"));
        Assert.False(await service.HasPermissionAsync("Roles.Manage"));
    }

    [Fact]
    public async Task Provisioning_CreatesTheUserRowExactlyOnce()
    {
        var options = NewDatabase();

        await ServiceFor(options, Subject).EnsureUserProvisionedAsync();
        await ServiceFor(options, Subject).EnsureUserProvisionedAsync();

        using var db = new AppDbContext(options);
        var user = Assert.Single(db.Users.ToList());
        Assert.Equal(Subject, user.KeycloakSubjectId);
        Assert.Equal("a@b.c", user.Email);
        Assert.Equal("Test Person", user.DisplayName);
    }
}
