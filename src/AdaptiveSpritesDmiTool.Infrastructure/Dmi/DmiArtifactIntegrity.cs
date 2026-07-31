using System.Security.Cryptography;
using System.Collections;
using System.Globalization;
using System.Reflection;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using DMISharp;

namespace AdaptiveSpritesDmiTool.Infrastructure.Dmi;

internal sealed record DmiFrameFingerprint(
    StateDirection Direction,
    int FrameIndex,
    string RgbaSha256);

internal sealed record DmiStateFingerprint(
    string Name,
    DirectionDepth DirectionDepth,
    int TotalFrames,
    int Width,
    int Height,
    string MetadataSignature,
    IReadOnlyList<DmiFrameFingerprint> Frames);

internal sealed record DmiArtifactFingerprint(
    string NormalizedMetadata,
    IReadOnlyList<DmiStateFingerprint> States);

internal interface IDmiArtifactValidator
{
    Result Validate(string path, DmiArtifactFingerprint expected, CancellationToken cancellationToken);
}

internal interface IDmiAtomicCommitter
{
    void Commit(string temporaryPath, string destinationPath);
}

internal sealed class DmiArtifactValidator : IDmiArtifactValidator
{
    public Result Validate(string path, DmiArtifactFingerprint expected, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(expected);

        cancellationToken.ThrowIfCancellationRequested();
        using var reopened = new DMIFile(path);
        var actual = DmiArtifactFingerprintFactory.Create(reopened, cancellationToken);

        if (!StringComparer.Ordinal.Equals(expected.NormalizedMetadata, actual.NormalizedMetadata))
        {
            return Result.Failure(Errors.Validation("Saved DMI metadata does not match the expected result."));
        }

        if (expected.States.Count != actual.States.Count)
        {
            return Result.Failure(Errors.Validation("Saved DMI state count does not match the expected result."));
        }

        for (var stateIndex = 0; stateIndex < expected.States.Count; stateIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expectedState = expected.States[stateIndex];
            var actualState = actual.States[stateIndex];
            if (expectedState.Name != actualState.Name ||
                expectedState.DirectionDepth != actualState.DirectionDepth ||
                expectedState.TotalFrames != actualState.TotalFrames ||
                expectedState.Width != actualState.Width ||
                expectedState.Height != actualState.Height ||
                expectedState.MetadataSignature != actualState.MetadataSignature)
            {
                return Result.Failure(
                    Errors.Validation($"Saved DMI state metadata differs at index {stateIndex}."));
            }

            if (!expectedState.Frames.SequenceEqual(actualState.Frames))
            {
                return Result.Failure(
                    Errors.Validation($"Saved DMI RGBA frame data differs for state '{expectedState.Name}'."));
            }
        }

        return Result.Success();
    }
}

internal sealed class DmiAtomicCommitter : IDmiAtomicCommitter
{
    public void Commit(string temporaryPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Replace(temporaryPath, destinationPath, null);
            return;
        }

        File.Move(temporaryPath, destinationPath);
    }
}

internal static class DmiArtifactFingerprintFactory
{
    public static DmiArtifactFingerprint Create(DMIFile dmiFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dmiFile);

        var states = new List<DmiStateFingerprint>(dmiFile.States.Count);
        foreach (var state in dmiFile.States)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directions = DmiSharpConversions.GetDirections(state.DirectionDepth);
            var frameCount = GetFrameCount(state, directions.Count);
            var frames = new List<DmiFrameFingerprint>(state.TotalFrames);
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                foreach (var direction in directions)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var frame = state.GetFrame(direction, frameIndex)
                        ?? throw new ArgumentException(
                            $"State '{state.Name}' is missing frame {frameIndex} for direction '{direction}'.");
                    frames.Add(new DmiFrameFingerprint(direction, frameIndex, ComputeRgbaSha256(frame)));
                }
            }

            states.Add(
                new DmiStateFingerprint(
                    state.Name,
                    state.DirectionDepth,
                    state.TotalFrames,
                    state.Width,
                    state.Height,
                    CreateObjectSignature(state.Data),
                    frames));
        }

        return new DmiArtifactFingerprint(CreateFileMetadataSignature(dmiFile), states);
    }

    private static int GetFrameCount(DMIState state, int directionCount)
    {
        if (state.TotalFrames <= 0 || state.TotalFrames % directionCount != 0)
        {
            throw new ArgumentException(
                $"State '{state.Name}' has an unsupported frame layout for direction depth '{state.DirectionDepth}'.");
        }

        return state.TotalFrames / directionCount;
    }

    private static string ComputeRgbaSha256(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image)
    {
        var rgba = new byte[checked(image.Width * image.Height * 4)];
        image.ProcessPixelRows(accessor =>
        {
            var destinationOffset = 0;
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    rgba[destinationOffset++] = pixel.R;
                    rgba[destinationOffset++] = pixel.G;
                    rgba[destinationOffset++] = pixel.B;
                    rgba[destinationOffset++] = pixel.A;
                }
            }
        });

        return Convert.ToHexString(SHA256.HashData(rgba));
    }

    private static string CreateFileMetadataSignature(DMIFile dmiFile) =>
        CreateObjectSignature(dmiFile.Metadata, "States");

    private static string CreateObjectSignature(object value, params string[] excludedProperties)
    {
        var excluded = excludedProperties.ToHashSet(StringComparer.Ordinal);
        return string.Join(
            "|",
            value.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && !excluded.Contains(property.Name))
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => $"{property.Name}={FormatMetadataValue(property.GetValue(value))}"));
    }

    private static string FormatMetadataValue(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        if (value is string text)
        {
            return text.Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        if (value is IEnumerable values)
        {
            return "[" + string.Join(",", values.Cast<object?>().Select(FormatMetadataValue)) + "]";
        }

        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
    }
}
