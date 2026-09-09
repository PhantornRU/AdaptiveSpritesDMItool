using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;

namespace AdaptiveSpritesDmiTool.Infrastructure.Documents;

public sealed class ContentAwareAuxiliaryLayerFrameReader(
    IAssetProbeService probeService,
    ISpriteDocumentImporter importer,
    ISpriteFrameSource frameSource) : IAuxiliaryLayerFrameReader
{
    public async Task<Result<SpriteImage>> ReadAsync(
        AuxiliaryLayerFrameRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var probeResult = await probeService
            .ProbeAsync(request.SourcePath, AssetImportLimits.Default, cancellationToken)
            .ConfigureAwait(false);
        if (probeResult.IsFailure)
        {
            return Result.Failure<SpriteImage>(probeResult.Error);
        }

        var probe = probeResult.Value;
        if (probe.DetectedFormat != request.Format)
        {
            return Result.Failure<SpriteImage>(Errors.Conflict(
                $"Auxiliary source format changed from '{request.Format}' to '{probe.DetectedFormat}'."));
        }

        var importRequest = request.Format switch
        {
            SpriteSourceFormat.Dmi => new SpriteDocumentImportRequest(
                [request.SourcePath],
                Path.GetFileNameWithoutExtension(request.SourcePath),
                SpriteDocumentImportKind.NativeDmi),
            SpriteSourceFormat.Png => new SpriteDocumentImportRequest(
                [request.SourcePath],
                Path.GetFileNameWithoutExtension(request.SourcePath),
                SpriteDocumentImportKind.SingleRaster),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Format, "Unsupported auxiliary source format.")
        };
        var documentResult = await importer.ImportAsync(importRequest, cancellationToken).ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure<SpriteImage>(documentResult.Error);
        }

        if (documentResult.Value.Resolution != request.RequiredResolution)
        {
            return Result.Failure<SpriteImage>(Errors.Validation(
                $"Auxiliary source resolution {documentResult.Value.Resolution} does not match current {request.RequiredResolution}."));
        }

        var state = request.Format == SpriteSourceFormat.Png
            ? documentResult.Value.States[0]
            : documentResult.Value.States.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, request.StateName, StringComparison.OrdinalIgnoreCase));
        if (state is null)
        {
            return Result.Failure<SpriteImage>(Errors.NotFound(
                $"Auxiliary state '{request.StateName}' was not found."));
        }

        if (request.FrameIndex < 0 || request.FrameIndex >= state.FramesPerDirection)
        {
            return Result.Failure<SpriteImage>(Errors.Validation(
                $"Auxiliary frame index {request.FrameIndex} is outside state '{state.Name}'."));
        }

        var direction = state.DirectionDepth.GetDirections().Contains(request.Direction)
            ? request.Direction
            : SpriteDirection.South;
        return await frameSource.ReadAsync(
            new SpriteFrameReadRequest(documentResult.Value, state.Name, direction, request.FrameIndex),
            cancellationToken).ConfigureAwait(false);
    }
}
