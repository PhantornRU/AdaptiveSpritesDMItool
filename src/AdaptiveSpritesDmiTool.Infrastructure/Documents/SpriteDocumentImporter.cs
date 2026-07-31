using System.Collections;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using DMISharp;

namespace AdaptiveSpritesDmiTool.Infrastructure.Documents;

public sealed class SpriteDocumentImporter(IAssetProbeService probeService) : ISpriteDocumentImporter
{
    public async Task<Result<SpriteDocument>> ImportAsync(
        SpriteDocumentImportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourcePaths is null || request.SourcePaths.Count == 0)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("At least one source path is required."));
        }

        if (string.IsNullOrWhiteSpace(request.DocumentName))
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("Document name is required."));
        }

        var limits = request.Limits ?? AssetImportLimits.Default;
        try
        {
            var probes = new List<AssetProbe>(request.SourcePaths.Count);
            foreach (var path in request.SourcePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var probeResult = await probeService.ProbeAsync(path, limits, cancellationToken).ConfigureAwait(false);
                if (probeResult.IsFailure)
                {
                    return Result.Failure<SpriteDocument>(probeResult.Error);
                }

                probes.Add(probeResult.Value);
            }

            var budgetError = ValidateCombinedBudget(probes, limits);
            if (budgetError is not null)
            {
                return Result.Failure<SpriteDocument>(Errors.Validation(budgetError));
            }

            return request.Kind switch
            {
                SpriteDocumentImportKind.NativeDmi => ImportDmi(request, probes, limits, cancellationToken),
                SpriteDocumentImportKind.SingleRaster => ImportSingleRaster(request, probes),
                SpriteDocumentImportKind.RasterSequence => ImportRasterSequence(request, probes, limits),
                SpriteDocumentImportKind.SpriteSheet => ImportSpriteSheet(request, probes, limits),
                _ => Result.Failure<SpriteDocument>(Errors.Validation("Unsupported document import kind."))
            };
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpriteDocument>(Errors.Cancelled("Sprite document import was cancelled."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation($"Sprite document import is invalid: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<SpriteDocument>(Errors.Unexpected($"Failed to import sprite document: {exception.Message}"));
        }
    }

    private static Result<SpriteDocument> ImportDmi(
        SpriteDocumentImportRequest request,
        List<AssetProbe> probes,
        AssetImportLimits limits,
        CancellationToken cancellationToken)
    {
        if (probes.Count != 1)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("Native DMI import accepts exactly one source."));
        }

        var probe = probes[0];
        if (probe.DetectedFormat != SpriteSourceFormat.Dmi)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("The selected source does not contain DMI metadata."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var dmiFile = new DMIFile(probe.SourcePath);
        if (dmiFile.States.Count == 0)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("DMI source does not contain any states."));
        }

        if (dmiFile.States.Count > limits.MaximumStates)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation($"DMI exceeds the {limits.MaximumStates} state limit."));
        }

        var resolution = DmiSharpConversions.InferResolution(dmiFile);
        var source = CreateSourceReference(probe, request.ProjectPath);
        var states = new List<SpriteDocumentState>(dmiFile.States.Count);
        var totalFrames = 0;
        foreach (var dmiState in dmiFile.States)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directionDepth = DmiSharpConversions.ToDomainDepth(dmiState.DirectionDepth);
            var framesPerDirection = dmiState.Data.Frames;
            if (framesPerDirection <= 0)
            {
                return Result.Failure<SpriteDocument>(Errors.Validation($"DMI state '{dmiState.Name}' has no frames."));
            }

            totalFrames = checked(totalFrames + checked((int)directionDepth * framesPerDirection));
            if (totalFrames > limits.MaximumFramesOrCells)
            {
                return Result.Failure<SpriteDocument>(Errors.Validation($"DMI exceeds the {limits.MaximumFramesOrCells} frame limit."));
            }

            var frames = new List<SpriteDocumentFrame>((int)directionDepth * framesPerDirection);
            foreach (var dmiDirection in DmiSharpConversions.GetDirections(dmiState.DirectionDepth))
            {
                var direction = DmiSharpConversions.ToDomainDirection(dmiDirection);
                for (var frameIndex = 0; frameIndex < framesPerDirection; frameIndex++)
                {
                    frames.Add(new SpriteDocumentFrame(
                        direction,
                        frameIndex,
                        new SpriteFrameReference(
                            source.Id,
                            new SpriteSourceRectangle(0, 0, resolution.Width, resolution.Height),
                            frameIndex,
                            sourceStateName: dmiState.Name,
                            sourceDirection: direction)));
                }
            }

            states.Add(new SpriteDocumentState(
                dmiState.Name,
                directionDepth,
                framesPerDirection,
                new SpriteAnimationMetadata(
                    dmiState.Data.Delay,
                    dmiState.Data.Loop,
                    dmiState.Data.Rewind,
                    dmiState.Data.Movement,
                    ReadHotspots(dmiState.Data.Hotspots)),
                frames));
        }

        return Result.Success(new SpriteDocument(
            Guid.NewGuid(),
            request.DocumentName,
            resolution,
            [source],
            states));
    }

    private static Result<SpriteDocument> ImportSingleRaster(
        SpriteDocumentImportRequest request,
        List<AssetProbe> probes)
    {
        if (probes.Count != 1)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("Single-raster import accepts exactly one source."));
        }

        var probe = probes[0];
        var rasterError = ValidateRasterProbe(probe);
        if (rasterError is not null)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(rasterError));
        }

        var resolution = request.FrameResolution ?? new SpriteResolution(probe.Width, probe.Height);
        var source = CreateSourceReference(probe, request.ProjectPath);
        var crop = CreateTopLeftCrop(probe, resolution);
        var stateName = ResolveStateName(request.StateName, probe.SourcePath);
        var frame = new SpriteDocumentFrame(
            SpriteDirection.South,
            0,
            new SpriteFrameReference(source.Id, crop));

        return Result.Success(new SpriteDocument(
            Guid.NewGuid(),
            request.DocumentName,
            resolution,
            [source],
            [new SpriteDocumentState(stateName, SpriteDirectionDepth.One, 1, SpriteAnimationMetadata.Static, [frame])]));
    }

    private static Result<SpriteDocument> ImportRasterSequence(
        SpriteDocumentImportRequest request,
        List<AssetProbe> probes,
        AssetImportLimits limits)
    {
        foreach (var probe in probes)
        {
            var rasterError = ValidateRasterProbe(probe);
            if (rasterError is not null)
            {
                return Result.Failure<SpriteDocument>(Errors.Validation(rasterError));
            }
        }

        var directionsResult = ResolveDirectionOrder(request.DirectionDepth, request.DirectionOrder);
        if (directionsResult.Error is not null)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(directionsResult.Error));
        }

        var directions = directionsResult.Directions;
        if (probes.Count % directions.Count != 0)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("Raster sequence count must be divisible by its direction count."));
        }

        if (probes.Count > limits.MaximumFramesOrCells)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation($"Raster sequence exceeds the {limits.MaximumFramesOrCells} frame limit."));
        }

        var resolution = request.FrameResolution ?? new SpriteResolution(probes[0].Width, probes[0].Height);
        var sources = probes.Select(probe => CreateSourceReference(probe, request.ProjectPath)).ToArray();
        var framesPerDirection = probes.Count / directions.Count;
        var frames = new List<SpriteDocumentFrame>(probes.Count);
        var sourceIndex = 0;
        foreach (var direction in directions)
        {
            for (var frameIndex = 0; frameIndex < framesPerDirection; frameIndex++)
            {
                frames.Add(new SpriteDocumentFrame(
                    direction,
                    frameIndex,
                    new SpriteFrameReference(
                        sources[sourceIndex].Id,
                        CreateTopLeftCrop(probes[sourceIndex], resolution))));
                sourceIndex++;
            }
        }

        var stateName = ResolveStateName(request.StateName, probes[0].SourcePath);
        return Result.Success(new SpriteDocument(
            Guid.NewGuid(),
            request.DocumentName,
            resolution,
            sources,
            [new SpriteDocumentState(stateName, request.DirectionDepth, framesPerDirection, SpriteAnimationMetadata.Static, frames)]));
    }

    private static Result<SpriteDocument> ImportSpriteSheet(
        SpriteDocumentImportRequest request,
        List<AssetProbe> probes,
        AssetImportLimits limits)
    {
        if (probes.Count != 1 || request.SlicingRecipe is null)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation("Sprite-sheet import requires one source and a slicing recipe."));
        }

        var probe = probes[0];
        var rasterError = ValidateRasterProbe(probe);
        if (rasterError is not null)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(rasterError));
        }

        var recipe = request.SlicingRecipe;
        var recipeError = ValidateSlicingRecipe(recipe, probe, limits);
        if (recipeError is not null)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(recipeError));
        }

        var depth = recipe.DirectionOrder.Count switch
        {
            1 => SpriteDirectionDepth.One,
            4 => SpriteDirectionDepth.Four,
            8 => SpriteDirectionDepth.Eight,
            _ => throw new InvalidOperationException("Slicing direction order must contain 1, 4, or 8 entries.")
        };
        var directionResult = ResolveDirectionOrder(depth, recipe.DirectionOrder);
        if (directionResult.Error is not null)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(directionResult.Error));
        }

        var source = CreateSourceReference(probe, request.ProjectPath);
        var frames = new List<SpriteDocumentFrame>(checked(recipe.DirectionOrder.Count * recipe.FramesPerDirection));
        var cellIndex = 0;
        foreach (var direction in recipe.DirectionOrder)
        {
            for (var frameIndex = 0; frameIndex < recipe.FramesPerDirection; frameIndex++)
            {
                var (column, row) = ResolveCell(recipe, cellIndex++);
                frames.Add(new SpriteDocumentFrame(
                    direction,
                    frameIndex,
                    new SpriteFrameReference(
                        source.Id,
                        new SpriteSourceRectangle(
                            recipe.MarginLeft + column * (recipe.CellWidth + recipe.HorizontalSpacing),
                            recipe.MarginTop + row * (recipe.CellHeight + recipe.VerticalSpacing),
                            recipe.CellWidth,
                            recipe.CellHeight))));
            }
        }

        var stateName = ResolveStateName(request.StateName, probe.SourcePath);
        return Result.Success(new SpriteDocument(
            Guid.NewGuid(),
            request.DocumentName,
            new SpriteResolution(recipe.CellWidth, recipe.CellHeight),
            [source],
            [new SpriteDocumentState(stateName, depth, recipe.FramesPerDirection, SpriteAnimationMetadata.Static, frames)],
            recipe));
    }

    private static SpriteSourceReference CreateSourceReference(AssetProbe probe, string? projectPath)
    {
        var projectDirectory = string.IsNullOrWhiteSpace(projectPath)
            ? Path.GetDirectoryName(probe.SourcePath)!
            : Path.GetDirectoryName(Path.GetFullPath(projectPath))!;
        var relativePath = Path.GetRelativePath(projectDirectory, probe.SourcePath);
        return new SpriteSourceReference(
            Guid.NewGuid(),
            relativePath,
            probe.SourcePath,
            probe.DetectedFormat,
            probe.Width,
            probe.Height,
            probe.EncodedLength,
            probe.Sha256);
    }

    private static string? ValidateCombinedBudget(List<AssetProbe> probes, AssetImportLimits limits)
    {
        try
        {
            long decodedPixels = 0;
            foreach (var probe in probes)
            {
                decodedPixels = checked(decodedPixels + checked((long)probe.Width * probe.Height * probe.EncodedFrameCount));
            }

            return decodedPixels > limits.MaximumDecodedPixels
                ? $"Combined decoded sources exceed the {limits.MaximumDecodedPixels} pixel limit."
                : null;
        }
        catch (OverflowException)
        {
            return "Combined source dimensions overflow the supported pixel budget.";
        }
    }

    private static string? ValidateRasterProbe(AssetProbe probe)
    {
        if (probe.DetectedFormat != SpriteSourceFormat.Png)
        {
            return $"'{Path.GetFileName(probe.SourcePath)}' contains DMI metadata and must be opened as a native DMI.";
        }

        return probe.EncodedFrameCount == 1
            ? null
            : $"'{Path.GetFileName(probe.SourcePath)}' contains multiple encoded frames; animated PNG import is not part of V2.4.";
    }

    private static SpriteSourceRectangle CreateTopLeftCrop(AssetProbe probe, SpriteResolution resolution) =>
        new(0, 0, Math.Min(probe.Width, resolution.Width), Math.Min(probe.Height, resolution.Height));

    private static string ResolveStateName(string? requestedName, string sourcePath) =>
        string.IsNullOrWhiteSpace(requestedName)
            ? Path.GetFileNameWithoutExtension(sourcePath)
            : requestedName.Trim();

    private static (IReadOnlyList<SpriteDirection> Directions, string? Error) ResolveDirectionOrder(
        SpriteDirectionDepth depth,
        IReadOnlyList<SpriteDirection>? requestedOrder)
    {
        var canonical = depth.GetDirections();
        var directions = requestedOrder?.ToArray() ?? canonical.ToArray();
        if (directions.Length != canonical.Count ||
            directions.Distinct().Count() != directions.Length ||
            directions.Any(direction => !canonical.Contains(direction)))
        {
            return ([], $"Direction order must contain every {depth} direction exactly once.");
        }

        return (directions, null);
    }

    private static string? ValidateSlicingRecipe(
        SpriteSheetSlicingRecipe recipe,
        AssetProbe probe,
        AssetImportLimits limits)
    {
        if (recipe.CellWidth <= 0 || recipe.CellHeight <= 0 || recipe.Columns <= 0 || recipe.Rows <= 0)
        {
            return "Sprite-sheet cell dimensions, columns, and rows must be positive.";
        }

        if (recipe.MarginLeft < 0 || recipe.MarginTop < 0 || recipe.HorizontalSpacing < 0 || recipe.VerticalSpacing < 0)
        {
            return "Sprite-sheet margins and spacing must be non-negative.";
        }

        if (recipe.FramesPerDirection <= 0)
        {
            return "Frames per direction must be positive.";
        }

        int totalCells;
        int usedCells;
        long requiredWidth;
        long requiredHeight;
        try
        {
            totalCells = checked(recipe.Columns * recipe.Rows);
            usedCells = checked(recipe.DirectionOrder.Count * recipe.FramesPerDirection);
            requiredWidth = checked((long)recipe.MarginLeft + (long)recipe.Columns * recipe.CellWidth + (long)(recipe.Columns - 1) * recipe.HorizontalSpacing);
            requiredHeight = checked((long)recipe.MarginTop + (long)recipe.Rows * recipe.CellHeight + (long)(recipe.Rows - 1) * recipe.VerticalSpacing);
        }
        catch (OverflowException)
        {
            return "Sprite-sheet recipe arithmetic overflowed.";
        }

        if (totalCells > limits.MaximumFramesOrCells || usedCells > limits.MaximumFramesOrCells)
        {
            return $"Sprite-sheet recipe exceeds the {limits.MaximumFramesOrCells} cell limit.";
        }

        if (usedCells > totalCells)
        {
            return "Sprite-sheet recipe requests more frames than available cells.";
        }

        if (requiredWidth > probe.Width || requiredHeight > probe.Height)
        {
            return "Sprite-sheet grid extends outside the source image.";
        }

        return null;
    }

    private static (int Column, int Row) ResolveCell(SpriteSheetSlicingRecipe recipe, int index) =>
        recipe.ReadingOrder switch
        {
            SpriteSheetReadingOrder.RowsFirst => (index % recipe.Columns, index / recipe.Columns),
            SpriteSheetReadingOrder.ColumnsFirst => (index / recipe.Rows, index % recipe.Rows),
            _ => throw new ArgumentOutOfRangeException(nameof(recipe), recipe.ReadingOrder, "Unsupported sprite-sheet reading order.")
        };

    private static List<SpriteHotspot> ReadHotspots(object? value)
    {
        if (value is not IEnumerable values)
        {
            return [];
        }

        var hotspots = new List<SpriteHotspot>();
        foreach (var item in values)
        {
            if (item is null)
            {
                continue;
            }

            if (item is int[] { Length: >= 2 } coordinates)
            {
                hotspots.Add(new SpriteHotspot(coordinates[0], coordinates[1]));
                continue;
            }

            var type = item.GetType();
            var x = type.GetProperty("X")?.GetValue(item) ?? type.GetField("X")?.GetValue(item);
            var y = type.GetProperty("Y")?.GetValue(item) ?? type.GetField("Y")?.GetValue(item);
            if (x is IConvertible convertibleX && y is IConvertible convertibleY)
            {
                hotspots.Add(new SpriteHotspot(convertibleX.ToInt32(null), convertibleY.ToInt32(null)));
            }
        }

        return hotspots;
    }
}
