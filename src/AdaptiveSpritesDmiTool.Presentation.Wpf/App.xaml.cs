using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Infrastructure.Configs;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using AdaptiveSpritesDmiTool.Infrastructure.Preview;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<EditorSession>();
                services.AddSingleton<IWorkspaceService, EditorWorkspaceService>();
                services.AddSingleton<IConfigRepository, JsonSpriteConfigRepository>();
                services.AddSingleton<ILegacyCsvConfigImporter, LegacyCsvConfigImporter>();
                services.AddSingleton<IDmiReader, DmiSharpReader>();
                services.AddSingleton<IPreviewBuilder, DmiSharpPreviewBuilder>();
                services.AddSingleton<StartEmptyWorkspaceUseCase>();
                services.AddSingleton<CreateConfigUseCase>();
                services.AddSingleton<SaveConfigUseCase>();
                services.AddSingleton<LoadConfigUseCase>();
                services.AddSingleton<ImportLegacyCsvConfigUseCase>();
                services.AddSingleton<LoadDmiFileUseCase>();
                services.AddSingleton<BuildPreviewUseCase>();
                services.AddSingleton<SpriteImageBitmapSourceFactory>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.StartAsync().GetAwaiter().GetResult();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
        MainWindow = mainWindow;
        viewModel.Initialize();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}