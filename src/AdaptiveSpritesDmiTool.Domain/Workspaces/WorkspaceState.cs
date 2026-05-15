using AdaptiveSpritesDmiTool.Domain.Configurations;

namespace AdaptiveSpritesDmiTool.Domain.Workspaces;

public sealed record WorkspaceState(
    SpriteResolution? Resolution,
    SupportedDirectionSet? SupportedDirections,
    string? DisplayName,
    bool HasSpriteLoaded,
    string? LoadedDocumentPath)
{
    public static WorkspaceState Empty { get; } = new(null, null, null, false, null);

    public bool IsEmpty => !HasSpriteLoaded;

    public static WorkspaceState ForSprite(string displayName, SpriteResolution resolution, SupportedDirectionSet supportedDirections) =>
        new(resolution, supportedDirections, displayName, true, null);

    public static WorkspaceState Loaded(
        string displayName,
        SpriteResolution resolution,
        SupportedDirectionSet supportedDirections,
        string loadedDocumentPath) =>
        new(resolution, supportedDirections, displayName, true, loadedDocumentPath);
}