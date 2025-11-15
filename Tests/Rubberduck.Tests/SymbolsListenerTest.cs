using Antlr4.Runtime.Tree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model;
using Rubberduck.Parsing._v3.Pipeline.Services;
using Rubberduck.Parsing.Abstract;
using Rubberduck.Parsing.Exceptions;
using Rubberduck.Parsing.Parsers;
using Rubberduck.Parsing.TokenStreamProviders;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;

namespace Rubberduck.Tests;

public abstract class ListenerBaseTest : ServiceBaseTest
{
    protected override IEnumerable<(Type, object)> ConfigureMocking()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var tokenStreamParserLogger = Substitute.For<ILogger<ITokenStreamParser>>();
        var performanceLogger = Substitute.For<ILogger<PerformanceRecordAggregator>>();
        var settingsLogger = Substitute.For<ILogger<SettingsService<RubberduckSettings>>>();
        var appWorkspacesManager = Substitute.For<IAppWorkspacesStateManager>();
        var errorMessageService = Substitute.For<ISyntaxErrorMessageService>();
        var settingsProvider = new RubberduckSettingsProvider(settingsLogger, fileSystem, RubberduckSettings.Default);

        var performanceAggregator = new PerformanceRecordAggregator(performanceLogger, settingsProvider);

        var tokenStreamProvider = new StringTokenStreamProvider();
        var tokenStreamParser = new VBATokenStreamParser(errorMessageService, tokenStreamParserLogger, settingsProvider, performanceAggregator);
        var parser = new TokenStreamParserStringParserAdapter(tokenStreamProvider, tokenStreamParser);

        return
        [
            (typeof(IParser<string>), parser),
        ];
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped(provider => (IParser<string>)Mocks[typeof(IParser<string>)]);
        base.ConfigureServices(services);
    }

    protected IParseTree Parse(WorkspaceFileUri uri, string content, CancellationToken token, IEnumerable<IParseTreeListener>? listeners = null)
    {
        var parser = Services.GetRequiredService<IParser<string>>();
        return parser.Parse(uri, content, token, parseListeners: listeners ?? []).SyntaxTree;
    }
}

[TestClass]
public class SymbolListenerTest : ListenerBaseTest
{
    [TestMethod]
    public void Test1()
    {
        // arrange
        var moduleName = "Module1";
        var procedureName = "DoSomething";

        var uri = new WorkspaceFileUri($"{moduleName}.bas", new("file://root"));
        var code = @$"Option Explicit

Public Sub {procedureName}(ByVal X As Long, ByVal Y As Long)
    Debug.Print (X + Y)
End Sub";

        var logger = Substitute.For<ILogger<MemberSymbolsListener>>();
        var listener = new MemberSymbolsListener(logger, uri);

        // act
        var tree = Parse(uri, code, CancellationToken.None, [listener]);
        var symbol = listener.Result;

        var procedure = symbol?.Children?.OfType<ProcedureSymbol>().SingleOrDefault(e => e.Name == procedureName);

        // assert
        Assert.IsNotNull(symbol);
        Assert.AreEqual(moduleName, symbol.Name);

        Assert.IsNotNull(procedure);
    }
}