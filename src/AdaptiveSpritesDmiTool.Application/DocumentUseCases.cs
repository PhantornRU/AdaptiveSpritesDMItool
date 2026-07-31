using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;

namespace AdaptiveSpritesDmiTool.Application;

public sealed class SpriteDocumentSession
{
    public SpriteDocument? CurrentDocument { get; private set; }

    public string? ProjectPath { get; private set; }

    public bool IsDirty { get; private set; }

    public Result<SpriteDocument> Open(SpriteDocument document, string? projectPath, bool isDirty)
    {
        ArgumentNullException.ThrowIfNull(document);
        CurrentDocument = document;
        ProjectPath = string.IsNullOrWhiteSpace(projectPath) ? null : Path.GetFullPath(projectPath);
        IsDirty = isDirty;
        return Result.Success(document);
    }

    public Result MarkSaved(string projectPath)
    {
        if (CurrentDocument is null)
        {
            return Result.Failure(Errors.Conflict("No sprite document is open."));
        }

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return Result.Failure(Errors.Validation("Sprite document project path is required."));
        }

        ProjectPath = Path.GetFullPath(projectPath);
        IsDirty = false;
        return Result.Success();
    }

    public void MarkDirty()
    {
        if (CurrentDocument is not null)
        {
            IsDirty = true;
        }
    }

    public void Clear()
    {
        CurrentDocument = null;
        ProjectPath = null;
        IsDirty = false;
    }
}

public sealed class ProbeAssetUseCase(IAssetProbeService probeService)
{
    public Task<Result<AssetProbe>> ExecuteAsync(
        string path,
        CancellationToken cancellationToken) =>
        probeService.ProbeAsync(path, AssetImportLimits.Default, cancellationToken);
}

public sealed class ImportSpriteDocumentUseCase(
    ISpriteDocumentImporter importer,
    SpriteDocumentSession session)
{
    public async Task<Result<SpriteDocument>> ExecuteAsync(
        SpriteDocumentImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await importer.ImportAsync(request, cancellationToken).ConfigureAwait(false);
        return result.IsFailure
            ? result
            : session.Open(result.Value, request.ProjectPath, isDirty: true);
    }
}

public sealed class LoadSpriteDocumentUseCase(
    ISpriteDocumentRepository repository,
    SpriteDocumentSession session)
{
    public async Task<Result<SpriteDocument>> ExecuteAsync(
        SpriteDocumentLoadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await repository.LoadAsync(request, cancellationToken).ConfigureAwait(false);
        return result.IsFailure
            ? result
            : session.Open(result.Value, request.ProjectPath, SpriteDocumentLoadRequestPolicy.HasResolvedSources(request));
    }
}

public sealed class SaveSpriteDocumentUseCase(
    ISpriteDocumentRepository repository,
    SpriteDocumentSession session)
{
    public async Task<Result> ExecuteAsync(string projectPath, CancellationToken cancellationToken)
    {
        if (session.CurrentDocument is null)
        {
            return Result.Failure(Errors.Conflict("No sprite document is open."));
        }

        var saveResult = await repository
            .SaveAsync(projectPath, session.CurrentDocument, cancellationToken)
            .ConfigureAwait(false);
        return saveResult.IsFailure ? saveResult : session.MarkSaved(projectPath);
    }
}

public sealed class ReadSpriteDocumentFrameUseCase(
    ISpriteFrameSource frameSource,
    SpriteDocumentSession session)
{
    public Task<Result<SpriteImage>> ExecuteAsync(
        string stateName,
        Domain.Configurations.SpriteDirection direction,
        int frameIndex,
        CancellationToken cancellationToken)
    {
        if (session.CurrentDocument is null)
        {
            return Task.FromResult(Result.Failure<SpriteImage>(Errors.Conflict("No sprite document is open.")));
        }

        return frameSource.ReadAsync(
            new SpriteFrameReadRequest(session.CurrentDocument, stateName, direction, frameIndex),
            cancellationToken);
    }
}

public sealed class ExportSpriteDocumentUseCase(
    ISpriteDocumentExporter exporter,
    SpriteDocumentSession session)
{
    public Task<Result<SpriteDocumentExportResult>> ExecuteAsync(
        string outputPath,
        SpriteDocumentExportFormat format,
        OverwritePolicy overwritePolicy,
        CancellationToken cancellationToken)
    {
        if (session.CurrentDocument is null)
        {
            return Task.FromResult(Result.Failure<SpriteDocumentExportResult>(Errors.Conflict("No sprite document is open.")));
        }

        return exporter.ExportAsync(
            new SpriteDocumentExportRequest(session.CurrentDocument, outputPath, format, overwritePolicy),
            cancellationToken);
    }
}

public enum SpriteDocumentImportTarget
{
    NewDocument = 0,
    AddStates = 1,
    AuxiliaryLayer = 2
}

public sealed record SpriteDocumentImportOutcome(
    SpriteDocument? ActiveDocument,
    SpriteDocument? AuxiliaryDocument);

public sealed class SpriteDocumentWorkflow(
    IAssetProbeService probeService,
    ISpriteDocumentImporter importer,
    ISpriteDocumentRepository repository,
    ISpriteFrameSource frameSource,
    ISpriteDocumentExporter exporter,
    SpriteDocumentSession session)
{
    public SpriteDocumentSession Session => session;

    public Task<Result<AssetProbe>> ProbeAsync(string path, CancellationToken cancellationToken) =>
        probeService.ProbeAsync(path, AssetImportLimits.Default, cancellationToken);

    public async Task<Result<SpriteDocumentImportOutcome>> ImportAsync(
        SpriteDocumentImportRequest request,
        SpriteDocumentImportTarget target,
        CancellationToken cancellationToken)
    {
        var importResult = await importer.ImportAsync(request, cancellationToken).ConfigureAwait(false);
        if (importResult.IsFailure)
        {
            return Result.Failure<SpriteDocumentImportOutcome>(importResult.Error);
        }

        if (target == SpriteDocumentImportTarget.AuxiliaryLayer)
        {
            return Result.Success(new SpriteDocumentImportOutcome(session.CurrentDocument, importResult.Value));
        }

        var document = importResult.Value;
        if (target == SpriteDocumentImportTarget.AddStates)
        {
            if (session.CurrentDocument is null)
            {
                return Result.Failure<SpriteDocumentImportOutcome>(Errors.Conflict(
                    "Open or create a sprite document before adding states."));
            }

            var mergeResult = MergeDocuments(session.CurrentDocument, document);
            if (mergeResult.IsFailure)
            {
                return Result.Failure<SpriteDocumentImportOutcome>(mergeResult.Error);
            }

            document = mergeResult.Value;
        }

        session.Open(document, request.ProjectPath, isDirty: true);
        return Result.Success(new SpriteDocumentImportOutcome(document, null));
    }

    public async Task<Result<SpriteDocument>> OpenNativeDmiAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var probeResult = await ProbeAsync(path, cancellationToken).ConfigureAwait(false);
        if (probeResult.IsFailure)
        {
            return Result.Failure<SpriteDocument>(probeResult.Error);
        }

        if (probeResult.Value.DetectedFormat != SpriteSourceFormat.Dmi)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(
                "The selected file is an ordinary PNG. Use Import graphics instead of Open DMI."));
        }

        var request = new SpriteDocumentImportRequest(
            [path],
            Path.GetFileNameWithoutExtension(path),
            SpriteDocumentImportKind.NativeDmi);
        var importResult = await importer.ImportAsync(request, cancellationToken).ConfigureAwait(false);
        if (importResult.IsSuccess)
        {
            session.Open(importResult.Value, projectPath: null, isDirty: false);
        }

        return importResult;
    }

    public async Task<Result<SpriteDocument>> LoadProjectAsync(
        SpriteDocumentLoadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await repository.LoadAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            session.Open(result.Value, request.ProjectPath, SpriteDocumentLoadRequestPolicy.HasResolvedSources(request));
        }

        return result;
    }

    public async Task<Result> SaveProjectAsync(string path, CancellationToken cancellationToken)
    {
        if (session.CurrentDocument is null)
        {
            return Result.Failure(Errors.Conflict("No sprite document is open."));
        }

        var result = await repository.SaveAsync(path, session.CurrentDocument, cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? result : session.MarkSaved(path);
    }

    public Task<Result<SpriteImage>> ReadFrameAsync(
        string stateName,
        SpriteDirection direction,
        int frameIndex,
        CancellationToken cancellationToken) =>
        session.CurrentDocument is null
            ? Task.FromResult(Result.Failure<SpriteImage>(Errors.Conflict("No sprite document is open.")))
            : frameSource.ReadAsync(
                new SpriteFrameReadRequest(session.CurrentDocument, stateName, direction, frameIndex),
                cancellationToken);

    public async Task<Result<SpriteImage>> ReadRasterPreviewAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var probeResult = await ProbeAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        if (probeResult.IsFailure)
        {
            return Result.Failure<SpriteImage>(probeResult.Error);
        }

        if (probeResult.Value.DetectedFormat != SpriteSourceFormat.Png)
        {
            return Result.Failure<SpriteImage>(Errors.Validation("Sprite-sheet preview requires an ordinary PNG source."));
        }

        var previewDocumentResult = await importer.ImportAsync(
            new SpriteDocumentImportRequest(
                [sourcePath],
                Path.GetFileNameWithoutExtension(sourcePath),
                SpriteDocumentImportKind.SingleRaster),
            cancellationToken).ConfigureAwait(false);
        if (previewDocumentResult.IsFailure)
        {
            return Result.Failure<SpriteImage>(previewDocumentResult.Error);
        }

        var state = previewDocumentResult.Value.States[0];
        return await frameSource.ReadAsync(
            new SpriteFrameReadRequest(previewDocumentResult.Value, state.Name, SpriteDirection.South, 0),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<SpriteDocumentExportResult>> ExportAsync(
        string path,
        SpriteDocumentExportFormat format,
        OverwritePolicy overwritePolicy,
        CancellationToken cancellationToken) =>
        session.CurrentDocument is null
            ? Task.FromResult(Result.Failure<SpriteDocumentExportResult>(Errors.Conflict("No sprite document is open.")))
            : exporter.ExportAsync(
                new SpriteDocumentExportRequest(session.CurrentDocument, path, format, overwritePolicy),
                cancellationToken);

    private static Result<SpriteDocument> MergeDocuments(SpriteDocument active, SpriteDocument imported)
    {
        if (active.Resolution != imported.Resolution)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(
                $"Imported document resolution {imported.Resolution} does not match active {active.Resolution}."));
        }

        var duplicateState = imported.States.FirstOrDefault(state =>
            active.States.Any(existing => string.Equals(existing.Name, state.Name, StringComparison.Ordinal)));
        if (duplicateState is not null)
        {
            return Result.Failure<SpriteDocument>(Errors.Conflict(
                $"State '{duplicateState.Name}' already exists in the active document."));
        }

        if (active.Sources.Select(static source => source.Id)
            .Intersect(imported.Sources.Select(static source => source.Id))
            .Any())
        {
            return Result.Failure<SpriteDocument>(Errors.Conflict("Imported document source identifiers conflict with the active document."));
        }

        try
        {
            return Result.Success(new SpriteDocument(
                active.Id,
                active.Name,
                active.Resolution,
                active.Sources.Concat(imported.Sources).ToArray(),
                active.States.Concat(imported.States).ToArray(),
                active.SlicingRecipe));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(exception.Message));
        }
    }
}

internal static class SpriteDocumentLoadRequestPolicy
{
    public static bool HasResolvedSources(SpriteDocumentLoadRequest request) =>
        request.SourceChangeResolution == SourceChangeResolution.AcceptNewFingerprint ||
        request.RelinkedSources is { Count: > 0 } ||
        request.AcceptedChangedSources is { Count: > 0 };
}
