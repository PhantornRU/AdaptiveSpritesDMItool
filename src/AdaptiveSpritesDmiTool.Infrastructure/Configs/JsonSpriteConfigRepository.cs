using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using Newtonsoft.Json;

namespace AdaptiveSpritesDmiTool.Infrastructure.Configs;

public sealed class JsonSpriteConfigRepository : IConfigRepository
{
    public async Task<Result<SpriteConfig>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure<SpriteConfig>(Errors.Validation("Config path is required."));
        }

        if (!File.Exists(path))
        {
            return Result.Failure<SpriteConfig>(Errors.NotFound($"Config file '{path}' was not found."));
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            var model = JsonConvert.DeserializeObject<ConfigDocument>(json);
            if (model is null)
            {
                return Result.Failure<SpriteConfig>(Errors.Validation("Config file is empty or malformed."));
            }

            if (model.Version != 1)
            {
                return Result.Failure<SpriteConfig>(Errors.Validation($"Unsupported config version '{model.Version}'."));
            }

            var config = ToDomain(model);
            var validation = config.Validate();
            return validation.IsValid
                ? Result.Success(config)
                : Result.Failure<SpriteConfig>(Errors.Validation(validation.Errors[0].Message));
        }
        catch (JsonException exception)
        {
            return Result.Failure<SpriteConfig>(Errors.Validation($"Config JSON is invalid: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<SpriteConfig>(Errors.Unexpected($"Failed to load config: {exception.Message}"));
        }
    }

    public async Task<Result> SaveAsync(string path, SpriteConfig config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure(Errors.Validation("Config path is required."));
        }

        ArgumentNullException.ThrowIfNull(config);

        var validation = config.Validate();
        if (!validation.IsValid)
        {
            return Result.Failure(Errors.Validation(validation.Errors[0].Message));
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(FromDomain(config), Formatting.Indented);
            await File.WriteAllTextAsync(path, json, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure(Errors.Unexpected($"Failed to save config: {exception.Message}"));
        }
    }

    private static ConfigDocument FromDomain(SpriteConfig config) =>
        new()
        {
            Version = 1,
            Name = config.Name,
            Resolution = new ResolutionDocument
            {
                Width = config.Resolution.Width,
                Height = config.Resolution.Height
            },
            SupportedDirections = config.SupportedDirections.ToString(),
            Metadata = new MetadataDocument
            {
                CreatedUtc = config.Metadata.CreatedUtc,
                UpdatedUtc = config.Metadata.UpdatedUtc,
                Source = config.Metadata.Source.ToString(),
                SourceIdentifier = config.Metadata.SourceIdentifier,
                ImportedFromLegacy = config.Metadata.ImportedFromLegacy
            },
            Mappings = config.Directions.ToDictionary(
                static direction => direction.ToString(),
                direction => config.GetMappings(direction).Select(
                    static mapping => new MappingDocument
                    {
                        Source = new CoordinateDocument { X = mapping.Source.X, Y = mapping.Source.Y },
                        Target = mapping.Target is null
                            ? null
                            : new CoordinateDocument { X = mapping.Target.Value.X, Y = mapping.Target.Value.Y }
                    }).ToArray())
        };

    private static SpriteConfig ToDomain(ConfigDocument model)
    {
        var supportedDirections = ParseDirections(model.SupportedDirections);
        var metadata = new ConfigMetadata(
            model.Metadata.CreatedUtc,
            model.Metadata.UpdatedUtc,
            Enum.Parse<ConfigSource>(model.Metadata.Source, true),
            model.Metadata.SourceIdentifier,
            model.Metadata.ImportedFromLegacy);

        var config = SpriteConfig.CreateEmpty(
            model.Name,
            new SpriteResolution(model.Resolution.Width, model.Resolution.Height),
            supportedDirections,
            metadata);

            foreach (var pair in model.Mappings)
            {
                var direction = Enum.Parse<SpriteDirection>(pair.Key, true);
                foreach (var mapping in pair.Value)
                {
                    var source = new PixelCoordinate(mapping.Source.X, mapping.Source.Y);
                    PixelCoordinate? target = mapping.Target is null
                        ? null
                        : new PixelCoordinate(mapping.Target.X, mapping.Target.Y);
                    config = config.SetMapping(direction, source, target, metadata.UpdatedUtc);
                }
            }

        return config.WithMetadata(metadata);
    }

    private static SupportedDirectionSet ParseDirections(string value) =>
        value.ToLowerInvariant() switch
        {
            "four" => SupportedDirectionSet.Four,
            "eight" => SupportedDirectionSet.Eight,
            _ => throw new JsonException($"Unsupported direction set '{value}'.")
        };

    private sealed class ConfigDocument
    {
        public int Version { get; set; }

        public string Name { get; set; } = string.Empty;

        public ResolutionDocument Resolution { get; set; } = new();

        public string SupportedDirections { get; set; } = "four";

        public MetadataDocument Metadata { get; set; } = new();

        public Dictionary<string, MappingDocument[]> Mappings { get; set; } = [];
    }

    private sealed class ResolutionDocument
    {
        public int Width { get; set; }

        public int Height { get; set; }
    }

    private sealed class MetadataDocument
    {
        public DateTimeOffset CreatedUtc { get; set; }

        public DateTimeOffset UpdatedUtc { get; set; }

        public string Source { get; set; } = ConfigSource.UserCreated.ToString();

        public string? SourceIdentifier { get; set; }

        public string? ImportedFromLegacy { get; set; }
    }

    private sealed class MappingDocument
    {
        public CoordinateDocument Source { get; set; } = new();

        public CoordinateDocument? Target { get; set; }
    }

    private sealed class CoordinateDocument
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}