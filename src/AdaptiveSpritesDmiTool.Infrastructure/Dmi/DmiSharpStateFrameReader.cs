using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace AdaptiveSpritesDmiTool.Infrastructure.Dmi;

public sealed class DmiSharpStateFrameReader : IStateFrameReader
{
    public async Task<Result<SpriteImage>> ReadFrameAsync(string dmiPath, string stateName, SpriteDirection direction, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dmiPath))
        {
            return Result.Failure<SpriteImage>(Errors.Validation("DMI path is required."));
        }

        if (string.IsNullOrWhiteSpace(stateName))
        {
            return Result.Failure<SpriteImage>(Errors.Validation("State name is required."));
        }

        var normalizedPath = Path.GetFullPath(dmiPath);
        if (!File.Exists(normalizedPath))
        {
            return Result.Failure<SpriteImage>(Errors.NotFound($"DMI file '{normalizedPath}' was not found."));
        }

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var dmiFile = new DMIFile(normalizedPath);
                var state = dmiFile.States.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, stateName, StringComparison.OrdinalIgnoreCase));
                if (state is null)
                {
                    return Result.Failure<SpriteImage>(Errors.NotFound($"State '{stateName}' was not found in '{Path.GetFileName(normalizedPath)}'."));
                }

                using var frame = ReadFrame(state, direction);
                if (frame is null)
                {
                    return Result.Failure<SpriteImage>(Errors.Validation($"State '{stateName}' does not contain a readable '{direction}' frame."));
                }

                return Result.Success(ToSpriteImage(frame));
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpriteImage>(Errors.Cancelled("State frame loading was cancelled."));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SpriteImage>(Errors.Validation($"Failed to read state frame: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<SpriteImage>(Errors.Unexpected($"Failed to load state frame: {exception.Message}"));
        }
    }

    private static Image<Rgba32>? ReadFrame(DMIState state, SpriteDirection requestedDirection)
    {
        var requested = DmiSharpConversions.ToDmiDirection(requestedDirection);
        var availableDirections = DmiSharpConversions.GetDirections(state.DirectionDepth);
        var resolvedDirection = availableDirections.Contains(requested)
            ? requested
            : availableDirections[0];

        return state.GetFrame(resolvedDirection, 0)?.Clone();
    }

    private static SpriteImage ToSpriteImage(Image<Rgba32> image)
    {
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        var bytes = MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
        return new SpriteImage(image.Width, image.Height, bytes);
    }
}
