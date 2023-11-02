﻿//using IndenterSettings = Rubberduck.SmartIndenter.IndenterSettings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Rubberduck.Common;
using Rubberduck.Core;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Settings;
using Rubberduck.Main.About;
using Rubberduck.Main.Commands.ShowRubberduckEditor;
using Rubberduck.Main.RPC.EditorServer;
using Rubberduck.Main.Settings;
using Rubberduck.SettingsProvider;
using Rubberduck.SettingsProvider.Model;
using Rubberduck.SettingsProvider.Model.General;
using Rubberduck.SettingsProvider.Model.LanguageServer;
using Rubberduck.SettingsProvider.Model.TelemetryServer;
using Rubberduck.SettingsProvider.Model.UpdateServer;
using Rubberduck.UI.About;
using Rubberduck.UI.Command;
using Rubberduck.UI.Message;
using Rubberduck.Unmanaged;
using Rubberduck.Unmanaged.Abstract;
using Rubberduck.Unmanaged.Abstract.SafeComWrappers.Office;
using Rubberduck.Unmanaged.Abstract.SafeComWrappers.VB;
using Rubberduck.Unmanaged.Events;
using Rubberduck.Unmanaged.NonDisposingDecorators;
using Rubberduck.Unmanaged.TypeLibs.Abstract;
using Rubberduck.Unmanaged.TypeLibs.Public;
using Rubberduck.Unmanaged.UIContext;
using Rubberduck.Unmanaged.VBERuntime;
using Rubberduck.VBEditor.UI.OfficeMenus;
using Rubberduck.VBEditor.UI.OfficeMenus.RubberduckMenu;
using System;
using System.IO.Abstractions;
using System.Reflection;

namespace Rubberduck.Main.Root
{
    internal class RubberduckServicesBuilder
    {
        private readonly IServiceCollection _services = new ServiceCollection();

        public RubberduckServicesBuilder(IVBE vbe, IAddIn addin)
        {
            Configure(vbe, addin);
        }

        public IServiceProvider Build() => _services.BuildServiceProvider();
        private void Configure(IVBE vbe, IAddIn addin)
        {
            _services.AddLogging(ConfigureLogging);
            _services.AddSingleton<App>();

            this.WithAssemblyInfo()
                .WithNativeServices(vbe, addin)
                .WithSettingsProviders()
                .WithRubberduckMenu(vbe)
                .WithServices()
                .WithRubberduckEditorServer();
        }

        private void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.AddNLog("NLog-client.config");

            _services.AddSingleton<ILogLevelService, LogLevelService>();
        }

        private RubberduckServicesBuilder WithAssemblyInfo()
        {
            _services.AddSingleton(provider => Assembly.GetExecutingAssembly().GetName().Version!);
            _services.AddSingleton<IOperatingSystem, WindowsOperatingSystem>();

            return this;
        }

        private RubberduckServicesBuilder WithNativeServices(IVBE vbe, IAddIn addin)
        {
            _services.AddSingleton(provider => vbe);
            _services.AddSingleton(provider => addin);
            _services.AddSingleton(provider => vbe.TempSourceFileHandler);

            _services.AddSingleton<IProjectsRepository>(provider => new ProjectsRepository(vbe, provider.GetRequiredService<ILogger<ProjectsRepository>>()));
            _services.AddSingleton<IProjectsProvider>(provider => provider.GetRequiredService<IProjectsRepository>());

            var nativeApi = new VbeNativeApiAccessor();
            _services.AddSingleton<IVbeNativeApi>(provider => nativeApi);
            _services.AddSingleton<IVbeEvents>(provider => VbeEvents.Initialize(vbe));
            _services.AddSingleton<IVBETypeLibsAPI, VBETypeLibsAPI>();

            #region still needed?
            _services.AddSingleton<IUiDispatcher, UiDispatcher>();
            _services.AddSingleton<IUiContextProvider>(provider => UiContextProvider.Instance());
            #endregion

            return this;
        }

        private RubberduckServicesBuilder WithSettingsProviders()
        {
            // ISettingsService<TSettings> provides file I/O
            _services.AddSingleton<ISettingsService<RubberduckSettings>, SettingsService<RubberduckSettings>>();

            // IDefaultSettingsProvider<TSettings> provide the default configuration settings for injectable setting groups
            _services.AddSingleton<IDefaultSettingsProvider<RubberduckSettings>>(provider => RubberduckSettings.Default);
            _services.AddSingleton<IDefaultSettingsProvider<GeneralSettings>>(provider => GeneralSettings.Default);
            _services.AddSingleton<IDefaultSettingsProvider<LanguageServerSettings>>(provider => LanguageServerSettings.Default);
            _services.AddSingleton<IDefaultSettingsProvider<UpdateServerSettings>>(provider => UpdateServerSettings.Default);
            _services.AddSingleton<IDefaultSettingsProvider<TelemetryServerSettings>>(provider => TelemetryServerSettings.Default);

            // ISettingsProvider<TSettings> provide current applicable settings for injectable setting groups
            _services.AddTransient<ISettingsProvider<RubberduckSettings>, SettingsService<RubberduckSettings>>();
            _services.AddTransient<ISettingsProvider<GeneralSettings>, SettingsService<GeneralSettings>>();
            _services.AddTransient<ISettingsProvider<LanguageServerSettings>, SettingsService<LanguageServerSettings>>();
            _services.AddTransient<ISettingsProvider<UpdateServerSettings>, SettingsService<UpdateServerSettings>>();
            _services.AddTransient<ISettingsProvider<TelemetryServerSettings>, SettingsService<TelemetryServerSettings>>();

            return this;
        }

        private RubberduckServicesBuilder WithRubberduckMenu(IVBE vbe)
        {
            _services.AddSingleton<ICommandBars>(provider => new CommandBarsNonDisposingDecorator<ICommandBars>(vbe.CommandBars));

            _services.AddSingleton<RubberduckParentMenu>();

            _services.AddSingleton<IAboutCommand, AboutCommand>();
            _services.AddSingleton<AboutCommandMenuItem>();
            _services.AddSingleton<AboutService>();
            _services.AddSingleton<IAboutWindowViewModel, AboutWindowViewModel>();

            _services.AddSingleton<IShowRubberduckEditorCommand, ShowRubberduckEditorCommand>();
            _services.AddSingleton<ShowRubberduckEditorCommandMenuItem>();
            _services.AddSingleton<IEditorServerProcessService, EditorServerProcessService>();

            _services.AddSingleton<ISettingsCommand, SettingsCommand>();
            _services.AddSingleton<SettingsCommandMenuItem>();
            
            _services.AddSingleton(ConfigureRubberduckMenu);
            return this;
        }

        private static IRubberduckMenu ConfigureRubberduckMenu(IServiceProvider services)
        {
            var addin = services.GetRequiredService<IAddIn>();
            var vbe = services.GetRequiredService<IVBE>();

            var location = addin.CommandBarLocations[CommandBarSite.MenuBar];
            var builder = new CommandBarMenuBuilder<RubberduckParentMenu>(location, services, MainCommandBarControls(vbe, location.ParentId))
                .WithCommandMenuItem<ShowRubberduckEditorCommandMenuItem>()
                .WithSeparator()
                .WithCommandMenuItem<SettingsCommandMenuItem>()
                .WithCommandMenuItem<AboutCommandMenuItem>();

            return builder.Build();
        }

        private static ICommandBarControls MainCommandBarControls(IVBE vbe, int commandBarIndex)
        {
            ICommandBarControls controls;
            using (var commandBars = vbe.CommandBars)
            {
                using var menuBar = commandBars[commandBarIndex];
                controls = menuBar.Controls;
            }
            return controls;
        }


        private RubberduckServicesBuilder WithServices()
        {
            _services.AddSingleton<IFileSystem, FileSystem>();

            _services.AddSingleton<IMessageService, MessageService>();
            _services.AddSingleton<IMessageWindowFactory, MessageWindowFactory>();
            _services.AddSingleton<MessageActionsProvider>();

            _services.AddSingleton<IEditorServerProcessService, EditorServerProcessService>();

            return this;
        }

        private RubberduckServicesBuilder WithRubberduckEditorServer()
        {

            return this;
        }
    }
}