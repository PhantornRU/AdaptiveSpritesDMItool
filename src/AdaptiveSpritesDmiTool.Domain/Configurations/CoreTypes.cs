namespace AdaptiveSpritesDmiTool.Domain.Configurations;

public enum SpriteDirection
{
    South = 0,
    North = 1,
    East = 2,
    West = 3,
    SouthEast = 4,
    SouthWest = 5,
    NorthEast = 6,
    NorthWest = 7
}

public enum DirectionSet
{
    Four = 4,
    Eight = 8
}

public sealed class SupportedDirectionSet : IEquatable<SupportedDirectionSet>
{
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

    private SupportedDirectionSet(DirectionSet directions, IReadOnlyList<SpriteDirection> supportedDirections)
    {
        Directions = directions;
        SupportedDirections = supportedDirections;
    }

    public DirectionSet Directions { get; }

    public IReadOnlyList<SpriteDirection> SupportedDirections { get; }

    public static SupportedDirectionSet Four { get; } = new(DirectionSet.Four, FourDirections);

    public static SupportedDirectionSet Eight { get; } = new(DirectionSet.Eight, EightDirections);

    public static SupportedDirectionSet From(DirectionSet directions) =>
        directions switch
        {
            DirectionSet.Four => Four,
            DirectionSet.Eight => Eight,
            _ => throw new ArgumentOutOfRangeException(nameof(directions), directions, "Unsupported direction set.")
        };

    public static SupportedDirectionSet FromDirections(IEnumerable<SpriteDirection> directions)
    {
        ArgumentNullException.ThrowIfNull(directions);

        var normalized = directions.Distinct().OrderBy(static direction => (int)direction).ToArray();
        var four = FourDirections.OrderBy(static direction => (int)direction).ToArray();
        var eight = EightDirections.OrderBy(static direction => (int)direction).ToArray();

        if (normalized.SequenceEqual(four))
        {
            return Four;
        }

        if (normalized.SequenceEqual(eight))
        {
            return Eight;
        }

        throw new ArgumentException("Supported directions must match either the standard four-direction or eight-direction set.", nameof(directions));
    }

    public bool Supports(SpriteDirection direction) => SupportedDirections.Contains(direction);

    public IEnumerable<SpriteDirection> GetDirections() => SupportedDirections;

    public bool Equals(SupportedDirectionSet? other) => other is not null && Directions == other.Directions;

    public override bool Equals(object? obj) => obj is SupportedDirectionSet other && Equals(other);

    public override int GetHashCode() => (int)Directions;

    public override string ToString() => Directions switch
    {
        DirectionSet.Four => "four",
        DirectionSet.Eight => "eight",
        _ => Directions.ToString()
    };
}

public readonly record struct PixelCoordinate
{
    public PixelCoordinate(int x, int y)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Pixel coordinate X must be non-negative.");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Pixel coordinate Y must be non-negative.");
        }

        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public override string ToString() => $"{X},{Y}";
}

public readonly record struct SpriteResolution
{
    public SpriteResolution(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public bool IsPositive => Width > 0 && Height > 0;

    public bool Contains(PixelCoordinate coordinate) => coordinate.X < Width && coordinate.Y < Height;

    public override string ToString() => $"{Width}x{Height}";
}

public enum ConfigSource
{
    UserCreated = 0,
    Json = 1,
    ImportedLegacyCsv = 2
}

public sealed record ConfigMetadata(
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    ConfigSource Source,
    string? SourceIdentifier,
    string? ImportedFromLegacy)
{
    public static ConfigMetadata CreateNew(
        ConfigSource source,
        string? sourceIdentifier = null,
        string? importedFromLegacy = null,
        DateTimeOffset? utcNow = null)
    {
        var timestamp = utcNow ?? DateTimeOffset.UtcNow;
        return new ConfigMetadata(timestamp, timestamp, source, sourceIdentifier, importedFromLegacy);
    }

    public ConfigMetadata Touch(DateTimeOffset updatedUtc) => this with { UpdatedUtc = updatedUtc };
}

public sealed record ConfigValidationIssue(
    string Code,
    string Message,
    SpriteDirection? Direction = null,
    PixelCoordinate? Coordinate = null);

public sealed class ConfigValidationResult
{
    private ConfigValidationResult(IReadOnlyList<ConfigValidationIssue> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<ConfigValidationIssue> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static ConfigValidationResult Success() => new([]);

    public static ConfigValidationResult Failure(IEnumerable<ConfigValidationIssue> errors) => new(errors.ToArray());
}

public readonly record struct PixelMapping(PixelCoordinate Source, PixelCoordinate? Target)
{
    public bool IsTransparent => Target is null;
}