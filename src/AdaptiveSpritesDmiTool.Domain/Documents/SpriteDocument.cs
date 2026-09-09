using AdaptiveSpritesDmiTool.Domain.Configurations;

namespace AdaptiveSpritesDmiTool.Domain.Documents;

public enum SpriteSourceFormat
{
    Png = 0,
    Dmi = 1
}

public enum SpriteDirectionDepth
{
    One = 1,
    Four = 4,
    Eight = 8
}

public static class SpriteDirectionDepthExtensions
{
    private static readonly SpriteDirection[] OneDirections = [SpriteDirection.South];
    private static readonly SpriteDirection[] FourDirections =
    [
        SpriteDirection.South,
        SpriteDirection.North,
        SpriteDirection.East,
        SpriteDirection.West
    ];
    private static readonly SpriteDirection[] EightDirections =
    [
        SpriteDirection.South,
        SpriteDirection.North,
        SpriteDirection.East,
        SpriteDirection.West,
        SpriteDirection.SouthEast,
        SpriteDirection.SouthWest,
        SpriteDirection.NorthEast,
        SpriteDirection.NorthWest
    ];

    public static IReadOnlyList<SpriteDirection> GetDirections(this SpriteDirectionDepth depth) =>
        depth switch
        {
            SpriteDirectionDepth.One => OneDirections,
            SpriteDirectionDepth.Four => FourDirections,
            SpriteDirectionDepth.Eight => EightDirections,
            _ => throw new ArgumentOutOfRangeException(nameof(depth), depth, "Direction depth must be 1, 4, or 8.")
        };
}

public readonly record struct SpriteSourceRectangle
{
    public SpriteSourceRectangle(int x, int y, int width, int height)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Source X must be non-negative.");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Source Y must be non-negative.");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Source width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Source height must be positive.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public bool FitsInside(int sourceWidth, int sourceHeight) =>
        (long)X + Width <= sourceWidth && (long)Y + Height <= sourceHeight;
}

public sealed record SpriteFrameTransform
{
    public SpriteFrameTransform(
        bool flipHorizontal = false,
        bool flipVertical = false,
        int clockwiseQuarterTurns = 0,
        int offsetX = 0,
        int offsetY = 0,
        int? outputWidth = null,
        int? outputHeight = null)
    {
        if (clockwiseQuarterTurns is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(clockwiseQuarterTurns), clockwiseQuarterTurns, "Quarter turns must be between 0 and 3.");
        }

        if (outputWidth.HasValue != outputHeight.HasValue)
        {
            throw new ArgumentException("Output width and height must either both be specified or both be omitted.");
        }

        if (outputWidth is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputWidth), outputWidth, "Output width must be positive.");
        }

        if (outputHeight is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputHeight), outputHeight, "Output height must be positive.");
        }

        FlipHorizontal = flipHorizontal;
        FlipVertical = flipVertical;
        ClockwiseQuarterTurns = clockwiseQuarterTurns;
        OffsetX = offsetX;
        OffsetY = offsetY;
        OutputWidth = outputWidth;
        OutputHeight = outputHeight;
    }

    public bool FlipHorizontal { get; }

    public bool FlipVertical { get; }

    public int ClockwiseQuarterTurns { get; }

    public int OffsetX { get; }

    public int OffsetY { get; }

    public int? OutputWidth { get; }

    public int? OutputHeight { get; }

    public static SpriteFrameTransform Identity { get; } = new();
}

public sealed record SpriteSourceReference
{
    public SpriteSourceReference(
        Guid id,
        string relativePath,
        string absolutePathFallback,
        SpriteSourceFormat format,
        int width,
        int height,
        long encodedLength,
        string sha256)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Source id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative source path is required.", nameof(relativePath));
        }

        if (string.IsNullOrWhiteSpace(absolutePathFallback))
        {
            throw new ArgumentException("Absolute source path fallback is required.", nameof(absolutePathFallback));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Source dimensions must be positive.");
        }

        if (encodedLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(encodedLength), encodedLength, "Encoded length must be positive.");
        }

        if (sha256.Length != 64 || sha256.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("SHA-256 must contain exactly 64 hexadecimal characters.", nameof(sha256));
        }

        Id = id;
        RelativePath = relativePath;
        AbsolutePathFallback = absolutePathFallback;
        Format = format;
        Width = width;
        Height = height;
        EncodedLength = encodedLength;
        Sha256 = sha256.ToLowerInvariant();
    }

    public Guid Id { get; }

    public string RelativePath { get; }

    public string AbsolutePathFallback { get; }

    public SpriteSourceFormat Format { get; }

    public int Width { get; }

    public int Height { get; }

    public long EncodedLength { get; }

    public string Sha256 { get; }
}

public sealed record SpriteFrameReference
{
    public SpriteFrameReference(
        Guid sourceId,
        SpriteSourceRectangle sourceRectangle,
        int sourceFrameIndex = 0,
        SpriteFrameTransform? transform = null,
        string? sourceStateName = null,
        SpriteDirection? sourceDirection = null)
    {
        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException("Source id must not be empty.", nameof(sourceId));
        }

        if (sourceFrameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceFrameIndex), sourceFrameIndex, "Source frame index must be non-negative.");
        }

        if (sourceStateName is not null && string.IsNullOrWhiteSpace(sourceStateName))
        {
            throw new ArgumentException("Source state name cannot be whitespace.", nameof(sourceStateName));
        }

        SourceId = sourceId;
        SourceRectangle = sourceRectangle;
        SourceFrameIndex = sourceFrameIndex;
        Transform = transform ?? SpriteFrameTransform.Identity;
        SourceStateName = sourceStateName;
        SourceDirection = sourceDirection;
    }

    public Guid SourceId { get; }

    public int SourceFrameIndex { get; }

    public SpriteSourceRectangle SourceRectangle { get; }

    public SpriteFrameTransform Transform { get; }

    public string? SourceStateName { get; }

    public SpriteDirection? SourceDirection { get; }
}

public sealed record SpriteHotspot(int X, int Y);

public sealed record SpriteAnimationMetadata
{
    public SpriteAnimationMetadata(
        IReadOnlyList<double>? delays = null,
        int loop = 0,
        bool rewind = false,
        bool movement = false,
        IReadOnlyList<SpriteHotspot>? hotspots = null)
    {
        if (loop < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(loop), loop, "Loop must be non-negative.");
        }

        var normalizedDelays = delays?.ToArray() ?? [];
        if (normalizedDelays.Any(static delay => !double.IsFinite(delay) || delay <= 0))
        {
            throw new ArgumentException("Animation delays must be finite positive values.", nameof(delays));
        }

        Delays = normalizedDelays;
        Loop = loop;
        Rewind = rewind;
        Movement = movement;
        Hotspots = hotspots?.ToArray() ?? [];
    }

    public IReadOnlyList<double> Delays { get; }

    public int Loop { get; }

    public bool Rewind { get; }

    public bool Movement { get; }

    public IReadOnlyList<SpriteHotspot> Hotspots { get; }

    public static SpriteAnimationMetadata Static { get; } = new();
}

public sealed record SpriteDocumentFrame
{
    public SpriteDocumentFrame(
        SpriteDirection direction,
        int frameIndex,
        SpriteFrameReference source)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex), frameIndex, "Frame index must be non-negative.");
        }

        ArgumentNullException.ThrowIfNull(source);

        Direction = direction;
        FrameIndex = frameIndex;
        Source = source;
    }

    public SpriteDirection Direction { get; }

    public int FrameIndex { get; }

    public SpriteFrameReference Source { get; }
}

public sealed class SpriteDocumentState
{
    public SpriteDocumentState(
        string name,
        SpriteDirectionDepth directionDepth,
        int framesPerDirection,
        SpriteAnimationMetadata animation,
        IReadOnlyList<SpriteDocumentFrame> frames)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("State name is required.", nameof(name));
        }

        if (framesPerDirection <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerDirection), framesPerDirection, "Frames per direction must be positive.");
        }

        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(frames);

        var normalizedFrames = frames.ToArray();
        var directions = directionDepth.GetDirections();
        var expectedFrameCount = checked(directions.Count * framesPerDirection);
        if (normalizedFrames.Length != expectedFrameCount)
        {
            throw new ArgumentException($"State '{name}' must contain exactly {expectedFrameCount} frame references.", nameof(frames));
        }

        if (animation.Delays.Count is not 0 && animation.Delays.Count != framesPerDirection)
        {
            throw new ArgumentException("Delay count must be zero or match frames per direction.", nameof(animation));
        }

        foreach (var direction in directions)
        {
            for (var frameIndex = 0; frameIndex < framesPerDirection; frameIndex++)
            {
                if (normalizedFrames.Count(frame => frame.Direction == direction && frame.FrameIndex == frameIndex) != 1)
                {
                    throw new ArgumentException($"State '{name}' must contain one '{direction}' frame at index {frameIndex}.", nameof(frames));
                }
            }
        }

        if (normalizedFrames.Any(frame => !directions.Contains(frame.Direction) || frame.FrameIndex >= framesPerDirection))
        {
            throw new ArgumentException($"State '{name}' contains a frame outside its direction or frame range.", nameof(frames));
        }

        Name = name;
        DirectionDepth = directionDepth;
        FramesPerDirection = framesPerDirection;
        Animation = animation;
        Frames = normalizedFrames;
    }

    public string Name { get; }

    public SpriteDirectionDepth DirectionDepth { get; }

    public int FramesPerDirection { get; }

    public SpriteAnimationMetadata Animation { get; }

    public IReadOnlyList<SpriteDocumentFrame> Frames { get; }
}

public enum SpriteSheetReadingOrder
{
    RowsFirst = 0,
    ColumnsFirst = 1
}

public sealed record SpriteSheetSlicingRecipe(
    int CellWidth,
    int CellHeight,
    int MarginLeft,
    int MarginTop,
    int HorizontalSpacing,
    int VerticalSpacing,
    int Columns,
    int Rows,
    SpriteSheetReadingOrder ReadingOrder,
    IReadOnlyList<SpriteDirection> DirectionOrder,
    int FramesPerDirection);

public sealed class SpriteDocument
{
    public SpriteDocument(
        Guid id,
        string name,
        SpriteResolution resolution,
        IReadOnlyList<SpriteSourceReference> sources,
        IReadOnlyList<SpriteDocumentState> states,
        SpriteSheetSlicingRecipe? slicingRecipe = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Document id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Document name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(states);

        var normalizedSources = sources.ToArray();
        var normalizedStates = states.ToArray();
        if (normalizedSources.Length == 0)
        {
            throw new ArgumentException("Document must contain at least one source.", nameof(sources));
        }

        if (normalizedStates.Length == 0)
        {
            throw new ArgumentException("Document must contain at least one state.", nameof(states));
        }

        if (normalizedSources.Select(static source => source.Id).Distinct().Count() != normalizedSources.Length)
        {
            throw new ArgumentException("Document source ids must be unique.", nameof(sources));
        }

        if (normalizedStates.Select(static state => state.Name).Distinct(StringComparer.Ordinal).Count() != normalizedStates.Length)
        {
            throw new ArgumentException("Document state names must be unique.", nameof(states));
        }

        var sourcesById = normalizedSources.ToDictionary(static source => source.Id);
        foreach (var frame in normalizedStates.SelectMany(static state => state.Frames))
        {
            if (!sourcesById.TryGetValue(frame.Source.SourceId, out var source))
            {
                throw new ArgumentException($"Frame source '{frame.Source.SourceId}' is not part of the document.", nameof(states));
            }

            if (!frame.Source.SourceRectangle.FitsInside(source.Width, source.Height))
            {
                throw new ArgumentException($"Frame crop for source '{source.RelativePath}' is outside source bounds.", nameof(states));
            }
        }

        Id = id;
        Name = name;
        Resolution = resolution;
        Sources = normalizedSources;
        States = normalizedStates;
        SlicingRecipe = slicingRecipe;
    }

    public Guid Id { get; }

    public string Name { get; }

    public SpriteResolution Resolution { get; }

    public IReadOnlyList<SpriteSourceReference> Sources { get; }

    public IReadOnlyList<SpriteDocumentState> States { get; }

    public SpriteSheetSlicingRecipe? SlicingRecipe { get; }
}
