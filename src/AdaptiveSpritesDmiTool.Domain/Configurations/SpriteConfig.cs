namespace AdaptiveSpritesDmiTool.Domain.Configurations;

public sealed class SpriteConfig
{
    private readonly Dictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelMapping>> _mappings;

    private SpriteConfig(
        string name,
        SpriteResolution resolution,
        SupportedDirectionSet supportedDirections,
        ConfigMetadata metadata,
        Dictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelMapping>> mappings)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Config name is required.", nameof(name));
        }

        Name = name.Trim();
        Resolution = resolution;
        SupportedDirections = supportedDirections ?? throw new ArgumentNullException(nameof(supportedDirections));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _mappings = mappings;
    }

    public string Name { get; }

    public SpriteResolution Resolution { get; }

    public SupportedDirectionSet SupportedDirections { get; }

    public ConfigMetadata Metadata { get; }

    public IReadOnlyCollection<SpriteDirection> Directions => SupportedDirections.SupportedDirections;

    public static SpriteConfig CreateEmpty(
        string name,
        SpriteResolution resolution,
        SupportedDirectionSet supportedDirections,
        ConfigMetadata metadata) =>
        new(name, resolution, supportedDirections, metadata, CreateDirectionBuckets(supportedDirections));

    public ConfigValidationResult Validate()
    {
        var issues = new List<ConfigValidationIssue>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            issues.Add(new ConfigValidationIssue("config.name.empty", "Config name must not be empty."));
        }

        if (!Resolution.IsPositive)
        {
            issues.Add(new ConfigValidationIssue("config.resolution.invalid", "Resolution must be positive."));
        }

        if (Metadata.UpdatedUtc < Metadata.CreatedUtc)
        {
            issues.Add(new ConfigValidationIssue("config.metadata.invalid", "Updated time must not be earlier than created time."));
        }

        foreach (var (direction, mappings) in _mappings)
        {
            if (!SupportedDirections.Supports(direction))
            {
                issues.Add(new ConfigValidationIssue(
                    "config.direction.unsupported",
                    $"Direction '{direction}' is not allowed for direction set '{SupportedDirections}'.",
                    direction));
            }

            foreach (var mapping in mappings.Values)
            {
                if (!Resolution.Contains(mapping.Source))
                {
                    issues.Add(new ConfigValidationIssue(
                        "config.mapping.source.out_of_bounds",
                        $"Source coordinate '{mapping.Source}' is out of bounds for resolution '{Resolution}'.",
                        direction,
                        mapping.Source));
                }

                if (mapping.Target is { } target && !Resolution.Contains(target))
                {
                    issues.Add(new ConfigValidationIssue(
                        "config.mapping.target.out_of_bounds",
                        $"Target coordinate '{target}' is out of bounds for resolution '{Resolution}'.",
                        direction,
                        target));
                }
            }
        }

        return issues.Count == 0 ? ConfigValidationResult.Success() : ConfigValidationResult.Failure(issues);
    }

    public static ConfigValidationResult ValidateCandidate(
        SpriteResolution resolution,
        SupportedDirectionSet supportedDirections,
        IReadOnlyDictionary<SpriteDirection, IReadOnlyCollection<PixelMapping>> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);

        var issues = new List<ConfigValidationIssue>();

        foreach (var direction in supportedDirections.GetDirections())
        {
            if (!mappings.TryGetValue(direction, out var directionMappings))
            {
                continue;
            }

            foreach (var mapping in directionMappings)
            {
                if (!resolution.Contains(mapping.Source))
                {
                    issues.Add(new ConfigValidationIssue(
                        "source-out-of-bounds",
                        $"Source coordinate '{mapping.Source}' is out of bounds for resolution '{resolution}'.",
                        direction,
                        mapping.Source));
                }

                if (mapping.Target is { } target && !resolution.Contains(target))
                {
                    issues.Add(new ConfigValidationIssue(
                        "target-out-of-bounds",
                        $"Target coordinate '{target}' is out of bounds for resolution '{resolution}'.",
                        direction,
                        target));
                }
            }
        }

        foreach (var direction in mappings.Keys.Except(supportedDirections.GetDirections()))
        {
            issues.Add(new ConfigValidationIssue(
                "unsupported-direction",
                $"Direction '{direction}' is not allowed for direction set '{supportedDirections}'.",
                direction));
        }

        return issues.Count == 0 ? ConfigValidationResult.Success() : ConfigValidationResult.Failure(issues);
    }

    public ConfigValidationResult ValidateCompatibility(SpriteResolution resolution, SupportedDirectionSet supportedDirections)
    {
        var issues = new List<ConfigValidationIssue>();

        if (Resolution != resolution)
        {
            issues.Add(new ConfigValidationIssue(
                "config.compatibility.resolution",
                $"Config resolution '{Resolution}' does not match sprite resolution '{resolution}'."));
        }

        if (!Equals(SupportedDirections, supportedDirections))
        {
            issues.Add(new ConfigValidationIssue(
                "config.compatibility.direction_set",
                $"Config direction set '{SupportedDirections}' does not match sprite direction set '{supportedDirections}'."));
        }

        return issues.Count == 0 ? ConfigValidationResult.Success() : ConfigValidationResult.Failure(issues);
    }

    public IReadOnlyCollection<PixelMapping> GetMappings(SpriteDirection direction)
    {
        EnsureDirection(direction);
        return _mappings[direction].Values.ToArray();
    }

    public PixelCoordinate GetEffectiveTarget(SpriteDirection direction, PixelCoordinate source)
    {
        EnsureDirection(direction);
        EnsureCoordinate(source, nameof(source));

        return _mappings[direction].TryGetValue(source, out var mapping) && mapping.Target is { } target
            ? target
            : source;
    }

    public bool IsTransparent(SpriteDirection direction, PixelCoordinate source)
    {
        EnsureDirection(direction);
        EnsureCoordinate(source, nameof(source));

        return _mappings[direction].TryGetValue(source, out var mapping) && mapping.IsTransparent;
    }

    /// <summary>
    /// Sets a mapping for a specific direction.
    /// Includes identity/no-op optimizations: if the target equals the source, the explicit mapping is removed
    /// to rely on the default transparent/identity behavior. Use this for regular user edits.
    /// </summary>
    public SpriteConfig SetMapping(SpriteDirection direction, PixelCoordinate source, PixelCoordinate? target) =>
        SetMapping(direction, source, target, DateTimeOffset.UtcNow);

    /// <summary>
    /// Sets a mapping for a specific direction.
    /// Includes identity/no-op optimizations: if the target equals the source, the explicit mapping is removed.
    /// </summary>
    public SpriteConfig SetMapping(SpriteDirection direction, PixelCoordinate source, PixelCoordinate? target, DateTimeOffset updatedUtc)
    {
        EnsureDirection(direction);
        EnsureCoordinate(source, nameof(source));

        if (target is { } concreteTarget)
        {
            EnsureCoordinate(concreteTarget, nameof(target));
        }

        var next = CloneMappings(_mappings);
        var bucket = next[direction];

        if (target == source)
        {
            bucket.Remove(source);
        }
        else
        {
            bucket[source] = new PixelMapping(source, target);
        }

        return new SpriteConfig(Name, Resolution, SupportedDirections, Metadata.Touch(updatedUtc), next);
    }

    /// <summary>
    /// Forces a mapping regardless of whether <paramref name="target"/> equals <paramref name="source"/>.
    /// Unlike <see cref="SetMapping"/>, this method overwrites existing mappings without identity checks.
    /// It is intended for use in Undo/Redo mechanisms, Drag & Drop move operations, or mass deserialization.
    /// </summary>
    public SpriteConfig SetMappingForced(SpriteDirection direction, PixelCoordinate source, PixelCoordinate? target) =>
        SetMappingForced(direction, source, target, DateTimeOffset.UtcNow);

    /// <summary>
    /// Forces a mapping regardless of whether <paramref name="target"/> equals <paramref name="source"/>.
    /// Overwrites existing mappings without identity checks.
    /// Intended for Undo/Redo, Drag & Drop, or mass deserialization.
    /// </summary>
    public SpriteConfig SetMappingForced(SpriteDirection direction, PixelCoordinate source, PixelCoordinate? target, DateTimeOffset updatedUtc)
    {
        EnsureDirection(direction);
        EnsureCoordinate(source, nameof(source));

        if (target is { } concreteTarget)
        {
            EnsureCoordinate(concreteTarget, nameof(target));
        }

        var next = CloneMappings(_mappings);
        next[direction][source] = new PixelMapping(source, target);
        return new SpriteConfig(Name, Resolution, SupportedDirections, Metadata.Touch(updatedUtc), next);
    }

    public SpriteConfig RemoveMapping(SpriteDirection direction, PixelCoordinate source)
    {
        EnsureDirection(direction);
        EnsureCoordinate(source, nameof(source));

        var next = CloneMappings(_mappings);
        next[direction].Remove(source);
        return new SpriteConfig(Name, Resolution, SupportedDirections, Metadata.Touch(DateTimeOffset.UtcNow), next);
    }

    public SpriteConfig WithMetadata(ConfigMetadata metadata) =>
        new(Name, Resolution, SupportedDirections, metadata, CloneMappings(_mappings));

    public SpriteConfig WithName(string name) =>
        new(name, Resolution, SupportedDirections, Metadata.Touch(DateTimeOffset.UtcNow), CloneMappings(_mappings));

    public SpriteConfig Clone() =>
        new(Name, Resolution, SupportedDirections, Metadata, CloneMappings(_mappings));

    private static Dictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelMapping>> CreateDirectionBuckets(SupportedDirectionSet supportedDirections)
    {
        var buckets = new Dictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelMapping>>();

        foreach (var direction in supportedDirections.GetDirections())
        {
            buckets[direction] = [];
        }

        return buckets;
    }

    private static Dictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelMapping>> CloneMappings(
        IReadOnlyDictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelMapping>> source) =>
        source.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToDictionary(static entry => entry.Key, static entry => entry.Value));

    private void EnsureDirection(SpriteDirection direction)
    {
        if (!SupportedDirections.Supports(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction is not supported by this config.");
        }
    }

    private void EnsureCoordinate(PixelCoordinate coordinate, string parameterName)
    {
        if (!Resolution.Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(parameterName, coordinate, "Coordinate is outside of the configured resolution.");
        }
    }
}