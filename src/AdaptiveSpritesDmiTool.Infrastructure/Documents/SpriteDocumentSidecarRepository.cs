using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AdaptiveSpritesDmiTool.Infrastructure.Documents;

public sealed class SpriteDocumentSidecarRepository(IAssetProbeService probeService) : ISpriteDocumentRepository
{
    internal const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
        Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) }
    };

    public async Task<Result<SpriteDocument>> LoadAsync(
        SpriteDocumentLoadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ProjectPath))
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("Sprite document project path is required."));
        }

        var projectPath = Path.GetFullPath(request.ProjectPath);
        if (!File.Exists(projectPath))
        {
            return Result.Failure<SpriteDocument>(Errors.NotFound($"Sprite document project '{projectPath}' was not found."));
        }

        try
        {
            var json = await File.ReadAllTextAsync(projectPath, cancellationToken).ConfigureAwait(false);
            var dto = DeserializeProject(json);
            if (dto.Version != CurrentSchemaVersion)
            {
                return Result.Failure<SpriteDocument>(Errors.Validation($"Unsupported sprite document project version '{dto.Version}'."));
            }

            if (dto.Sources is null || dto.Sources.Count == 0)
            {
                return Result.Failure<SpriteDocument>(Errors.Validation("Sprite document project does not contain sources."));
            }

            var limits = request.Limits ?? AssetImportLimits.Default;
            var shapeError = ValidateProjectShape(dto, limits);
            if (shapeError is not null)
            {
                return Result.Failure<SpriteDocument>(Errors.Validation(shapeError));
            }

            var projectDirectory = Path.GetDirectoryName(projectPath)!;
            var sources = new List<SpriteSourceReference>(dto.Sources.Count);
            long decodedSourcePixels = 0;
            foreach (var sourceDto in dto.Sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePathResult = ResolveSourcePath(projectDirectory, sourceDto, request.RelinkedSources);
                if (sourcePathResult.IsFailure)
                {
                    return Result.Failure<SpriteDocument>(sourcePathResult.Error);
                }

                var probeResult = await probeService
                    .ProbeAsync(sourcePathResult.Value, limits, cancellationToken)
                    .ConfigureAwait(false);
                if (probeResult.IsFailure)
                {
                    return Result.Failure<SpriteDocument>(probeResult.Error);
                }

                var probe = probeResult.Value;
                decodedSourcePixels = checked(
                    decodedSourcePixels + checked((long)probe.Width * probe.Height * probe.EncodedFrameCount));
                if (decodedSourcePixels > limits.MaximumDecodedPixels)
                {
                    return Result.Failure<SpriteDocument>(Errors.Validation(
                        $"Sprite document sources exceed the {limits.MaximumDecodedPixels} decoded-pixel limit."));
                }

                var fingerprintMatches = sourceDto.Format == probe.DetectedFormat &&
                    sourceDto.Width == probe.Width &&
                    sourceDto.Height == probe.Height &&
                    sourceDto.EncodedLength == probe.EncodedLength &&
                    string.Equals(sourceDto.Sha256, probe.Sha256, StringComparison.OrdinalIgnoreCase);
                if (!fingerprintMatches && request.SourceChangeResolution == SourceChangeResolution.Reject)
                {
                    return Result.Failure<SpriteDocument>(Errors.Conflict(
                        $"Source '{sourceDto.RelativePath}' changed after the project was saved. Relink it, accept the new fingerprint, or cancel."));
                }

                sources.Add(new SpriteSourceReference(
                    sourceDto.Id,
                    Path.GetRelativePath(projectDirectory, probe.SourcePath),
                    probe.SourcePath,
                    probe.DetectedFormat,
                    probe.Width,
                    probe.Height,
                    probe.EncodedLength,
                    probe.Sha256));
            }

            var document = ToDomain(dto, sources);
            return Result.Success(document);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpriteDocument>(Errors.Cancelled("Sprite document loading was cancelled."));
        }
        catch (JsonException exception)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation($"Sprite document project JSON is invalid: {exception.Message}"));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation($"Sprite document project is invalid: {exception.Message}"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<SpriteDocument>(Errors.Unexpected($"Failed to load sprite document project: {exception.Message}"));
        }
    }

    public async Task<Result> SaveAsync(
        string projectPath,
        SpriteDocument document,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return Result.Failure(Errors.Validation("Sprite document project path is required."));
        }

        ArgumentNullException.ThrowIfNull(document);
        var normalizedPath = Path.GetFullPath(projectPath);
        var directory = Path.GetDirectoryName(normalizedPath)!;
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(normalizedPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(directory);
            var normalizedDocument = NormalizeSourcePaths(document, directory);
            var dto = FromDomain(normalizedDocument);
            var json = JsonConvert.SerializeObject(dto, SerializerSettings) + Environment.NewLine;
            await File.WriteAllTextAsync(temporaryPath, json, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken)
                .ConfigureAwait(false);

            var persistedJson = await File.ReadAllTextAsync(temporaryPath, cancellationToken).ConfigureAwait(false);
            var persisted = DeserializeProject(persistedJson);
            var roundTrip = ToDomain(persisted, normalizedDocument.Sources);
            if (roundTrip.Id != normalizedDocument.Id ||
                roundTrip.States.Count != normalizedDocument.States.Count ||
                roundTrip.Sources.Count != normalizedDocument.Sources.Count ||
                !roundTrip.Sources.Select(static source => source.Sha256)
                    .SequenceEqual(normalizedDocument.Sources.Select(static source => source.Sha256), StringComparer.Ordinal))
            {
                return Result.Failure(Errors.Unexpected("Sprite document project verification failed before commit."));
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(normalizedPath))
            {
                File.Replace(temporaryPath, normalizedPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, normalizedPath);
            }

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(Errors.Cancelled("Sprite document saving was cancelled."));
        }
        catch (JsonException exception)
        {
            return Result.Failure(Errors.Validation($"Sprite document project serialization failed: {exception.Message}"));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return Result.Failure(Errors.Validation($"Sprite document project is invalid: {exception.Message}"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure(Errors.Unexpected($"Failed to save sprite document project: {exception.Message}"));
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static SpriteDocumentProjectDto DeserializeProject(string json) =>
        JsonConvert.DeserializeObject<SpriteDocumentProjectDto>(json, SerializerSettings)
        ?? throw new JsonSerializationException("Sprite document project JSON is empty.");

    private static string? ValidateProjectShape(
        SpriteDocumentProjectDto dto,
        AssetImportLimits limits)
    {
        if (dto.CanvasWidth <= 0 || dto.CanvasHeight <= 0 ||
            dto.CanvasWidth > limits.MaximumDimension || dto.CanvasHeight > limits.MaximumDimension)
        {
            return $"Sprite document canvas dimensions must be between 1 and {limits.MaximumDimension} pixels.";
        }

        if (dto.Sources!.Count > limits.MaximumFramesOrCells)
        {
            return $"Sprite document exceeds the {limits.MaximumFramesOrCells} source limit.";
        }

        if (dto.States is null || dto.States.Count == 0)
        {
            return "Sprite document project must contain at least one state.";
        }

        if (dto.States.Count > limits.MaximumStates)
        {
            return $"Sprite document exceeds the {limits.MaximumStates} state limit.";
        }

        try
        {
            var frameCount = 0;
            foreach (var state in dto.States)
            {
                if (state.Frames is null)
                {
                    return $"State '{state.Name}' does not contain frame references.";
                }

                frameCount = checked(frameCount + state.Frames.Count);
                if (frameCount > limits.MaximumFramesOrCells)
                {
                    return $"Sprite document exceeds the {limits.MaximumFramesOrCells} frame limit.";
                }
            }
        }
        catch (OverflowException)
        {
            return "Sprite document frame count overflowed the supported limit.";
        }

        return null;
    }

    private static Result<string> ResolveSourcePath(
        string projectDirectory,
        SpriteSourceDto source,
        IReadOnlyDictionary<Guid, string>? relinkedSources)
    {
        if (relinkedSources?.TryGetValue(source.Id, out var relinkedPath) == true && !string.IsNullOrWhiteSpace(relinkedPath))
        {
            return Result.Success(Path.GetFullPath(relinkedPath));
        }

        if (!string.IsNullOrWhiteSpace(source.RelativePath))
        {
            var relativeCandidate = Path.GetFullPath(Path.Combine(projectDirectory, source.RelativePath));
            if (File.Exists(relativeCandidate))
            {
                return Result.Success(relativeCandidate);
            }
        }

        if (!string.IsNullOrWhiteSpace(source.AbsolutePathFallback))
        {
            var fallback = Path.GetFullPath(source.AbsolutePathFallback);
            if (File.Exists(fallback))
            {
                return Result.Success(fallback);
            }
        }

        return Result.Failure<string>(Errors.NotFound(
            $"Source '{source.RelativePath}' is missing. Relink the source or cancel loading."));
    }

    private static SpriteDocument NormalizeSourcePaths(SpriteDocument document, string projectDirectory)
    {
        var sources = document.Sources
            .Select(source =>
            {
                var absolutePath = Path.GetFullPath(source.AbsolutePathFallback);
                var preservedRelativeCandidate = Path.GetFullPath(Path.Combine(projectDirectory, source.RelativePath));
                var relativePath = File.Exists(preservedRelativeCandidate)
                    ? source.RelativePath
                    : Path.GetRelativePath(projectDirectory, absolutePath);
                return new SpriteSourceReference(
                    source.Id,
                    relativePath,
                    absolutePath,
                    source.Format,
                    source.Width,
                    source.Height,
                    source.EncodedLength,
                    source.Sha256);
            })
            .ToArray();

        return new SpriteDocument(
            document.Id,
            document.Name,
            document.Resolution,
            sources,
            document.States,
            document.SlicingRecipe);
    }

    private static SpriteDocumentProjectDto FromDomain(SpriteDocument document) =>
        new(
            CurrentSchemaVersion,
            document.Id,
            document.Name,
            document.Resolution.Width,
            document.Resolution.Height,
            document.Sources.Select(static source => new SpriteSourceDto(
                source.Id,
                source.RelativePath,
                source.AbsolutePathFallback,
                source.Format,
                source.Width,
                source.Height,
                source.EncodedLength,
                source.Sha256)).ToArray(),
            document.States.Select(static state => new SpriteStateDto(
                state.Name,
                state.DirectionDepth,
                state.FramesPerDirection,
                new SpriteAnimationDto(
                    state.Animation.Delays,
                    state.Animation.Loop,
                    state.Animation.Rewind,
                    state.Animation.Movement,
                    state.Animation.Hotspots),
                state.Frames.Select(static frame => new SpriteFrameDto(
                    frame.Direction,
                    frame.FrameIndex,
                    frame.Source.SourceId,
                    frame.Source.SourceFrameIndex,
                    frame.Source.SourceRectangle,
                    frame.Source.Transform,
                    frame.Source.SourceStateName,
                    frame.Source.SourceDirection)).ToArray())).ToArray(),
            document.SlicingRecipe);

    private static SpriteDocument ToDomain(
        SpriteDocumentProjectDto dto,
        IReadOnlyList<SpriteSourceReference> resolvedSources)
    {
        if (dto.States is null || dto.States.Count == 0)
        {
            throw new ArgumentException("Sprite document project must contain at least one state.", nameof(dto));
        }

        var states = dto.States.Select(state =>
        {
            if (state.Animation is null || state.Frames is null)
            {
                throw new ArgumentException($"State '{state.Name}' is incomplete.", nameof(dto));
            }

            var animation = new SpriteAnimationMetadata(
                state.Animation.Delays,
                state.Animation.Loop,
                state.Animation.Rewind,
                state.Animation.Movement,
                state.Animation.Hotspots);
            var frames = state.Frames.Select(static frame => new SpriteDocumentFrame(
                frame.Direction,
                frame.FrameIndex,
                new SpriteFrameReference(
                    frame.SourceId,
                    frame.SourceRectangle,
                    frame.SourceFrameIndex,
                    frame.Transform,
                    frame.SourceStateName,
                    frame.SourceDirection))).ToArray();
            return new SpriteDocumentState(
                state.Name,
                state.DirectionDepth,
                state.FramesPerDirection,
                animation,
                frames);
        }).ToArray();

        return new SpriteDocument(
            dto.Id,
            dto.Name,
            new SpriteResolution(dto.CanvasWidth, dto.CanvasHeight),
            resolvedSources,
            states,
            dto.SlicingRecipe);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record SpriteDocumentProjectDto(
        int Version,
        Guid Id,
        string Name,
        int CanvasWidth,
        int CanvasHeight,
        IReadOnlyList<SpriteSourceDto>? Sources,
        IReadOnlyList<SpriteStateDto>? States,
        SpriteSheetSlicingRecipe? SlicingRecipe);

    private sealed record SpriteSourceDto(
        Guid Id,
        string RelativePath,
        string AbsolutePathFallback,
        SpriteSourceFormat Format,
        int Width,
        int Height,
        long EncodedLength,
        string Sha256);

    private sealed record SpriteStateDto(
        string Name,
        SpriteDirectionDepth DirectionDepth,
        int FramesPerDirection,
        SpriteAnimationDto? Animation,
        IReadOnlyList<SpriteFrameDto>? Frames);

    private sealed record SpriteAnimationDto(
        IReadOnlyList<double>? Delays,
        int Loop,
        bool Rewind,
        bool Movement,
        IReadOnlyList<SpriteHotspot>? Hotspots);

    private sealed record SpriteFrameDto(
        SpriteDirection Direction,
        int FrameIndex,
        Guid SourceId,
        int SourceFrameIndex,
        SpriteSourceRectangle SourceRectangle,
        SpriteFrameTransform Transform,
        string? SourceStateName,
        SpriteDirection? SourceDirection);
}
