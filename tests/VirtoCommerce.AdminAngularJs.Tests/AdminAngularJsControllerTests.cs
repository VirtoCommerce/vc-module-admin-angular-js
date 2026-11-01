using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtoCommerce.AdminAngularJs.Web.Controllers;
using VirtoCommerce.AdminAngularJs.Web.Models;
using VirtoCommerce.Platform.Core;
using Xunit;

namespace VirtoCommerce.AdminAngularJs.Tests;

[Trait("Category", "Unit")]
public class AdminAngularJsControllerTests
{
    [Fact]
    public async Task Index_PlatformUIDisabled_Forbids()
    {
        var controller = CreateController(new AdminUIContext(), enable: false);

        var result = await controller.Index();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_NoLicenseNoDemo_RendersDefaults()
    {
        var context = new AdminUIContext
        {
            PlatformVersion = "3.1079.0",
            AllowInstallModules = false,
            ForceWebSockets = true,
            EnvironmentBanner = new EnvironmentBannerOptions { Enable = true, Name = "QA", ThemePreset = "orange" },
        };
        var controller = CreateController(context);

        var model = GetModel(await controller.Index());

        Assert.Equal("3.1079.0", model.PlatformVersion.Value);
        Assert.Equal("''", model.DemoCredentials.Value);
        Assert.Equal("''", model.DemoResetTime.Value);
        Assert.Null(model.License);
        Assert.True(model.SendDiagnosticData);
        Assert.False(model.RefreshProbingFolder);
        Assert.True(model.ForceWebSockets);
        Assert.True(model.ShowEnvironmentBanner);
        Assert.Equal("QA", model.EnvironmentName.Value);
        Assert.Equal("orange", model.EnvironmentTheme.Value);
    }

    [Fact]
    public async Task Index_LicenseAndDemo_RendersJavaScriptLiteralsWithSingleQuotes()
    {
        var context = new AdminUIContext
        {
            DemoCredentials = "{ userName: 'admin', password: 'store' }",
            DemoResetTime = new DateTime(2026, 10, 14, 3, 0, 0, DateTimeKind.Utc),
            License = new { Type = "Commercial", CustomerName = "Acme", ExpirationDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            SendDiagnosticData = false,
        };
        var controller = CreateController(context);

        var model = GetModel(await controller.Index());

        // The values go verbatim into the ng-init attribute, so they must not contain double quotes
        Assert.Equal("{ userName: 'admin', password: 'store' }", model.DemoCredentials.Value);
        Assert.Equal("'2026-10-14T03:00:00Z'", model.DemoResetTime.Value);
        Assert.Equal("{'type':'Commercial','customerName':'Acme','expirationDate':'2027-01-01T00:00:00Z'}", model.License.Value);
        Assert.False(model.SendDiagnosticData);
    }

    [Theory]
    [InlineData("", null, "/")]
    [InlineData("", "/apps/platform", "/apps/platform/")]
    [InlineData("/vc", "/apps/platform", "/vc/apps/platform/")]
    [InlineData("/vc", null, "/vc/")]
    public async Task Index_BaseHref_FollowsThePathBaseAndTheMountPath(string pathBase, string mountPath, string expected)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = pathBase;
        if (mountPath != null)
        {
            httpContext.Features.Set(new AdminUIRequestFeature(Web.Module.AppId, mountPath));
        }
        var controller = CreateController(new AdminUIContext(), httpContext: httpContext);

        var model = GetModel(await controller.Index());

        Assert.Equal(expected, model.BaseHref);
    }

    private static AdminAngularJsController CreateController(AdminUIContext context, bool enable = true, HttpContext httpContext = null)
    {
        return new AdminAngularJsController(Options.Create(new PlatformUIOptions { Enable = enable }), new FakeContextProvider(context))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext ?? new DefaultHttpContext() },
        };
    }

    private static AdminAngularJsIndexModel GetModel(ActionResult result)
    {
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<AdminAngularJsIndexModel>(view.Model);
    }

    private sealed class FakeContextProvider(AdminUIContext context) : IAdminUIContextProvider
    {
        public Task<AdminUIContext> GetContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(context);
    }
}
