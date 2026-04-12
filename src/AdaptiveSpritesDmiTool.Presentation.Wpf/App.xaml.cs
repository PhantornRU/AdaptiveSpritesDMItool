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
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

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
                WindowBackground: MediaColor.FromRgb(0xF4, 0xEF, 0xE7),
                PanelBackground: MediaColor.FromRgb(0xFF, 0xFD, 0xF9),
                PanelBorder: MediaColor.FromRgb(0xD9, 0xCF, 0xC0),
                Accent: MediaColor.FromRgb(0x1E, 0x5C, 0x54),
                AccentMuted: MediaColor.FromRgb(0xCD, 0xE0, 0xDB),
                Warning: MediaColor.FromRgb(0xC6, 0x6A, 0x2B),
                TextPrimary: MediaColor.FromRgb(0x1F, 0x1C, 0x18),
                TextSecondary: MediaColor.FromRgb(0x6A, 0x62, 0x58),
                ControlBackground: MediaColor.FromRgb(0xFD, 0xFD, 0xF8),
                CardBackground: MediaColors.White,
                SurfaceSubtle: MediaColor.FromRgb(0xFD, 0xF8, 0xEF)),
            WorkspaceThemeMode.Warm => new ThemePalette(
                WindowBackground: MediaColor.FromRgb(0x28, 0x22, 0x1E),
                PanelBackground: MediaColor.FromRgb(0x34, 0x2D, 0x28),
                PanelBorder: MediaColor.FromRgb(0x60, 0x51, 0x46),
                Accent: MediaColor.FromRgb(0xB1, 0x67, 0x3A),
                AccentMuted: MediaColor.FromRgb(0x63, 0x45, 0x34),
                Warning: MediaColor.FromRgb(0xD8, 0x9A, 0x4A),
                TextPrimary: MediaColor.FromRgb(0xF7, 0xED, 0xE2),
                TextSecondary: MediaColor.FromRgb(0xC9, 0xB7, 0xA7),
                ControlBackground: MediaColor.FromRgb(0x40, 0x36, 0x2F),
                CardBackground: MediaColor.FromRgb(0x3B, 0x31, 0x2B),
                SurfaceSubtle: MediaColor.FromRgb(0x30, 0x29, 0x24)),
            _ => new ThemePalette(
                WindowBackground: MediaColor.FromRgb(0x17, 0x1A, 0x1F),
                PanelBackground: MediaColor.FromRgb(0x1F, 0x25, 0x2D),
                PanelBorder: MediaColor.FromRgb(0x39, 0x42, 0x4D),
                Accent: MediaColor.FromRgb(0x2D, 0x8C, 0x7F),
                AccentMuted: MediaColor.FromRgb(0x2A, 0x4D, 0x49),
                Warning: MediaColor.FromRgb(0xD8, 0x8B, 0x3D),
                TextPrimary: MediaColor.FromRgb(0xF4, 0xF1, 0xEA),
                TextSecondary: MediaColor.FromRgb(0xB6, 0xC0, 0xCA),
                ControlBackground: MediaColor.FromRgb(0x25, 0x2C, 0x35),
                CardBackground: MediaColor.FromRgb(0x23, 0x2A, 0x33),
                SurfaceSubtle: MediaColor.FromRgb(0x20, 0x27, 0x30))
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
    }

    private static void SetBrush(ResourceDictionary resources, string key, MediaColor color)
    {
        if (resources[key] is SolidColorBrush brush)
        {
            brush.Color = color;
        }
        else
        {
            resources[key] = new SolidColorBrush(color);
        }
    }

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
        MediaColor SurfaceSubtle);

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
