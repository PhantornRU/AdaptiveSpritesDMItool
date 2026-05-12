using System.Security.Cryptography;
using System.Text;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using Newtonsoft.Json;

namespace AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;

public sealed class BatchFingerprintService : IBatchFingerprintService
{
    public async Task<Result<string>> ComputeInputFingerprintAsync(string inputPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return Result.Failure<string>(Errors.Validation("Input path is required to compute batch fingerprint."));
        }

        var normalizedPath = Path.GetFullPath(inputPath);
        if (!File.Exists(normalizedPath))
        {
            return Result.Failure<string>(Errors.NotFound($"Batch input file '{normalizedPath}' was not found."));
        }

        try
        {
            await using var stream = File.OpenRead(normalizedPath);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return Result.Success(Convert.ToHexString(hash));
        }
        catch (Exception exception)
        {
            return Result.Failure<string>(Errors.Unexpected($"Failed to compute input fingerprint: {exception.Message}"));
        }
    }

    public string ComputeConfigFingerprint(SpriteConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var payload = new
        {
            resolution = new
            {
                width = config.Resolution.Width,
                height = config.Resolution.Height
            },
            supportedDirections = config.SupportedDirections.ToString(),
            mappings = config.Directions
                .OrderBy(static direction => direction)
                .Select(direction => new
                {
                    direction = direction.ToString(),
                    mappings = config.GetMappings(direction)
                        .OrderBy(static mapping => mapping.Source.X)
                        .ThenBy(static mapping => mapping.Source.Y)
                        .Select(mapping => new
                        {
                            source = new { x = mapping.Source.X, y = mapping.Source.Y },
                            target = mapping.Target is null
                                ? null
                                : new { x = mapping.Target.Value.X, y = mapping.Target.Value.Y }
                        })
                        .ToArray()
                })
                .ToArray()
        };

        var json = JsonConvert.SerializeObject(payload, Formatting.None);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
