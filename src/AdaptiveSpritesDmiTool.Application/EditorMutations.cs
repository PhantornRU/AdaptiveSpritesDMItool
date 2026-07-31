using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;

namespace AdaptiveSpritesDmiTool.Application;

public enum DirectionPropagationScope
{
    ActiveOnly = 0,
    Parallel = 1,
    All = 2
}

public sealed record DirectionProjectionOptions(
    SpriteResolution Resolution,
    SupportedDirectionSet SupportedDirections,
    SpriteDirection ActiveDirection,
    DirectionPropagationScope Scope,
    bool MirrorAcrossDirections,
    int MirrorAxisOffsetPixels);

public readonly record struct ProjectedPixel(SpriteDirection Direction, PixelCoordinate Coordinate);

public sealed record DirectionProjectionResult(
    IReadOnlyList<ProjectedPixel> Pixels,
    int SkippedProjectionCount);

public static class DirectionProjectionPolicy
{
    public static Result<DirectionProjectionResult> Project(
        PixelCoordinate coordinate,
        DirectionProjectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Resolution.Contains(coordinate))
        {
            return Result.Failure<DirectionProjectionResult>(
                Errors.Validation($"Coordinate '{coordinate}' is outside resolution '{options.Resolution}'."));
        }

        if (!options.SupportedDirections.Supports(options.ActiveDirection))
        {
            return Result.Failure<DirectionProjectionResult>(
                Errors.Validation($"Direction '{options.ActiveDirection}' is not supported."));
        }

        var maximumOffset = SpriteEditorSettings.GetMaximumMirrorAxisOffset(options.Resolution);
        if (Math.Abs((long)options.MirrorAxisOffsetPixels) > maximumOffset)
        {
            return Result.Failure<DirectionProjectionResult>(
                Errors.Validation(
                    $"Mirror axis offset '{options.MirrorAxisOffsetPixels}' is outside the valid range " +
                    $"'{-maximumOffset}..{maximumOffset}' for resolution '{options.Resolution}'."));
        }

        var pixels = new List<ProjectedPixel>();
        var skipped = 0;
        foreach (var direction in ResolveDirections(options))
        {
            var projectedX = coordinate.X;
            if (options.MirrorAcrossDirections && HasOppositeOrientation(options.ActiveDirection, direction))
            {
                var projectedXLong =
                    (long)options.Resolution.Width - 1 - coordinate.X + (2L * options.MirrorAxisOffsetPixels);
                if (projectedXLong < 0 || projectedXLong >= options.Resolution.Width)
                {
                    skipped++;
                    continue;
                }

                projectedX = (int)projectedXLong;
            }

            if (projectedX < 0 || projectedX >= options.Resolution.Width)
            {
                skipped++;
                continue;
            }

            pixels.Add(new ProjectedPixel(direction, new PixelCoordinate(projectedX, coordinate.Y)));
        }

        return Result.Success(
            new DirectionProjectionResult(
                pixels
                    .Distinct()
                    .OrderBy(static pixel => (int)pixel.Direction)
                    .ThenBy(static pixel => pixel.Coordinate.Y)
                    .ThenBy(static pixel => pixel.Coordinate.X)
                    .ToArray(),
                skipped));
    }

    public static IReadOnlyList<SpriteDirection> ResolveDirections(DirectionProjectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Scope switch
        {
            DirectionPropagationScope.ActiveOnly => [options.ActiveDirection],
            DirectionPropagationScope.Parallel => ResolveParallelDirections(options),
            DirectionPropagationScope.All => options.SupportedDirections.GetDirections().ToArray(),
            _ => [options.ActiveDirection]
        };
    }

    public static SpriteDirection? GetParallelDirection(SpriteDirection direction) =>
        direction switch
        {
            SpriteDirection.South => SpriteDirection.North,
            SpriteDirection.North => SpriteDirection.South,
            SpriteDirection.East => SpriteDirection.West,
            SpriteDirection.West => SpriteDirection.East,
            SpriteDirection.SouthEast => SpriteDirection.NorthWest,
            SpriteDirection.NorthWest => SpriteDirection.SouthEast,
            SpriteDirection.SouthWest => SpriteDirection.NorthEast,
            SpriteDirection.NorthEast => SpriteDirection.SouthWest,
            _ => null
        };

    public static bool HasOppositeOrientation(SpriteDirection left, SpriteDirection right) =>
        IsPrimaryOrientation(left) != IsPrimaryOrientation(right);

    private static List<SpriteDirection> ResolveParallelDirections(DirectionProjectionOptions options)
    {
        var directions = new List<SpriteDirection> { options.ActiveDirection };
        if (GetParallelDirection(options.ActiveDirection) is { } parallel &&
            options.SupportedDirections.Supports(parallel))
        {
            directions.Add(parallel);
        }

        return directions;
    }

    private static bool IsPrimaryOrientation(SpriteDirection direction) =>
        direction is SpriteDirection.South
            or SpriteDirection.East
            or SpriteDirection.SouthEast
            or SpriteDirection.SouthWest;
}

public enum EditorMappingMutationKind
{
    Restore = 0,
    SetSource = 1,
    SetTransparent = 2,
    SetSourceForced = 3
}

public sealed record EditorMappingMutation(
    EditorMappingMutationKind Kind,
    SpriteDirection Direction,
    PixelCoordinate EditableCoordinate,
    PixelCoordinate? SourceCoordinate = null);

public sealed record EditorMutationPlan(
    IReadOnlyList<EditorMappingMutation> Operations,
    int SkippedProjectionCount = 0)
{
    public static EditorMutationPlan Empty { get; } = new(Array.Empty<EditorMappingMutation>());
}

public sealed record EditorMutationApplyResult(
    SpriteConfig Config,
    int AppliedOperationCount,
    int SkippedProjectionCount,
    bool IsChanged);

public static class EditorMutationPlanFactory
{
    public static Result<EditorMutationPlan> CreateStroke(
        IEnumerable<PixelCoordinate> coordinates,
        EditorMappingMutationKind kind,
        PixelCoordinate? sourceCoordinate,
        DirectionProjectionOptions projectionOptions)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        ArgumentNullException.ThrowIfNull(projectionOptions);

        var operations = new List<EditorMappingMutation>();
        var skipped = 0;
        foreach (var coordinate in coordinates)
        {
            var projection = DirectionProjectionPolicy.Project(coordinate, projectionOptions);
            if (projection.IsFailure)
            {
                return Result.Failure<EditorMutationPlan>(projection.Error);
            }

            skipped += projection.Value.SkippedProjectionCount;
            operations.AddRange(
                projection.Value.Pixels.Select(pixel =>
                    new EditorMappingMutation(kind, pixel.Direction, pixel.Coordinate, sourceCoordinate)));
        }

        return EditorMutationEngine.Normalize(new EditorMutationPlan(operations, skipped));
    }
}

public static class EditorMutationEngine
{
    public static Result<EditorMutationPlan> Normalize(EditorMutationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var restores = new Dictionary<MutationKey, EditorMappingMutation>();
        var writes = new Dictionary<MutationKey, EditorMappingMutation>();
        foreach (var operation in plan.Operations)
        {
            var key = new MutationKey(operation.Direction, operation.EditableCoordinate);
            if (operation.Kind == EditorMappingMutationKind.Restore)
            {
                restores.TryAdd(key, operation);
                continue;
            }

            if (writes.TryGetValue(key, out var existing))
            {
                if (existing != operation)
                {
                    return Result.Failure<EditorMutationPlan>(
                        Errors.Conflict(
                            $"Conflicting editor mutations target '{operation.Direction}:{operation.EditableCoordinate}'."));
                }

                continue;
            }

            writes.Add(key, operation);
        }

        var normalized = restores.Values
            .OrderBy(static operation => (int)operation.Direction)
            .ThenBy(static operation => operation.EditableCoordinate.Y)
            .ThenBy(static operation => operation.EditableCoordinate.X)
            .Concat(
                writes.Values
                    .OrderBy(static operation => (int)operation.Direction)
                    .ThenBy(static operation => operation.EditableCoordinate.Y)
                    .ThenBy(static operation => operation.EditableCoordinate.X))
            .ToArray();

        return Result.Success(new EditorMutationPlan(normalized, plan.SkippedProjectionCount));
    }

    public static Result<EditorMutationApplyResult> Apply(SpriteConfig config, EditorMutationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(plan);

        var normalized = Normalize(plan);
        if (normalized.IsFailure)
        {
            return Result.Failure<EditorMutationApplyResult>(normalized.Error);
        }

        var validation = Validate(config, normalized.Value);
        if (validation.IsFailure)
        {
            return Result.Failure<EditorMutationApplyResult>(validation.Error);
        }

        try
        {
            var next = config.Clone();
            foreach (var operation in normalized.Value.Operations)
            {
                next = operation.Kind switch
                {
                    EditorMappingMutationKind.Restore =>
                        next.RemoveMapping(operation.Direction, operation.EditableCoordinate),
                    EditorMappingMutationKind.SetSource =>
                        next.SetMapping(operation.Direction, operation.EditableCoordinate, operation.SourceCoordinate),
                    EditorMappingMutationKind.SetTransparent =>
                        next.SetMapping(operation.Direction, operation.EditableCoordinate, null),
                    EditorMappingMutationKind.SetSourceForced =>
                        next.SetMappingForced(operation.Direction, operation.EditableCoordinate, operation.SourceCoordinate),
                    _ => throw new InvalidOperationException($"Unsupported editor mutation '{operation.Kind}'.")
                };
            }

            var isChanged = !config.HasSameMappingContent(next);
            return Result.Success(
                new EditorMutationApplyResult(
                    isChanged ? next : config,
                    normalized.Value.Operations.Count,
                    normalized.Value.SkippedProjectionCount,
                    isChanged));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<EditorMutationApplyResult>(Errors.Validation(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<EditorMutationApplyResult>(Errors.Conflict(exception.Message));
        }
    }

    private static Result Validate(SpriteConfig config, EditorMutationPlan plan)
    {
        foreach (var operation in plan.Operations)
        {
            if (!Enum.IsDefined(operation.Kind))
            {
                return Result.Failure(Errors.Validation($"Unsupported editor mutation '{operation.Kind}'."));
            }

            if (!config.SupportedDirections.Supports(operation.Direction))
            {
                return Result.Failure(
                    Errors.Validation($"Direction '{operation.Direction}' is not supported by the active config."));
            }

            if (!config.Resolution.Contains(operation.EditableCoordinate))
            {
                return Result.Failure(
                    Errors.Validation($"Editable coordinate '{operation.EditableCoordinate}' is outside '{config.Resolution}'."));
            }

            if (operation.SourceCoordinate is { } source && !config.Resolution.Contains(source))
            {
                return Result.Failure(
                    Errors.Validation($"Source coordinate '{source}' is outside '{config.Resolution}'."));
            }

            if (operation.Kind == EditorMappingMutationKind.SetSource && operation.SourceCoordinate is null)
            {
                return Result.Failure(Errors.Validation("SetSource mutations require a source coordinate."));
            }

            if (operation.Kind is EditorMappingMutationKind.Restore or EditorMappingMutationKind.SetTransparent &&
                operation.SourceCoordinate is not null)
            {
                return Result.Failure(
                    Errors.Validation($"Mutation '{operation.Kind}' must not include a source coordinate."));
            }
        }

        return Result.Success();
    }

    private readonly record struct MutationKey(SpriteDirection Direction, PixelCoordinate Coordinate);
}

public static class PixelStrokeInterpolator
{
    public static IReadOnlyList<PixelCoordinate> Interpolate(IEnumerable<PixelCoordinate> sampledCoordinates)
    {
        ArgumentNullException.ThrowIfNull(sampledCoordinates);

        var samples = sampledCoordinates.ToArray();
        if (samples.Length == 0)
        {
            return Array.Empty<PixelCoordinate>();
        }

        var result = new List<PixelCoordinate>();
        var seen = new HashSet<PixelCoordinate>();
        Add(samples[0]);
        for (var index = 1; index < samples.Length; index++)
        {
            foreach (var coordinate in InterpolateSegment(samples[index - 1], samples[index]).Skip(1))
            {
                Add(coordinate);
            }
        }

        return result;

        void Add(PixelCoordinate coordinate)
        {
            if (seen.Add(coordinate))
            {
                result.Add(coordinate);
            }
        }
    }

    public static IReadOnlyList<PixelCoordinate> InterpolateSegment(PixelCoordinate start, PixelCoordinate end)
    {
        var result = new List<PixelCoordinate> { start };
        var x = start.X;
        var y = start.Y;
        var deltaX = Math.Abs(end.X - start.X);
        var deltaY = Math.Abs(end.Y - start.Y);
        var stepX = Math.Sign(end.X - start.X);
        var stepY = Math.Sign(end.Y - start.Y);
        var progressedX = 0;
        var progressedY = 0;

        while (progressedX < deltaX || progressedY < deltaY)
        {
            var decision = ((1 + (2 * progressedX)) * deltaY) - ((1 + (2 * progressedY)) * deltaX);
            if (decision == 0)
            {
                x += stepX;
                y += stepY;
                progressedX++;
                progressedY++;
            }
            else if (decision < 0)
            {
                x += stepX;
                progressedX++;
            }
            else
            {
                y += stepY;
                progressedY++;
            }

            result.Add(new PixelCoordinate(x, y));
        }

        return result;
    }
}
