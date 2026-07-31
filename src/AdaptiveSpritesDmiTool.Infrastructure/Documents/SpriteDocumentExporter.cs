using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using DMISharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Png.Chunks;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Infrastructure.Documents;

public sealed class SpriteDocumentExporter(
    ISpriteFrameSource frameSource,
    IAssetProbeService probeService,
    ISpriteDocumentRepository documentRepository) : ISpriteDocumentExporter
{
    private const int ManagedExportManifestVersion = 1;
    private const string ManagedExportOwner = "AdaptiveSpritesDMItool";
    private const string ManagedExportManifestFileName = ".adaptive-dmi-export.json";
    private static readonly JsonSerializerSettings ManifestSerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.Indented,
        Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) }
    };

    public async Task<Result<SpriteDocumentExportResult>> ExportAsync(
        SpriteDocumentExportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            return Result.Failure<SpriteDocumentExportResult>(Errors.Validation("Document export path is required."));
        }

        try
        {
            return request.Format switch
            {
                SpriteDocumentExportFormat.Dmi => await ExportDmiAsync(request, cancellationToken).ConfigureAwait(false),
                SpriteDocumentExportFormat.PngSheet => await ExportPngAsync(request, sheet: true, cancellationToken).ConfigureAwait(false),
                SpriteDocumentExportFormat.PngSequence => await ExportPngAsync(request, sheet: false, cancellationToken).ConfigureAwait(false),
                _ => Result.Failure<SpriteDocumentExportResult>(Errors.Validation("Unsupported document export format."))
            };
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpriteDocumentExportResult>(Errors.Cancelled("Sprite document export was cancelled."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException or InvalidDataException)
        {
            return Result.Failure<SpriteDocumentExportResult>(Errors.Validation($"Sprite document export is invalid: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<SpriteDocumentExportResult>(Errors.Unexpected($"Failed to export sprite document: {exception.Message}"));
        }
    }

    private async Task<Result<SpriteDocumentExportResult>> ExportDmiAsync(
        SpriteDocumentExportRequest request,
        CancellationToken cancellationToken)
    {
        var destinationPath = Path.GetFullPath(request.OutputPath);
        var overwriteResult = CheckFileOverwrite(destinationPath, request.OverwritePolicy);
        if (overwriteResult.IsFailure)
        {
            return Result.Failure<SpriteDocumentExportResult>(overwriteResult.Error);
        }

        if (!overwriteResult.Value)
        {
            return Result.Success(new SpriteDocumentExportResult(destinationPath, [], null));
        }

        var directory = Path.GetDirectoryName(destinationPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileNameWithoutExtension(destinationPath)}.{Guid.NewGuid():N}.tmp.dmi");

        try
        {
            DmiArtifactFingerprint expected;
            using (var dmiFile = new DMIFile(request.Document.Resolution.Width, request.Document.Resolution.Height))
            {
                foreach (var documentState in request.Document.States)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var dmiState = new DMIState(
                        documentState.Name,
                        ToDmiDepth(documentState.DirectionDepth),
                        documentState.FramesPerDirection,
                        request.Document.Resolution.Width,
                        request.Document.Resolution.Height);
                    try
                    {
                        foreach (var documentFrame in documentState.Frames)
                        {
                            var frameResult = await frameSource.ReadAsync(
                                new SpriteFrameReadRequest(
                                    request.Document,
                                    documentState.Name,
                                    documentFrame.Direction,
                                    documentFrame.FrameIndex),
                                cancellationToken).ConfigureAwait(false);
                            if (frameResult.IsFailure)
                            {
                                dmiState.Dispose();
                                return Result.Failure<SpriteDocumentExportResult>(frameResult.Error);
                            }

                            var image = Image.LoadPixelData<Rgba32>(
                                frameResult.Value.RgbaBytes,
                                frameResult.Value.Width,
                                frameResult.Value.Height);
                            dmiState.SetFrame(
                                image,
                                DmiSharpConversions.ToDmiDirection(documentFrame.Direction),
                                documentFrame.FrameIndex);
                        }

                        ApplyAnimationMetadata(dmiState, documentState.Animation, documentState.FramesPerDirection);
                        dmiFile.AddState(dmiState);
                    }
                    catch
                    {
                        dmiState.Dispose();
                        throw;
                    }
                }

                expected = DmiArtifactFingerprintFactory.Create(dmiFile, cancellationToken);
                dmiFile.Save(temporaryPath);
                if (dmiFile.States.Any(static state => state.Data.Hotspots is { Count: > 0 }))
                {
                    await RewriteDmiMetadataAsync(temporaryPath, dmiFile, cancellationToken).ConfigureAwait(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var validation = new DmiArtifactValidator().Validate(temporaryPath, expected, cancellationToken);
            if (validation.IsFailure)
            {
                return Result.Failure<SpriteDocumentExportResult>(validation.Error);
            }

            cancellationToken.ThrowIfCancellationRequested();
            new DmiAtomicCommitter().Commit(temporaryPath, destinationPath);
            return Result.Success(new SpriteDocumentExportResult(destinationPath, [destinationPath], null));
        }
        finally
        {
            TryDeleteFile(temporaryPath);
        }
    }

    private async Task<Result<SpriteDocumentExportResult>> ExportPngAsync(
        SpriteDocumentExportRequest request,
        bool sheet,
        CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.GetFullPath(request.OutputPath);
        var overwriteResult = CheckManagedDirectoryOverwrite(destinationDirectory, request.OverwritePolicy);
        if (overwriteResult.IsFailure)
        {
            return Result.Failure<SpriteDocumentExportResult>(overwriteResult.Error);
        }

        var sidecarFileName = $"{SanitizeName(request.Document.Name)}.adaptive-dmi.json";
        if (!overwriteResult.Value)
        {
            return Result.Success(new SpriteDocumentExportResult(
                destinationDirectory,
                [],
                Path.Combine(destinationDirectory, sidecarFileName)));
        }

        var parentDirectory = Path.GetDirectoryName(destinationDirectory)!;
        Directory.CreateDirectory(parentDirectory);
        var stagingDirectory = Path.Combine(
            parentDirectory,
            $".{Path.GetFileName(destinationDirectory)}.{Guid.NewGuid():N}.staging");
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            var exportedDocumentResult = sheet
                ? await WritePngSheetAsync(request.Document, stagingDirectory, destinationDirectory, cancellationToken).ConfigureAwait(false)
                : await WritePngSequenceAsync(request.Document, stagingDirectory, destinationDirectory, cancellationToken).ConfigureAwait(false);
            if (exportedDocumentResult.IsFailure)
            {
                return Result.Failure<SpriteDocumentExportResult>(exportedDocumentResult.Error);
            }

            var sidecarPath = Path.Combine(stagingDirectory, sidecarFileName);
            var sidecarSave = await documentRepository
                .SaveAsync(sidecarPath, exportedDocumentResult.Value, cancellationToken)
                .ConfigureAwait(false);
            if (sidecarSave.IsFailure)
            {
                return Result.Failure<SpriteDocumentExportResult>(sidecarSave.Error);
            }

            var relativeFiles = Directory
                .EnumerateFiles(stagingDirectory, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(stagingDirectory, path))
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToList();
            relativeFiles.Add(ManagedExportManifestFileName);
            var manifest = new ManagedExportManifest(
                ManagedExportManifestVersion,
                ManagedExportOwner,
                request.Document.Id,
                sheet ? SpriteDocumentExportFormat.PngSheet : SpriteDocumentExportFormat.PngSequence,
                relativeFiles.OrderBy(static path => path, StringComparer.Ordinal).ToArray());
            var manifestPath = Path.Combine(stagingDirectory, ManagedExportManifestFileName);
            await File.WriteAllTextAsync(
                manifestPath,
                JsonConvert.SerializeObject(manifest, ManifestSerializerSettings) + Environment.NewLine,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            CommitManagedDirectory(stagingDirectory, destinationDirectory);

            var writtenFiles = relativeFiles
                .Select(path => Path.Combine(destinationDirectory, path))
                .ToArray();
            return Result.Success(new SpriteDocumentExportResult(
                destinationDirectory,
                writtenFiles,
                Path.Combine(destinationDirectory, sidecarFileName)));
        }
        finally
        {
            TryDeleteDirectory(stagingDirectory);
        }
    }

    private async Task<Result<SpriteDocument>> WritePngSheetAsync(
        SpriteDocument document,
        string stagingDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var maximumFrames = document.States.Max(static state => state.FramesPerDirection);
        var directionRows = document.States.Sum(static state => (int)state.DirectionDepth);
        var sheetWidth = checked(maximumFrames * document.Resolution.Width);
        var sheetHeight = checked(directionRows * document.Resolution.Height);
        ValidateRasterDimensions(sheetWidth, sheetHeight);

        using var sheet = new Image<Rgba32>(sheetWidth, sheetHeight, Color.Transparent);
        var exportedFrames = new List<(SpriteDocumentState State, SpriteDocumentFrame Frame, SpriteSourceRectangle Crop)>();
        var row = 0;
        foreach (var state in document.States)
        {
            foreach (var direction in state.DirectionDepth.GetDirections())
            {
                for (var frameIndex = 0; frameIndex < state.FramesPerDirection; frameIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var frameResult = await frameSource.ReadAsync(
                        new SpriteFrameReadRequest(document, state.Name, direction, frameIndex),
                        cancellationToken).ConfigureAwait(false);
                    if (frameResult.IsFailure)
                    {
                        return Result.Failure<SpriteDocument>(frameResult.Error);
                    }

                    var targetX = frameIndex * document.Resolution.Width;
                    var targetY = row * document.Resolution.Height;
                    CopyToImage(frameResult.Value, sheet, targetX, targetY);
                    var originalFrame = state.Frames.First(frame => frame.Direction == direction && frame.FrameIndex == frameIndex);
                    exportedFrames.Add((state, originalFrame, new SpriteSourceRectangle(
                        targetX,
                        targetY,
                        document.Resolution.Width,
                        document.Resolution.Height)));
                }

                row++;
            }
        }

        var fileName = $"{SanitizeName(document.Name)}.png";
        var stagingPath = Path.Combine(stagingDirectory, fileName);
        await sheet.SaveAsPngAsync(stagingPath, cancellationToken).ConfigureAwait(false);
        var probeResult = await probeService.ProbeAsync(stagingPath, AssetImportLimits.Default, cancellationToken).ConfigureAwait(false);
        if (probeResult.IsFailure)
        {
            return Result.Failure<SpriteDocument>(probeResult.Error);
        }

        var sourceId = CreateDeterministicGuid(document.Id, "png-sheet");
        var source = CreateExportSource(
            sourceId,
            fileName,
            Path.Combine(destinationDirectory, fileName),
            probeResult.Value);
        var states = document.States.Select(state => new SpriteDocumentState(
            state.Name,
            state.DirectionDepth,
            state.FramesPerDirection,
            state.Animation,
            exportedFrames
                .Where(item => ReferenceEquals(item.State, state))
                .Select(item => new SpriteDocumentFrame(
                    item.Frame.Direction,
                    item.Frame.FrameIndex,
                    new SpriteFrameReference(sourceId, item.Crop)))
                .ToArray())).ToArray();

        return Result.Success(new SpriteDocument(
            document.Id,
            document.Name,
            document.Resolution,
            [source],
            states));
    }

    private async Task<Result<SpriteDocument>> WritePngSequenceAsync(
        SpriteDocument document,
        string stagingDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var sources = new List<SpriteSourceReference>();
        var states = new List<SpriteDocumentState>(document.States.Count);
        for (var stateIndex = 0; stateIndex < document.States.Count; stateIndex++)
        {
            var state = document.States[stateIndex];
            var stateDirectoryName = $"{stateIndex:D4}-{SanitizeName(state.Name)}";
            var stateDirectory = Path.Combine(stagingDirectory, stateDirectoryName);
            Directory.CreateDirectory(stateDirectory);
            var frames = new List<SpriteDocumentFrame>(state.Frames.Count);

            foreach (var direction in state.DirectionDepth.GetDirections())
            {
                var directionDirectoryName = direction.ToString().ToLowerInvariant();
                var directionDirectory = Path.Combine(stateDirectory, directionDirectoryName);
                Directory.CreateDirectory(directionDirectory);
                for (var frameIndex = 0; frameIndex < state.FramesPerDirection; frameIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var frameResult = await frameSource.ReadAsync(
                        new SpriteFrameReadRequest(document, state.Name, direction, frameIndex),
                        cancellationToken).ConfigureAwait(false);
                    if (frameResult.IsFailure)
                    {
                        return Result.Failure<SpriteDocument>(frameResult.Error);
                    }

                    var fileName = $"frame-{frameIndex:D4}.png";
                    var relativePath = Path.Combine(stateDirectoryName, directionDirectoryName, fileName);
                    var stagingPath = Path.Combine(stagingDirectory, relativePath);
                    using (var image = Image.LoadPixelData<Rgba32>(
                        frameResult.Value.RgbaBytes,
                        frameResult.Value.Width,
                        frameResult.Value.Height))
                    {
                        await image.SaveAsPngAsync(stagingPath, cancellationToken).ConfigureAwait(false);
                    }

                    var probeResult = await probeService.ProbeAsync(stagingPath, AssetImportLimits.Default, cancellationToken).ConfigureAwait(false);
                    if (probeResult.IsFailure)
                    {
                        return Result.Failure<SpriteDocument>(probeResult.Error);
                    }

                    var sourceId = CreateDeterministicGuid(document.Id, $"{stateIndex}:{direction}:{frameIndex}");
                    sources.Add(CreateExportSource(
                        sourceId,
                        relativePath,
                        Path.Combine(destinationDirectory, relativePath),
                        probeResult.Value));
                    frames.Add(new SpriteDocumentFrame(
                        direction,
                        frameIndex,
                        new SpriteFrameReference(
                            sourceId,
                            new SpriteSourceRectangle(0, 0, document.Resolution.Width, document.Resolution.Height))));
                }
            }

            states.Add(new SpriteDocumentState(
                state.Name,
                state.DirectionDepth,
                state.FramesPerDirection,
                state.Animation,
                frames));
        }

        return Result.Success(new SpriteDocument(
            document.Id,
            document.Name,
            document.Resolution,
            sources,
            states));
    }

    private static Result<bool> CheckFileOverwrite(string destinationPath, OverwritePolicy overwritePolicy)
    {
        if (!File.Exists(destinationPath))
        {
            return Result.Success(true);
        }

        return overwritePolicy switch
        {
            OverwritePolicy.SkipExisting => Result.Success(false),
            OverwritePolicy.OverwriteExisting => Result.Success(true),
            OverwritePolicy.FailIfExists => Result.Failure<bool>(Errors.Conflict($"Output file '{destinationPath}' already exists.")),
            _ => Result.Failure<bool>(Errors.Validation("Unsupported overwrite policy."))
        };
    }

    private static Result<bool> CheckManagedDirectoryOverwrite(string destinationDirectory, OverwritePolicy overwritePolicy)
    {
        if (!Directory.Exists(destinationDirectory))
        {
            return Result.Success(true);
        }

        if (overwritePolicy == OverwritePolicy.SkipExisting)
        {
            return Result.Success(false);
        }

        if (overwritePolicy == OverwritePolicy.FailIfExists)
        {
            return Result.Failure<bool>(Errors.Conflict($"Output directory '{destinationDirectory}' already exists."));
        }

        var manifestPath = Path.Combine(destinationDirectory, ManagedExportManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return Result.Failure<bool>(Errors.Conflict(
                $"Output directory '{destinationDirectory}' is not owned by AdaptiveSpritesDMItool and cannot be overwritten."));
        }

        try
        {
            var manifest = JsonConvert.DeserializeObject<ManagedExportManifest>(
                File.ReadAllText(manifestPath),
                ManifestSerializerSettings);
            return manifest is { Version: ManagedExportManifestVersion, Owner: ManagedExportOwner }
                ? Result.Success(true)
                : Result.Failure<bool>(Errors.Conflict(
                    $"Output directory '{destinationDirectory}' has an invalid ownership manifest."));
        }
        catch (JsonException)
        {
            return Result.Failure<bool>(Errors.Conflict(
                $"Output directory '{destinationDirectory}' has an invalid ownership manifest."));
        }
    }

    private static void CommitManagedDirectory(string stagingDirectory, string destinationDirectory)
    {
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.Move(stagingDirectory, destinationDirectory);
            return;
        }

        var parentDirectory = Path.GetDirectoryName(destinationDirectory)!;
        var backupDirectory = Path.Combine(
            parentDirectory,
            $".{Path.GetFileName(destinationDirectory)}.{Guid.NewGuid():N}.backup");
        Directory.Move(destinationDirectory, backupDirectory);
        try
        {
            Directory.Move(stagingDirectory, destinationDirectory);
        }
        catch
        {
            Directory.Move(backupDirectory, destinationDirectory);
            throw;
        }

        TryDeleteDirectory(backupDirectory);
    }

    private static void ApplyAnimationMetadata(
        DMIState state,
        SpriteAnimationMetadata metadata,
        int framesPerDirection)
    {
        if (metadata.Delays.Count > 0)
        {
            state.SetDelay(metadata.Delays.ToArray(), 0, framesPerDirection - 1);
        }

        state.SetLoop(metadata.Loop);
        state.SetRewind(metadata.Rewind);
        state.SetMovement(metadata.Movement);
        state.Data.Hotspots = metadata.Hotspots.Count == 0
            ? null
            : metadata.Hotspots
                .Select(static hotspot => new[] { hotspot.X, hotspot.Y })
                .ToList();
    }

    private static async Task RewriteDmiMetadataAsync(
        string path,
        DMIFile dmiFile,
        CancellationToken cancellationToken)
    {
        var rewrittenPath = Path.Combine(
            Path.GetDirectoryName(path)!,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.metadata.png");
        try
        {
            using var image = await Image.LoadAsync<Rgba32>(path, cancellationToken).ConfigureAwait(false);
            var pngMetadata = image.Metadata.GetFormatMetadata(PngFormat.Instance);
            for (var index = pngMetadata.TextData.Count - 1; index >= 0; index--)
            {
                if (pngMetadata.TextData[index].Keyword.Equals("Description", StringComparison.Ordinal))
                {
                    pngMetadata.TextData.RemoveAt(index);
                }
            }

            pngMetadata.TextData.Add(new PngTextData(
                "Description",
                BuildDmiMetadataText(dmiFile),
                string.Empty,
                string.Empty));
            await image.SaveAsPngAsync(rewrittenPath, cancellationToken).ConfigureAwait(false);
            File.Move(rewrittenPath, path, overwrite: true);
        }
        finally
        {
            TryDeleteFile(rewrittenPath);
        }
    }

    private static string BuildDmiMetadataText(DMIFile dmiFile)
    {
        var builder = new StringBuilder();
        builder.Append("# BEGIN DMI\nversion = ")
            .Append(dmiFile.Metadata.Version.ToString("0.0", CultureInfo.InvariantCulture))
            .Append("\n\twidth = ")
            .Append(dmiFile.Metadata.FrameWidth.ToString(CultureInfo.InvariantCulture))
            .Append("\n\theight = ")
            .Append(dmiFile.Metadata.FrameHeight.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        foreach (var state in dmiFile.States)
        {
            builder.Append("state = \"")
                .Append(state.Name.Replace("\"", "\\\"", StringComparison.Ordinal))
                .Append("\"\n\tdirs = ")
                .Append(state.Dirs.ToString(CultureInfo.InvariantCulture))
                .Append("\n\tframes = ")
                .Append(state.Frames.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
            if (state.Data.Delay is { Length: > 0 } delays)
            {
                builder.Append("\tdelay = ")
                    .AppendJoin(',', delays.Select(static delay => delay.ToString(CultureInfo.InvariantCulture)))
                    .Append('\n');
            }

            if (state.Data.Loop > 0)
            {
                builder.Append("\tloop = ")
                    .Append(state.Data.Loop.ToString(CultureInfo.InvariantCulture))
                    .Append('\n');
            }

            if (state.Data.Hotspots is not null)
            {
                foreach (var hotspot in state.Data.Hotspots.Where(static coordinates => coordinates.Length >= 2))
                {
                    builder.Append("\thotspot = ")
                        .Append(hotspot[0].ToString(CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(hotspot[1].ToString(CultureInfo.InvariantCulture))
                        .Append('\n');
                }
            }

            if (state.Data.Movement)
            {
                builder.Append("\tmovement = 1\n");
            }

            if (state.Data.Rewind)
            {
                builder.Append("\trewind = 1\n");
            }
        }

        return builder.Append("# END DMI\n").ToString();
    }

    private static DirectionDepth ToDmiDepth(SpriteDirectionDepth depth) =>
        depth switch
        {
            SpriteDirectionDepth.One => DirectionDepth.One,
            SpriteDirectionDepth.Four => DirectionDepth.Four,
            SpriteDirectionDepth.Eight => DirectionDepth.Eight,
            _ => throw new ArgumentOutOfRangeException(nameof(depth), depth, "Unsupported direction depth.")
        };

    private static SpriteSourceReference CreateExportSource(
        Guid id,
        string relativePath,
        string finalAbsolutePath,
        AssetProbe probe) =>
        new(
            id,
            relativePath,
            finalAbsolutePath,
            SpriteSourceFormat.Png,
            probe.Width,
            probe.Height,
            probe.EncodedLength,
            probe.Sha256);

    private static Guid CreateDeterministicGuid(Guid documentId, string discriminator)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{documentId:N}:{discriminator}"));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static void ValidateRasterDimensions(int width, int height)
    {
        var limits = AssetImportLimits.Default;
        if (width <= 0 || height <= 0 || width > limits.MaximumDimension || height > limits.MaximumDimension)
        {
            throw new ArgumentException($"Raster export dimensions must not exceed {limits.MaximumDimension} pixels.");
        }

        if (checked((long)width * height) > limits.MaximumDecodedPixels)
        {
            throw new ArgumentException($"Raster export exceeds the {limits.MaximumDecodedPixels} pixel limit.");
        }
    }

    private static void CopyToImage(SpriteImage source, Image<Rgba32> destination, int targetX, int targetY)
    {
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var sourceOffset = ((y * source.Width) + x) * 4;
                destination[targetX + x, targetY + y] = new Rgba32(
                    source.RgbaBytes[sourceOffset],
                    source.RgbaBytes[sourceOffset + 1],
                    source.RgbaBytes[sourceOffset + 2],
                    source.RgbaBytes[sourceOffset + 3]);
            }
        }
    }

    private static string SanitizeName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "sprite" : sanitized;
    }

    private static void TryDeleteFile(string path)
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

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record ManagedExportManifest(
        int Version,
        string Owner,
        Guid DocumentId,
        SpriteDocumentExportFormat Format,
        IReadOnlyList<string> Files);
}
