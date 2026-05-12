namespace AdaptiveSpritesDmiTool.Application;

public static class BatchPathLayout
{
    public static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static IReadOnlyList<string> ResolveInputFiles(
        string inputDirectory,
        string outputDirectory,
        IReadOnlyList<string>? explicitFiles)
    {
        var inputRoot = Path.GetFullPath(inputDirectory);
        var outputRoot = Path.GetFullPath(outputDirectory);
        var excludeOutputDirectory = IsStrictSubdirectory(inputRoot, outputRoot);

        var files = explicitFiles is { Count: > 0 }
            ? explicitFiles
                .Where(static file => !string.IsNullOrWhiteSpace(file))
                .Select(static file => Path.GetFullPath(file))
            : Directory
                .EnumerateFiles(inputRoot, "*.dmi", SearchOption.AllDirectories)
                .Select(static file => Path.GetFullPath(file));

        return files
            .Where(file => !excludeOutputDirectory || !IsPathUnderDirectory(file, outputRoot))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();
    }

    public static string BuildOutputPath(string inputDirectory, string outputDirectory, string inputPath)
    {
        var inputRoot = Path.GetFullPath(inputDirectory);
        var fullInputPath = Path.GetFullPath(inputPath);
        var relativePath = Path.GetRelativePath(inputRoot, fullInputPath);

        return IsRelativePathInsideRoot(relativePath)
            ? Path.Combine(outputDirectory, relativePath)
            : Path.Combine(outputDirectory, Path.GetFileName(fullInputPath));
    }

    public static bool IsPathUnderDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = EnsureTrailingSeparator(Path.GetFullPath(directory));
        return fullPath.StartsWith(fullDirectory, PathComparison);
    }

    public static bool IsRelativePathInsideRoot(string relativePath) =>
        !string.IsNullOrWhiteSpace(relativePath) &&
        !Path.IsPathRooted(relativePath) &&
        !relativePath.Equals(".", StringComparison.Ordinal) &&
        !relativePath.StartsWith("..", StringComparison.Ordinal) &&
        !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
        !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

    private static bool IsStrictSubdirectory(string parentDirectory, string candidateDirectory) =>
        !string.Equals(parentDirectory, candidateDirectory, PathComparison) &&
        IsPathUnderDirectory(candidateDirectory, parentDirectory);

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
