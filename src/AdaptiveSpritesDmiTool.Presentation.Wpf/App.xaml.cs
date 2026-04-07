using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;
using AdaptiveSpritesDmiTool.Infrastructure.Configs;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using AdaptiveSpritesDmiTool.Infrastructure.Preview;
using AdaptiveSpritesDmiTool.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterGlobalExceptionHandlers();
        base.OnStartup(e);

        try
        {
            _host = BuildHost();
            _host.StartAsync().GetAwaiter().GetResult();
            _logger = _host.Services.GetRequiredService<ILogger<App>>();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

            MainWindow = mainWindow;
            mainWindow.Show();
            _ = InitializeMainWindowAsync(mainWindow, viewModel);
        }
        catch (Exception exception)
        {
            HandleFatalException("Application startup failed.", exception);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                var viewModel = _host.Services.GetService<MainWindowViewModel>();
                viewModel?.PersistWorkspaceSettingsAsync().GetAwaiter().GetResult();

                _host.StopAsync().GetAwaiter().GetResult();
                _host.Dispose();
            }
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "Shutdown encountered an error.");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private static IHost BuildHost() =>
        Host
            .CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddProvider(new SimpleFileLoggerProvider(AppStoragePaths.LogFilePath));
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<EditorSession>();
                services.AddSingleton<IWorkspaceService, EditorWorkspaceService>();
                services.AddSingleton<IConfigRepository, JsonSpriteConfigRepository>();
                services.AddSingleton<ILegacyCsvConfigImporter, LegacyCsvConfigImporter>();
                services.AddSingleton<IDmiReader, DmiSharpReader>();
                services.AddSingleton<IDmiWriter, DmiSharpConfigWriter>();
                services.AddSingleton<IPreviewBuilder, DmiSharpPreviewBuilder>();
                services.AddSingleton<IBatchProcessingService, DeterministicBatchProcessingService>();
                services.AddSingleton<ISettingsRepository>(_ => new JsonWorkspaceSettingsRepository(AppStoragePaths.SettingsFilePath));
                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<StartEmptyWorkspaceUseCase>();
                services.AddSingleton<CreateConfigUseCase>();
                services.AddSingleton<SaveConfigUseCase>();
                services.AddSingleton<LoadConfigUseCase>();
                services.AddSingleton<ImportLegacyCsvConfigUseCase>();
                services.AddSingleton<LoadDmiFileUseCase>();
                services.AddSingleton<BuildPreviewUseCase>();
                services.AddSingleton<ApplyConfigToDmiBatchUseCase>();
                services.AddSingleton<UndoChangeUseCase>();
                services.AddSingleton<RedoChangeUseCase>();
                services.AddSingleton<SetPreviewSelectionUseCase>();
                services.AddSingleton<SetSelectedDirectionUseCase>();
                services.AddSingleton<UpsertPixelMappingUseCase>();
                services.AddSingleton<RemovePixelMappingUseCase>();
                services.AddSingleton<ApplyConfigTransformUseCase>();
                services.AddSingleton<LoadWorkspaceSettingsUseCase>();
                services.AddSingleton<SaveWorkspaceSettingsUseCase>();
                services.AddSingleton<SpriteImageBitmapSourceFactory>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

    private async Task InitializeMainWindowAsync(MainWindow mainWindow, MainWindowViewModel viewModel)
    {
        mainWindow.IsEnabled = false;

        try
        {
            await viewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            HandleFatalException("Application startup failed.", exception);
        }
        finally
        {
            if (mainWindow.IsLoaded)
            {
                mainWindow.IsEnabled = true;
            }
        }
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        HandleFatalException("An unexpected UI error occurred.", e.Exception);
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException("An unknown fatal error occurred.");

        HandleFatalException("A fatal application error occurred.", exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();

#if DEBUG
        Debug.WriteLine($"[App] OnUnobservedTaskException thread={Environment.CurrentManagedThreadId} dispatcherAccess={Dispatcher.CheckAccess()}");
#endif

        HandleFatalException("A background task failed unexpectedly.", e.Exception);
    }

    private void HandleFatalException(string message, Exception exception)
    {
        _logger?.LogCritical(exception, message);

#if DEBUG
        Debug.WriteLine($"[App] HandleFatalException thread={Environment.CurrentManagedThreadId} dispatcherAccess={Dispatcher.CheckAccess()} message={message}");
#endif

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => HandleFatalExceptionOnUiThread(message, exception));
            return;
        }

        HandleFatalExceptionOnUiThread(message, exception);
    }

    private void HandleFatalExceptionOnUiThread(string message, Exception exception)
    {
        _logger?.LogCritical(exception, message);

        try
        {
            System.Windows.MessageBox.Show(
                $"{message}{Environment.NewLine}{Environment.NewLine}{exception.Message}{Environment.NewLine}{Environment.NewLine}Log: {AppStoragePaths.LogFilePath}",
                "Adaptive Sprites DMI Tool",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
#if DEBUG
            Debug.WriteLine($"[App] Shutdown on UI thread={Environment.CurrentManagedThreadId}");
#endif
            Shutdown(-1);
        }
    }
}
