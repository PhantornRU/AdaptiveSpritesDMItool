using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;

namespace AdaptiveSpritesDmiTool.Application;

public sealed record AssetImportLimits(
    long MaximumEncodedBytes,
    int MaximumDimension,
    long MaximumDecodedPixels,
    int MaximumFramesOrCells,
    int MaximumStates)
{
    public static AssetImportLimits Default { get; } = new(
        MaximumEncodedBytes: 128L * 1024 * 1024,
        MaximumDimension: 16_384,
        MaximumDecodedPixels: 67_108_864,
        MaximumFramesOrCells: 4_096,
        MaximumStates: 1_024);
}

public sealed record AssetProbe(
    string SourcePath,
    SpriteSourceFormat DetectedFormat,
    bool HasDmiMetadata,
    bool ExtensionMatchesContent,
    int Width,
    int Height,
    int EncodedFrameCount,
    long EncodedLength,
    string Sha256);

public enum SpriteDocumentImportKind
{
    NativeDmi = 0,
    SingleRaster = 1,
    RasterSequence = 2,
    SpriteSheet = 3
}

public sealed record SpriteDocumentImportRequest(
    IReadOnlyList<string> SourcePaths,
    string DocumentName,
    SpriteDocumentImportKind Kind,
    SpriteResolution? FrameResolution = null,
    string? StateName = null,
    SpriteDirectionDepth DirectionDepth = SpriteDirectionDepth.One,
    IReadOnlyList<SpriteDirection>? DirectionOrder = null,
    SpriteSheetSlicingRecipe? SlicingRecipe = null,
    AssetImportLimits? Limits = null,
    string? ProjectPath = null);

public enum SourceChangeResolution
{
    Reject = 0,
    AcceptNewFingerprint = 1
}

public sealed record SpriteDocumentLoadRequest(
    string ProjectPath,
    SourceChangeResolution SourceChangeResolution = SourceChangeResolution.Reject,
    IReadOnlyDictionary<Guid, string>? RelinkedSources = null,
    AssetImportLimits? Limits = null);

public sealed record SpriteFrameReadRequest(
    SpriteDocument Document,
    string StateName,
    SpriteDirection Direction,
    int FrameIndex);

public sealed record AuxiliaryLayerFrameRequest(
    string SourcePath,
    SpriteSourceFormat Format,
    string StateName,
    SpriteDirection Direction,
    int FrameIndex,
    SpriteResolution RequiredResolution);

public enum SpriteDocumentExportFormat
{
    Dmi = 0,
    PngSheet = 1,
    PngSequence = 2
}

public sealed record SpriteDocumentExportRequest(
    SpriteDocument Document,
    string OutputPath,
    SpriteDocumentExportFormat Format,
    OverwritePolicy OverwritePolicy);

public sealed record SpriteDocumentExportResult(
    string OutputPath,
    IReadOnlyList<string> WrittenFiles,
    string? SidecarPath);

public interface IAssetProbeService
{
    Task<Result<AssetProbe>> ProbeAsync(
        string path,
        AssetImportLimits limits,
        CancellationToken cancellationToken);
}

public interface ISpriteDocumentImporter
{
    Task<Result<SpriteDocument>> ImportAsync(
        SpriteDocumentImportRequest request,
        CancellationToken cancellationToken);
}

public interface ISpriteDocumentRepository
{
    Task<Result<SpriteDocument>> LoadAsync(
        SpriteDocumentLoadRequest request,
        CancellationToken cancellationToken);

    Task<Result> SaveAsync(
        string projectPath,
        SpriteDocument document,
        CancellationToken cancellationToken);
}

public interface ISpriteFrameSource
{
    Task<Result<SpriteImage>> ReadAsync(
        SpriteFrameReadRequest request,
        CancellationToken cancellationToken);
}

public interface IAuxiliaryLayerFrameReader
{
    Task<Result<SpriteImage>> ReadAsync(
        AuxiliaryLayerFrameRequest request,
        CancellationToken cancellationToken);
}

public interface ISpriteDocumentExporter
{
    Task<Result<SpriteDocumentExportResult>> ExportAsync(
        SpriteDocumentExportRequest request,
        CancellationToken cancellationToken);
}
