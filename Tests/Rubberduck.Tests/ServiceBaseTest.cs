using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Rubberduck.Tests
{

    [TestClass]
    public abstract class ServiceBaseTest
    {
        protected IServiceProvider Services { get; private set; } = null!;
        protected Dictionary<Type, object> Mocks { get; private set; } = [];

        protected virtual IEnumerable<(Type, object)> ConfigureMocking() => [
            (typeof(IPath), Substitute.For<IPath>()),
            (typeof(IFileSystem), Substitute.For<IFileSystem>())
        ];

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            var mockPath = Substitute.For<IPath>();
            mockPath.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var parts = callInfo.Args().Select(arg => arg?.ToString() ?? string.Empty);
                return string.Join(System.IO.Path.DirectorySeparatorChar, parts);
            });
            var mockFS = Substitute.For<IFileSystem>();
            mockFS.Path.Returns(mockPath);

            if (!Mocks.ContainsKey(typeof(IFileSystem)))
            {
                Mocks[typeof(IPath)] = mockPath;
                Mocks[typeof(IFileSystem)] = mockFS;
                Mocks[typeof(ILanguageServer)] = Substitute.For<ILanguageServer>();
            }

            services.AddLogging();
            services.AddSingleton<ILogger>(provider => new TestLogger());
            services.AddSingleton(provider => (IFileSystem)Mocks[typeof(IFileSystem)]);
            services.AddSingleton(provider => (ILanguageServer)Mocks[typeof(ILanguageServer)]);
            services.AddSingleton<RubberduckSettingsProvider>();
            services.AddSingleton<IDefaultSettingsProvider<RubberduckSettings>>(provider => RubberduckSettings.Default);
            services.AddSingleton<PerformanceRecordAggregator>();
        }

        [TestInitialize]
        public virtual void OnInitialize()
        {
            var services = new ServiceCollection();
            Mocks = ConfigureMocking().ToDictionary();
            ConfigureServices(services);

            Services = services.BuildServiceProvider();
        }

        [TestCleanup]
        public virtual void OnCleanup()
        {
            Mocks.Clear();
            Mocks = null!;

            if (Services is IDisposable disposable)
            {
                disposable.Dispose();
            }
            Services = null!;
        }
    }
}
