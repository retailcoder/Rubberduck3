using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rubberduck.InternalApi.Extensions;
using System;

using SUT = Rubberduck.InternalApi.Extensions.WorkspaceFolderUri;
namespace Rubberduck.Tests.DocumentUri;

[TestClass]
public class WorkspaceFolderUriTests
{
    private static readonly string _workspaceRootPath = "C:\\Dev\\VBA\\Workspaces\\TestProject1";
    private static readonly Uri _workspaceRoot = new(_workspaceRootPath);

    [TestMethod]
    [TestCategory("WorkspaceUri")]
    public void GivenNullRelativeString_IsWorkspaceRoot()
    {
        var expected_relative = string.Empty;
        var expected_absolute = $"{_workspaceRootPath}\\.src";
        
        var result = new SUT(null, _workspaceRoot);

        Assert.IsFalse(result.IsAbsoluteUri);
        Assert.IsTrue(result.IsSrcRoot);
        Assert.AreEqual(expected_relative, result.ToString());
        Assert.AreEqual(expected_absolute, result.AbsoluteLocation.LocalPath);
    }

    [TestMethod]
    [TestCategory("WorkspaceUri")]
    public void GivenAbsoluteSourceRootString_IsRelativeSrcRoot()
    {
        var expected_relative = string.Empty;
        var expected_absolute = $"{_workspaceRootPath}\\.src";

        var absolute = $"{_workspaceRoot}\\{WorkspaceUri.SourceRootName}";

        var result = new SUT(absolute, _workspaceRoot);

        Assert.IsFalse(result.IsAbsoluteUri);
        Assert.IsTrue(result.IsSrcRoot);

        Assert.AreEqual(expected_relative, result.ToString());
        Assert.AreEqual(expected_absolute, result.AbsoluteLocation.LocalPath);
    }

    [TestMethod]
    [TestCategory("WorkspaceUri")]
    public void GivenAbsoluteWorkspaceRootString_IsRelativeSrcRoot()
    {
        var expected_relative = string.Empty;
        var expected_absolute = $"{_workspaceRootPath}\\.src";

        var absolute = $"{_workspaceRoot}";

        var result = new SUT(absolute, _workspaceRoot);

        Assert.IsFalse(result.IsAbsoluteUri);
        Assert.IsTrue(result.IsSrcRoot);

        Assert.AreEqual(expected_relative, result.ToString());
        Assert.AreEqual(expected_absolute, result.AbsoluteLocation.LocalPath);
    }
}
