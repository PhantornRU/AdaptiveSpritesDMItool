using System.Security.Cryptography;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Documents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace AdaptiveSpritesDmiTool.Infrastructure.Documents;

public sealed class AssetProbeService : IAssetProbeService
{
    public async Task<Result<AssetProbe>> ProbeAsync(
        string path,
        AssetImportLimits limits,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure<AssetProbe>(Errors.Validation("Asset path is required."));
        }

        ArgumentNullException.ThrowIfNull(limits);
        var normalizedPath = Path.GetFullPath(path);
        if (!File.Exists(normalizedPath))
        {
            return Result.Failure<AssetProbe>(Errors.NotFound($"Asset file '{normalizedPath}' was not found."));
        }

        var encodedLength = new FileInfo(normalizedPath).Length;
        if (encodedLength <= 0)
        {
            return Result.Failure<AssetProbe>(Errors.Validation("Asset file is empty."));
        }

        if (encodedLength > limits.MaximumEncodedBytes)
        {
            return Result.Failure<AssetProbe>(Errors.Validation($"Asset file exceeds the {limits.MaximumEncodedBytes} byte encoded-size limit."));
        }

        try
        {
            var decoderOptions = new DecoderOptions
            {
                MaxFrames = checked((uint)limits.MaximumFramesOrCells),
                SkipMetadata = false
            };
            var detected = await Image.DetectFormatAsync(decoderOptions, normalizedPath, cancellationToken).ConfigureAwait(false);
            if (detected is null || !detected.Name.Equals("PNG", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<AssetProbe>(Errors.Validation("V2.4 supports only PNG and DMI content."));
            }

            var imageInfo = await Image.IdentifyAsync(decoderOptions, normalizedPath, cancellationToken).ConfigureAwait(false);
            if (imageInfo is null)
            {
                return Result.Failure<AssetProbe>(Errors.Validation("Image metadata could not be identified."));
            }

            var frameCount = Math.Max(1, imageInfo.FrameMetadataCollection.Count);
            var limitsError = ValidateDecodedLimits(imageInfo.Width, imageInfo.Height, frameCount, limits);
            if (limitsError is not null)
            {
                return Result.Failure<AssetProbe>(Errors.Validation(limitsError));
            }

            var hasDmiMetadata = await PngDmiMetadataDetector
                .HasDmiMetadataAsync(normalizedPath, cancellationToken)
                .ConfigureAwait(false);
            var format = hasDmiMetadata ? SpriteSourceFormat.Dmi : SpriteSourceFormat.Png;
            var extensionMatches = ExtensionMatches(normalizedPath, format);

            await using var source = new FileStream(
                normalizedPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(source, cancellationToken).ConfigureAwait(false);

            return Result.Success(new AssetProbe(
                normalizedPath,
                format,
                hasDmiMetadata,
                extensionMatches,
                imageInfo.Width,
                imageInfo.Height,
                frameCount,
                encodedLength,
                Convert.ToHexStringLower(hash)));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<AssetProbe>(Errors.Cancelled("Asset probing was cancelled."));
        }
        catch (UnknownImageFormatException exception)
        {
            return Result.Failure<AssetProbe>(Errors.Validation($"Unsupported or corrupt image: {exception.Message}"));
        }
        catch (InvalidImageContentException exception)
        {
            return Result.Failure<AssetProbe>(Errors.Validation($"Corrupt image content: {exception.Message}"));
        }
        catch (InvalidDataException exception)
        {
            return Result.Failure<AssetProbe>(Errors.Validation($"Invalid PNG metadata: {exception.Message}"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<AssetProbe>(Errors.Unexpected($"Failed to probe asset: {exception.Message}"));
        }
    }

    private static string? ValidateDecodedLimits(
        int width,
        int height,
        int frameCount,
        AssetImportLimits limits)
    {
        if (width <= 0 || height <= 0)
        {
            return "Image dimensions must be positive.";
        }

        if (width > limits.MaximumDimension || height > limits.MaximumDimension)
        {
            return $"Image dimensions exceed the {limits.MaximumDimension} pixel limit.";
        }

        if (frameCount <= 0 || frameCount > limits.MaximumFramesOrCells)
        {
            return $"Image frame count exceeds the {limits.MaximumFramesOrCells} frame limit.";
        }

        try
        {
            var decodedPixels = checked((long)width * height * frameCount);
            return decodedPixels > limits.MaximumDecodedPixels
                ? $"Decoded image exceeds the {limits.MaximumDecodedPixels} pixel limit."
                : null;
        }
        catch (OverflowException)
        {
            return "Decoded image dimensions overflow the supported pixel budget.";
        }
    }

    private static bool ExtensionMatches(string path, SpriteSourceFormat format)
    {
        var extension = Path.GetExtension(path);
        return format switch
        {
            SpriteSourceFormat.Dmi => extension.Equals(".dmi", StringComparison.OrdinalIgnoreCase),
            SpriteSourceFormat.Png => extension.Equals(".png", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
