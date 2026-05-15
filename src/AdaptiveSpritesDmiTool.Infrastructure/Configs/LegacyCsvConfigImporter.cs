using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;

namespace AdaptiveSpritesDmiTool.Infrastructure.Configs;

public sealed class LegacyCsvConfigImporter : ILegacyCsvConfigImporter
{
    public async Task<Result<SpriteConfig>> ImportAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure<SpriteConfig>(Errors.Validation("CSV config path is required."));
        }

        if (!File.Exists(path))
        {
            return Result.Failure<SpriteConfig>(Errors.NotFound($"CSV config file '{path}' was not found."));
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            if (lines.Length == 0)
            {
                return Result.Failure<SpriteConfig>(Errors.Validation("CSV config is empty."));
            }

            var parsedMappings = new Dictionary<SpriteDirection, List<PixelMapping>>();
            SpriteResolution? inferredResolution = null;
            var directions = new HashSet<SpriteDirection>();

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = lines[lineIndex].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length != 5)
                {
                    return Result.Failure<SpriteConfig>(Errors.Validation($"CSV line {lineIndex + 1} must contain 5 columns."));
                }

                if (!Enum.TryParse(parts[0], true, out SpriteDirection direction))
                {
                    return Result.Failure<SpriteConfig>(Errors.Validation($"CSV line {lineIndex + 1} contains unsupported direction '{parts[0]}'."));
                }

                if (!int.TryParse(parts[1], out var sourceX) ||
                    !int.TryParse(parts[2], out var sourceY) ||
                    !int.TryParse(parts[3], out var targetX) ||
                    !int.TryParse(parts[4], out var targetY))
                {
                    return Result.Failure<SpriteConfig>(Errors.Validation($"CSV line {lineIndex + 1} contains non-numeric coordinates."));
                }

                var source = new PixelCoordinate(sourceX, sourceY);
                PixelCoordinate? target = targetX == -1 && targetY == -1
                    ? null
                    : new PixelCoordinate(targetX, targetY);

                directions.Add(direction);
                if (!parsedMappings.TryGetValue(direction, out var bucket))
                {
                    bucket = [];
                    parsedMappings[direction] = bucket;
                }

                bucket.Add(new PixelMapping(source, target));

                var maxX = Math.Max(sourceX, targetX);
                var maxY = Math.Max(sourceY, targetY);
                if (target is null)
                {
                    maxX = sourceX;
                    maxY = sourceY;
                }

                inferredResolution = inferredResolution is null
                    ? new SpriteResolution(maxX + 1, maxY + 1)
                    : new SpriteResolution(
                        Math.Max(inferredResolution.Value.Width, maxX + 1),
                        Math.Max(inferredResolution.Value.Height, maxY + 1));
            }

            var supportedDirections = SupportedDirectionSet.FromDirections(directions);
            var validationMappings = parsedMappings.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyCollection<PixelMapping>)pair.Value);

            var validation = SpriteConfig.ValidateCandidate(
                inferredResolution ?? new SpriteResolution(1, 1),
                supportedDirections,
                validationMappings);
            if (!validation.IsValid)
            {
                return Result.Failure<SpriteConfig>(Errors.Validation(validation.Errors[0].Message));
            }

            var metadata = ConfigMetadata.CreateNew(
                ConfigSource.ImportedLegacyCsv,
                sourceIdentifier: Path.GetFileName(path),
                importedFromLegacy: path);

            var config = SpriteConfig.CreateEmpty(
                Path.GetFileNameWithoutExtension(path),
                inferredResolution ?? new SpriteResolution(1, 1),
                supportedDirections,
                metadata);

            foreach (var pair in parsedMappings)
            {
                foreach (var mapping in pair.Value)
                {
                    config = config.SetMapping(pair.Key, mapping.Source, mapping.Target, metadata.UpdatedUtc);
                }
            }

            return Result.Success(config.WithMetadata(metadata));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpriteConfig>(Errors.Cancelled("CSV import was cancelled."));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Result.Failure<SpriteConfig>(Errors.Validation(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SpriteConfig>(Errors.Validation(exception.Message));
        }
        catch (Exception exception)
        {
            return Result.Failure<SpriteConfig>(Errors.Unexpected($"Failed to import CSV config: {exception.Message}"));
        }
    }
}