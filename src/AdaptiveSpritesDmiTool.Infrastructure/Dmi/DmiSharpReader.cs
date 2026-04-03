using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using DMISharp;

namespace AdaptiveSpritesDmiTool.Infrastructure.Dmi;

public sealed class DmiSharpReader : IDmiReader
{
    public async Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure<DmiAssetInfo>(Errors.Validation("DMI path is required."));
        }

        var normalizedPath = Path.GetFullPath(path);
        if (!File.Exists(normalizedPath))
        {
            return Result.Failure<DmiAssetInfo>(Errors.NotFound($"DMI file '{normalizedPath}' was not found."));
        }

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var dmiFile = new DMIFile(normalizedPath);
                if (dmiFile.States.Count == 0)
                {
                    return Result.Failure<DmiAssetInfo>(Errors.Validation("DMI file does not contain any states."));
                }

                var resolution = DmiSharpConversions.InferResolution(dmiFile);
                var supportedDirections = DmiSharpConversions.InferSupportedDirections(dmiFile.States);
                var states = dmiFile.States
                    .OrderBy(static state => state.Name, StringComparer.Ordinal)
                    .Select(static state => new DmiStateInfo(state.Name, state.TotalFrames))
                    .ToArray();

                return Result.Success(
                    new DmiAssetInfo(
                        Path.GetFileNameWithoutExtension(normalizedPath),
                        normalizedPath,
                        resolution,
                        supportedDirections,
                        states));
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<DmiAssetInfo>(Errors.Cancelled("DMI loading was cancelled."));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<DmiAssetInfo>(Errors.Validation($"Failed to read DMI file: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<DmiAssetInfo>(Errors.Unexpected($"Failed to load DMI file: {exception.Message}"));
        }
    }
}