using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using VirtoCommerce.AdminAngularJs.Web;
using VirtoCommerce.Platform.Core.Modularity;
using Xunit;

namespace VirtoCommerce.AdminAngularJs.Tests;

[Trait("Category", "Unit")]
public sealed class ModuleTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "admin-angularjs-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _platformWebRoot;
    private readonly string _moduleRoot;

    public ModuleTests()
    {
        _platformWebRoot = Directory.CreateDirectory(Path.Combine(_root, "platform", "wwwroot")).FullName;
        _moduleRoot = Directory.CreateDirectory(Path.Combine(_root, "modules", "VirtoCommerce.AdminAngularJs")).FullName;
    }

    [Fact]
    public void Initialize_AddsModuleWebRootToPlatformWebRoot()
    {
        WriteFile(Path.Combine(_platformWebRoot, "robots.txt"), "platform");
        WriteFile(Path.Combine(_moduleRoot, "Content", "wwwroot", "dist", "app.js"), "app");
        var environment = new FakeWebHostEnvironment { WebRootFileProvider = new PhysicalFileProvider(_platformWebRoot) };

        CreateModule(environment).Initialize(new ServiceCollection());

        Assert.True(environment.WebRootFileProvider.GetFileInfo("robots.txt").Exists);
        Assert.True(environment.WebRootFileProvider.GetFileInfo("dist/app.js").Exists);
    }

    [Fact]
    public void Initialize_SameFileInBothWebRoots_PlatformFileWins()
    {
        WriteFile(Path.Combine(_platformWebRoot, "favicon.ico"), "platform");
        WriteFile(Path.Combine(_moduleRoot, "Content", "wwwroot", "favicon.ico"), "module!");
        var environment = new FakeWebHostEnvironment { WebRootFileProvider = new PhysicalFileProvider(_platformWebRoot) };

        CreateModule(environment).Initialize(new ServiceCollection());

        Assert.Equal("platform".Length, environment.WebRootFileProvider.GetFileInfo("favicon.ico").Length);
    }

    [Fact]
    public void Initialize_ModuleWithoutWebRoot_KeepsPlatformWebRootWorking()
    {
        WriteFile(Path.Combine(_platformWebRoot, "robots.txt"), "platform");
        var environment = new FakeWebHostEnvironment { WebRootFileProvider = new PhysicalFileProvider(_platformWebRoot) };

        CreateModule(environment).Initialize(new ServiceCollection());

        Assert.True(environment.WebRootFileProvider.GetFileInfo("robots.txt").Exists);
        Assert.False(environment.WebRootFileProvider.GetFileInfo("dist/app.js").Exists);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private Module CreateModule(IWebHostEnvironment environment)
    {
        return new Module
        {
            ModuleInfo = new ManifestModuleInfo { FullPhysicalPath = _moduleRoot },
            HostEnvironment = environment,
        };
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
    }
}
