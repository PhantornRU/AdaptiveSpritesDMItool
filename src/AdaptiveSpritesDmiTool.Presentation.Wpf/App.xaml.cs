using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;
using AdaptiveSpritesDmiTool.Infrastructure.Configs;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using AdaptiveSpritesDmiTool.Infrastructure.Preview;
using AdaptiveSpritesDmiTool.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private static readonly TimeSpan ShutdownPersistenceTimeout = TimeSpan.FromSeconds(2);

    private IHost? _host;
    private ILogger<App>? _logger;
    private bool _isShutdownPersistenceComplete;
    private bool _isShutdownPersistenceRunning;

    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterGlobalExceptionHandlers();
        base.OnStartup(e);
        ApplyThemeMode(WorkspaceThemeMode.Dark);

        try
        {
            _host = BuildHost();
            _host.StartAsync().GetAwaiter().GetResult();
            _logger = _host.Services.GetRequiredService<ILogger<App>>();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

            MainWindow = mainWindow;
            mainWindow.Closing += OnMainWindowClosing;
            mainWindow.Show();
            _ = InitializeMainWindowAsync(mainWindow, viewModel);
        }
        catch (Exception exception)
        {
            HandleFatalException("Application startup failed.", exception);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                using var cancellationSource = new CancellationTokenSource(ShutdownPersistenceTimeout);
                await _host.StopAsync(cancellationSource.Token);
                _host.Dispose();
                _host = null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Host shutdown was cancelled.");
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

    private async void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_host is null || _isShutdownPersistenceComplete || _isShutdownPersistenceRunning)
        {
            return;
        }

        e.Cancel = true;
        _isShutdownPersistenceRunning = true;

        if (sender is Window window)
        {
            window.IsEnabled = false;
        }

        try
        {
            await PersistWorkspaceSettingsForShutdownAsync();
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Shutdown workspace settings persistence was cancelled.");
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "Shutdown workspace settings persistence failed.");
        }
        finally
        {
            _isShutdownPersistenceComplete = true;
            _isShutdownPersistenceRunning = false;

            if (sender is Window closingWindow)
            {
                closingWindow.Closing -= OnMainWindowClosing;
                closingWindow.Close();
            }
            else
            {
                Shutdown();
            }
        }
    }

    private async Task PersistWorkspaceSettingsForShutdownAsync()
    {
        if (_host?.Services.GetService<MainWindowViewModel>() is not { } viewModel)
        {
            return;
        }

        using var cancellationSource = new CancellationTokenSource(ShutdownPersistenceTimeout);
        await viewModel.PersistWorkspaceSettingsAsync(cancellationSource.Token).ConfigureAwait(false);
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
                services.AddSingleton<IStateFrameReader, DmiSharpStateFrameReader>();
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
                services.AddSingleton<InspectDmiFileUseCase>();
                services.AddSingleton<ReadStateFrameUseCase>();
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

    public static void ApplyThemeMode(WorkspaceThemeMode mode)
    {
        if (Current?.Resources is not ResourceDictionary resources)
        {
            return;
        }

        var palette = mode switch
        {
            WorkspaceThemeMode.Light => new ThemePalette(
                WindowBackground: MediaColor.FromRgb(0xF4, 0xF7, 0xFB),
                PanelBackground: MediaColor.FromRgb(0xFF, 0xFF, 0xFF),
                PanelBorder: MediaColor.FromRgb(0xD5, 0xDE, 0xE8),
                Accent: MediaColor.FromRgb(0x2A, 0x8F, 0x84),
                AccentMuted: MediaColor.FromRgb(0xDB, 0xEF, 0xEA),
                Warning: MediaColor.FromRgb(0xC7, 0x79, 0x24),
                TextPrimary: MediaColor.FromRgb(0x12, 0x18, 0x20),
                TextSecondary: MediaColor.FromRgb(0x56, 0x63, 0x72),
                ControlBackground: MediaColor.FromRgb(0xF5, 0xF8, 0xFB),
                CardBackground: MediaColor.FromRgb(0xFC, 0xFD, 0xFF),
                SurfaceSubtle: MediaColor.FromRgb(0xEB, 0xF1, 0xF7),
                ScrollTrack: MediaColor.FromRgb(0xDF, 0xE7, 0xF0),
                ScrollThumb: MediaColor.FromRgb(0x94, 0xA9, 0xBC),
                ScrollThumbHover: MediaColor.FromRgb(0x7A, 0x93, 0xA7),
                ScrollThumbPressed: MediaColor.FromRgb(0x66, 0x80, 0x95)),
            WorkspaceThemeMode.Warm => new ThemePalette(
                WindowBackground: MediaColor.FromRgb(0xF6, 0xEC, 0xDF),
                PanelBackground: MediaColor.FromRgb(0xFE, 0xF7, 0xEC),
                PanelBorder: MediaColor.FromRgb(0xDB, 0xC8, 0xB1),
                Accent: MediaColor.FromRgb(0x9B, 0x60, 0x34),
                AccentMuted: MediaColor.FromRgb(0xEF, 0xDE, 0xCA),
                Warning: MediaColor.FromRgb(0xC3, 0x7C, 0x37),
                TextPrimary: MediaColor.FromRgb(0x2E, 0x22, 0x18),
                TextSecondary: MediaColor.FromRgb(0x70, 0x59, 0x49),
                ControlBackground: MediaColor.FromRgb(0xFA, 0xF1, 0xE6),
                CardBackground: MediaColor.FromRgb(0xFF, 0xFA, 0xF3),
                SurfaceSubtle: MediaColor.FromRgb(0xF0, 0xE3, 0xD2),
                ScrollTrack: MediaColor.FromRgb(0xE8, 0xD8, 0xC4),
                ScrollThumb: MediaColor.FromRgb(0xB4, 0x87, 0x61),
                ScrollThumbHover: MediaColor.FromRgb(0xA1, 0x73, 0x4B),
                ScrollThumbPressed: MediaColor.FromRgb(0x8E, 0x63, 0x3F)),
            _ => new ThemePalette(
                WindowBackground: MediaColor.FromRgb(0x17, 0x1A, 0x1F),
                PanelBackground: MediaColor.FromRgb(0x1F, 0x25, 0x2D),
                PanelBorder: MediaColor.FromRgb(0x39, 0x42, 0x4D),
                Accent: MediaColor.FromRgb(0x2D, 0x8C, 0x7F),
                AccentMuted: MediaColor.FromRgb(0x2A, 0x4D, 0x49),
                Warning: MediaColor.FromRgb(0xD8, 0x8B, 0x3D),
                TextPrimary: MediaColor.FromRgb(0xF7, 0xF5, 0xF0),
                TextSecondary: MediaColor.FromRgb(0xC2, 0xCC, 0xD6),
                ControlBackground: MediaColor.FromRgb(0x29, 0x31, 0x3B),
                CardBackground: MediaColor.FromRgb(0x26, 0x2E, 0x38),
                SurfaceSubtle: MediaColor.FromRgb(0x22, 0x2A, 0x34),
                ScrollTrack: MediaColor.FromRgb(0x1B, 0x21, 0x29),
                ScrollThumb: MediaColor.FromRgb(0x4A, 0x59, 0x68),
                ScrollThumbHover: MediaColor.FromRgb(0x5D, 0x6F, 0x82),
                ScrollThumbPressed: MediaColor.FromRgb(0x76, 0x91, 0xAC))
        };

        SetBrush(resources, "Brush.WindowBackground", palette.WindowBackground);
        SetBrush(resources, "Brush.PanelBackground", palette.PanelBackground);
        SetBrush(resources, "Brush.PanelBorder", palette.PanelBorder);
        SetBrush(resources, "Brush.Accent", palette.Accent);
        SetBrush(resources, "Brush.AccentMuted", palette.AccentMuted);
        SetBrush(resources, "Brush.Warning", palette.Warning);
        SetBrush(resources, "Brush.TextPrimary", palette.TextPrimary);
        SetBrush(resources, "Brush.TextSecondary", palette.TextSecondary);
        SetBrush(resources, "Brush.ControlBackground", palette.ControlBackground);
        SetBrush(resources, "Brush.CardBackground", palette.CardBackground);
        SetBrush(resources, "Brush.SurfaceSubtle", palette.SurfaceSubtle);
        SetBrush(resources, "Brush.ScrollTrack", palette.ScrollTrack);
        SetBrush(resources, "Brush.ScrollThumb", palette.ScrollThumb);
        SetBrush(resources, "Brush.ScrollThumbHover", palette.ScrollThumbHover);
        SetBrush(resources, "Brush.ScrollThumbPressed", palette.ScrollThumbPressed);
    }

    private static void SetBrush(ResourceDictionary resources, string key, MediaColor color)
        => resources[key] = new SolidColorBrush(color);

    private readonly record struct ThemePalette(
        MediaColor WindowBackground,
        MediaColor PanelBackground,
        MediaColor PanelBorder,
        MediaColor Accent,
        MediaColor AccentMuted,
        MediaColor Warning,
        MediaColor TextPrimary,
        MediaColor TextSecondary,
        MediaColor ControlBackground,
        MediaColor CardBackground,
        MediaColor SurfaceSubtle,
        MediaColor ScrollTrack,
        MediaColor ScrollThumb,
        MediaColor ScrollThumbHover,
        MediaColor ScrollThumbPressed);

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
