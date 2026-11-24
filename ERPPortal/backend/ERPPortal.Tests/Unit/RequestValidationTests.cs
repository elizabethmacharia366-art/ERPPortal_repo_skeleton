using ERPPortal.Application.DTOs;

namespace ERPPortal.Tests.Unit;

public class RequestValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRole_RejectsBlankName(string name)
    {
        var errors = new CreateRoleRequest(name, null).Validate();
        Assert.Contains("name", errors.Keys);
    }

    [Fact]
    public void CreateRole_RejectsNameOver100Characters()
    {
        var errors = new CreateRoleRequest(new string('a', 101), null).Validate();
        Assert.Contains("name", errors.Keys);
    }

    [Fact]
    public void CreateRole_AcceptsValidRequest()
    {
        var errors = new CreateRoleRequest("Finance", "Views reports").Validate();
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Leave.Approve")]
    [InlineData("Roles.Manage")]
    [InlineData("Payroll.ViewAll")]
    public void CreatePermission_AcceptsModuleDotActionNames(string name)
    {
        var errors = new CreatePermissionRequest(name, "Module", null).Validate();
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("bad name")]
    [InlineData("LeaveApprove")]
    [InlineData("Leave.")]
    [InlineData("Leave.Approve.Extra")]
    [InlineData("Leave.Approve!")]
    public void CreatePermission_RejectsNamesNotInModuleDotActionForm(string name)
    {
        var errors = new CreatePermissionRequest(name, "Module", null).Validate();
        Assert.Contains("name", errors.Keys);
    }

    [Fact]
    public void CreatePermission_RequiresModule()
    {
        var errors = new CreatePermissionRequest("Leave.Approve", "", null).Validate();
        Assert.Contains("module", errors.Keys);
    }
}
