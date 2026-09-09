using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public sealed record DocumentStateItemViewModel(
    string Name,
    SpriteDirectionDepth DirectionDepth,
    int FramesPerDirection,
    string AnimationSummary);

public sealed partial class DocumentWorkspaceViewModel : ShellSectionViewModel, IDisposable
{
    private readonly SpriteDocumentWorkflow? _workflow;
    private readonly IFileDialogService _fileDialogs;
    private readonly SpriteImageBitmapSourceFactory _bitmapFactory;
    private CancellationTokenSource? _operationCts;

    public DocumentWorkspaceViewModel(
        WorkspaceShellViewModel shell,
        SpriteDocumentWorkflow? workflow,
        IFileDialogService fileDialogs,
        SpriteImageBitmapSourceFactory bitmapFactory)
        : base(shell)
    {
        _workflow = workflow;
        _fileDialogs = fileDialogs;
        _bitmapFactory = bitmapFactory;
        States = [];
        PreviewDirections = [];
        ImportTargets = Enum.GetValues<SpriteDocumentImportTarget>();
        DirectionDepths = Enum.GetValues<SpriteDirectionDepth>();
    }

    public ObservableCollection<DocumentStateItemViewModel> States { get; }

    public ObservableCollection<SpriteDirection> PreviewDirections { get; }

    public IReadOnlyList<SpriteDocumentImportTarget> ImportTargets { get; }

    public IReadOnlyList<SpriteDirectionDepth> DirectionDepths { get; }

    public bool IsAvailable => _workflow is not null;

    public bool HasDocument => CurrentDocument is not null;

    public SpriteDocument? CurrentDocument => _workflow?.Session.CurrentDocument;

    public string DocumentTitle => CurrentDocument?.Name ?? App.Text("Text.Documents.NoDocument", "No document open");

    public string DocumentSummary => CurrentDocument is null
        ? App.Text("Text.Documents.EmptySummary", "Open a native DMI or import PNG graphics.")
        : $"{CurrentDocument.Resolution} | {CurrentDocument.States.Count} state(s) | {CurrentDocument.Sources.Count} source(s)";

    public string ProjectPathSummary => _workflow?.Session.ProjectPath
        ?? App.Text("Text.Documents.Unsaved", "Unsaved project");

    public bool IsDocumentDirty => _workflow?.Session.IsDirty == true;

    [ObservableProperty]
    private string documentName = "sprite-project";

    [ObservableProperty]
    private string stateName = "state";

    [ObservableProperty]
    private SpriteDocumentImportTarget selectedImportTarget = SpriteDocumentImportTarget.NewDocument;

    [ObservableProperty]
    private SpriteDirectionDepth selectedDirectionDepth = SpriteDirectionDepth.One;

    [ObservableProperty]
    private DocumentStateItemViewModel? selectedState;

    [ObservableProperty]
    private SpriteDirection selectedPreviewDirection = SpriteDirection.South;

    [ObservableProperty]
    private int selectedFrameIndex;

    [ObservableProperty]
    private BitmapSource? previewImage;

    [ObservableProperty]
    private string statusMessage = "Ready.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDocumentCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportPngCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportSpriteSheetCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDmiCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportPngSheetCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportPngSequenceCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshPreviewCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isBusy;

    private bool CanStartOperation() => IsAvailable && !IsBusy;

    private bool CanUseDocument() => CanStartOperation() && HasDocument;

    partial void OnSelectedStateChanged(DocumentStateItemViewModel? value)
    {
        PreviewDirections.Clear();
        if (value is null)
        {
            return;
        }

        foreach (var direction in value.DirectionDepth.GetDirections())
        {
            PreviewDirections.Add(direction);
        }

        SelectedPreviewDirection = PreviewDirections[0];
        SelectedFrameIndex = Math.Clamp(SelectedFrameIndex, 0, value.FramesPerDirection - 1);
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task OpenDocumentAsync()
    {
        var path = _fileDialogs.OpenSpriteDocumentFile(_workflow?.Session.ProjectPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await RunAsync(async cancellationToken =>
        {
            var result = path.EndsWith(".adaptive-dmi.json", StringComparison.OrdinalIgnoreCase)
                ? await LoadProjectResolvingSourcesAsync(path, cancellationToken)
                : await _workflow!.OpenNativeDmiAsync(path, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error.Message;
            }

            if (result.Value.Sources.Any(static source => source.Format == SpriteSourceFormat.Dmi))
            {
                await Shell.OpenNativeDmiInEditorAsync(result.Value.Sources[0].AbsolutePathFallback, cancellationToken);
            }

            RefreshDocument(result.Value);
            return _workflow!.Session.IsDirty
                ? PresentationText.Format(
                    "Text.Documents.OpenedDirtyFormat",
                    "Opened '{0}'. Save the project to persist resolved source fingerprints.",
                    result.Value.Name)
                : $"Opened '{result.Value.Name}'.";
        });
    }

    private async Task<Result<SpriteDocument>> LoadProjectResolvingSourcesAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        var relinkedSources = new Dictionary<Guid, string>();
        var acceptedChangedSources = new HashSet<Guid>();
        var handledIssues = new HashSet<Guid>();
        while (true)
        {
            var result = await _workflow!.LoadProjectAsync(
                new SpriteDocumentLoadRequest(
                    projectPath,
                    RelinkedSources: relinkedSources.Count == 0 ? null : relinkedSources,
                    AcceptedChangedSources: acceptedChangedSources.Count == 0 ? null : acceptedChangedSources),
                cancellationToken);
            if (result.IsSuccess ||
                !SpriteDocumentSourceErrors.TryGetIssue(result.Error, out var issue) ||
                issue is null)
            {
                return result;
            }

            if (!handledIssues.Add(issue.SourceId))
            {
                return Result.Failure<SpriteDocument>(Errors.Conflict(
                    $"Source resolution for '{issue.RelativePath}' did not produce a loadable project."));
            }

            if (issue.Kind == SpriteDocumentSourceIssueKind.Changed)
            {
                var choice = _fileDialogs.ResolveSpriteSourceChange(issue);
                if (choice == SpriteSourceChangeChoice.AcceptNewFingerprint)
                {
                    acceptedChangedSources.Add(issue.SourceId);
                    continue;
                }

                if (choice == SpriteSourceChangeChoice.Cancel)
                {
                    return Result.Failure<SpriteDocument>(Errors.Cancelled(
                        App.Text(
                            "Text.Documents.LoadCancelled",
                            "Sprite document loading was cancelled without changing the active document.")));
                }
            }

            var initialPath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(projectPath))!,
                issue.RelativePath));
            var replacementPath = _fileDialogs.RelinkSpriteSource(issue, initialPath);
            if (string.IsNullOrWhiteSpace(replacementPath))
            {
                return Result.Failure<SpriteDocument>(Errors.Cancelled(
                    App.Text(
                        "Text.Documents.LoadCancelled",
                        "Sprite document loading was cancelled without changing the active document.")));
            }

            relinkedSources[issue.SourceId] = replacementPath;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task ImportPngAsync()
    {
        var paths = _fileDialogs.OpenPngFiles(_workflow?.Session.ProjectPath);
        if (paths.Count == 0)
        {
            return;
        }

        await RunAsync(async cancellationToken =>
        {
            var kind = paths.Count == 1
                ? SpriteDocumentImportKind.SingleRaster
                : SpriteDocumentImportKind.RasterSequence;
            var request = new SpriteDocumentImportRequest(
                paths,
                ResolveDocumentName(paths[0]),
                kind,
                StateName: ResolveStateName(paths[0]),
                DirectionDepth: paths.Count == 1 ? SpriteDirectionDepth.One : SelectedDirectionDepth,
                DirectionOrder: paths.Count == 1 ? null : SelectedDirectionDepth.GetDirections(),
                ProjectPath: _workflow!.Session.ProjectPath);
            var result = await _workflow.ImportAsync(request, SelectedImportTarget, cancellationToken);
            return await ApplyImportOutcomeAsync(result, cancellationToken);
        });
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task ImportSpriteSheetAsync()
    {
        var paths = _fileDialogs.OpenPngFiles(_workflow?.Session.ProjectPath);
        if (paths.Count == 0)
        {
            return;
        }

        var path = paths[0];
        await RunAsync(async cancellationToken =>
        {
            var previewResult = await _workflow!.ReadRasterPreviewAsync(path, cancellationToken);
            if (previewResult.IsFailure)
            {
                return previewResult.Error.Message;
            }

            var recipe = _fileDialogs.ConfigureSpriteSheet(previewResult.Value, ResolveStateName(path));
            if (recipe is null)
            {
                return "Sprite-sheet import cancelled.";
            }

            var request = new SpriteDocumentImportRequest(
                [path],
                ResolveDocumentName(path),
                SpriteDocumentImportKind.SpriteSheet,
                StateName: ResolveStateName(path),
                SlicingRecipe: recipe,
                ProjectPath: _workflow.Session.ProjectPath);
            var result = await _workflow.ImportAsync(request, SelectedImportTarget, cancellationToken);
            return await ApplyImportOutcomeAsync(result, cancellationToken);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseDocument))]
    private async Task SaveProjectAsync()
    {
        var path = _fileDialogs.SaveSpriteDocumentProject(
            _workflow?.Session.ProjectPath,
            CurrentDocument?.Name);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await RunAsync(async cancellationToken =>
        {
            var result = await _workflow!.SaveProjectAsync(path, cancellationToken);
            if (result.IsSuccess)
            {
                NotifyDocumentProperties();
            }

            return result.IsSuccess ? $"Saved '{Path.GetFileName(path)}'." : result.Error.Message;
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseDocument))]
    private async Task ExportDmiAsync()
    {
        var path = _fileDialogs.SaveDmiDocument(_workflow?.Session.ProjectPath, CurrentDocument?.Name);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await ExportAsync(path, SpriteDocumentExportFormat.Dmi);
    }

    [RelayCommand(CanExecute = nameof(CanUseDocument))]
    private async Task ExportPngSheetAsync() => await ExportPngAsync(SpriteDocumentExportFormat.PngSheet);

    [RelayCommand(CanExecute = nameof(CanUseDocument))]
    private async Task ExportPngSequenceAsync() => await ExportPngAsync(SpriteDocumentExportFormat.PngSequence);

    [RelayCommand(CanExecute = nameof(CanUseDocument))]
    private async Task RefreshPreviewAsync()
    {
        if (SelectedState is null)
        {
            StatusMessage = "Select a state first.";
            return;
        }

        await RunAsync(async cancellationToken =>
        {
            var state = CurrentDocument!.States.First(candidate => candidate.Name == SelectedState.Name);
            var direction = state.DirectionDepth.GetDirections().Contains(SelectedPreviewDirection)
                ? SelectedPreviewDirection
                : SpriteDirection.South;
            var frameIndex = Math.Clamp(SelectedFrameIndex, 0, state.FramesPerDirection - 1);
            var result = await _workflow!.ReadFrameAsync(state.Name, direction, frameIndex, cancellationToken);
            if (result.IsSuccess)
            {
                PreviewImage = _bitmapFactory.Create(result.Value);
                SelectedPreviewDirection = direction;
                SelectedFrameIndex = frameIndex;
            }

            return result.IsSuccess
                ? $"Preview: {state.Name}/{direction}/{frameIndex}."
                : result.Error.Message;
        });
    }

    [RelayCommand(CanExecute = nameof(IsBusy))]
    private void Cancel() => _operationCts?.Cancel();

    private async Task ExportPngAsync(SpriteDocumentExportFormat format)
    {
        var parent = _fileDialogs.SelectDirectory("Select parent directory for managed PNG export", _workflow?.Session.ProjectPath);
        if (string.IsNullOrWhiteSpace(parent))
        {
            return;
        }

        var suffix = format == SpriteDocumentExportFormat.PngSheet ? "png-sheet" : "png-sequence";
        var outputPath = Path.Combine(parent, $"{SanitizeName(CurrentDocument!.Name)}-{suffix}");
        await ExportAsync(outputPath, format);
    }

    private async Task ExportAsync(string path, SpriteDocumentExportFormat format)
    {
        await RunAsync(async cancellationToken =>
        {
            var result = await _workflow!.ExportAsync(
                path,
                format,
                OverwritePolicy.OverwriteExisting,
                cancellationToken);
            return result.IsSuccess
                ? $"Exported to '{result.Value.OutputPath}'."
                : result.Error.Message;
        });
    }

    private async Task<string> ApplyImportOutcomeAsync(
        AdaptiveSpritesDmiTool.Application.Common.Result<SpriteDocumentImportOutcome> result,
        CancellationToken cancellationToken)
    {
        if (result.IsFailure)
        {
            return result.Error.Message;
        }

        if (result.Value.AuxiliaryDocument is { } auxiliary)
        {
            var state = auxiliary.States[0];
            var frameIndex = Math.Clamp(SelectedFrameIndex, 0, state.FramesPerDirection - 1);
            var addResult = await Shell.AddAuxiliaryDocumentFrameAsync(
                auxiliary,
                state.Name,
                state.DirectionDepth.GetDirections()[0],
                frameIndex,
                cancellationToken);
            return addResult.IsSuccess
                ? $"Added '{state.Name}' frame {frameIndex} as an auxiliary layer."
                : addResult.Error.Message;
        }

        if (result.Value.ActiveDocument is { } active)
        {
            RefreshDocument(active);
            return $"Imported '{active.Name}'.";
        }

        return "Import completed.";
    }

    private async Task RunAsync(Func<CancellationToken, Task<string>> operation)
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _operationCts = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            StatusMessage = await operation(_operationCts.Token);
            Shell.StatusMessage = StatusMessage;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Unexpected error: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshDocument(SpriteDocument document)
    {
        States.Clear();
        foreach (var state in document.States)
        {
            States.Add(new DocumentStateItemViewModel(
                state.Name,
                state.DirectionDepth,
                state.FramesPerDirection,
                $"delay {state.Animation.Delays.Count}, loop {state.Animation.Loop}, rewind {state.Animation.Rewind}, movement {state.Animation.Movement}"));
        }

        DocumentName = document.Name;
        SelectedState = States.FirstOrDefault();
        SelectedPreviewDirection = document.States[0].DirectionDepth.GetDirections()[0];
        SelectedFrameIndex = 0;
        PreviewImage = null;
        NotifyDocumentProperties();
    }

    internal void RestoreDocument(SpriteDocument document) => RefreshDocument(document);

    internal void ResetDocument()
    {
        States.Clear();
        PreviewDirections.Clear();
        SelectedState = null;
        PreviewImage = null;
        StatusMessage = "Ready.";
        NotifyDocumentProperties();
    }

    private void NotifyDocumentProperties()
    {
        OnPropertyChanged(nameof(CurrentDocument));
        OnPropertyChanged(nameof(HasDocument));
        OnPropertyChanged(nameof(DocumentTitle));
        OnPropertyChanged(nameof(DocumentSummary));
        OnPropertyChanged(nameof(ProjectPathSummary));
        OnPropertyChanged(nameof(IsDocumentDirty));
        SaveProjectCommand.NotifyCanExecuteChanged();
        ExportDmiCommand.NotifyCanExecuteChanged();
        ExportPngSheetCommand.NotifyCanExecuteChanged();
        ExportPngSequenceCommand.NotifyCanExecuteChanged();
        RefreshPreviewCommand.NotifyCanExecuteChanged();
    }

    private string ResolveDocumentName(string sourcePath) =>
        string.IsNullOrWhiteSpace(DocumentName)
            ? Path.GetFileNameWithoutExtension(sourcePath)
            : DocumentName.Trim();

    private string ResolveStateName(string sourcePath) =>
        string.IsNullOrWhiteSpace(StateName)
            ? Path.GetFileNameWithoutExtension(sourcePath)
            : StateName.Trim();

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
    }

    public void Dispose()
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
