using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using AdaptiveSpritesDmiTool.Infrastructure.Preview;
using DMISharp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Dmi;

public sealed class DmiSharpConfigWriterIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.DmiWriterTests", Guid.NewGuid().ToString("N"));

    public DmiSharpConfigWriterIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ApplyAsyncShouldTransformFourDirectionDmiAndOverwriteExistingOutput()
    {
        var inputPath = Path.Combine(_tempDirectory, "input.dmi");
        var outputPath = Path.Combine(_tempDirectory, "output.dmi");

        TestDmiFactory.CreateDmi(
            inputPath,
            TestDmiFactory.CreateState("base", DirectionDepth.Four, 2, 1, static direction => direction switch
            {
                StateDirection.South => TestDmiFactory.CreateImage(new Rgba32(255, 0, 0, 255), new Rgba32(0, 255, 0, 255)),
                StateDirection.North => TestDmiFactory.CreateImage(new Rgba32(0, 0, 255, 255), new Rgba32(255, 255, 0, 255)),
                StateDirection.East => TestDmiFactory.CreateImage(new Rgba32(255, 0, 255, 255), new Rgba32(0, 255, 255, 255)),
                _ => TestDmiFactory.CreateImage(new Rgba32(32, 32, 32, 255), new Rgba32(64, 64, 64, 255))
            }));

        TestDmiFactory.CreateDmi(
            outputPath,
            TestDmiFactory.CreateState("base", DirectionDepth.Four, 2, 1, static _ => TestDmiFactory.CreateImage(
                new Rgba32(1, 1, 1, 255),
                new Rgba32(2, 2, 2, 255))));

        var config = SpriteConfig.CreateEmpty(
                "config",
                new SpriteResolution(2, 1),
                SupportedDirectionSet.Four,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"))
            .SetMapping(SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(1, 0));

        var logger = new RecordingLogger<DmiSharpConfigWriter>();
        var writer = new DmiSharpConfigWriter(logger);
        var request = new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting);

        var result = await writer.ApplyAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BatchFileStatus.Processed);
        result.Value.InputPath.Should().Be(Path.GetFullPath(inputPath));
        result.Value.OutputPath.Should().Be(Path.GetFullPath(outputPath));

        using var outputFile = new DMIFile(outputPath);
        var southFrame = outputFile.States.First().GetFrame(StateDirection.South, 0);
        southFrame.Should().NotBeNull();
        TestDmiFactory.ReadPixel(southFrame!, 0, 0).Should().Be(new Rgba32(0, 255, 0, 255));
        TestDmiFactory.ReadPixel(southFrame!, 1, 0).Should().Be(new Rgba32(0, 255, 0, 255));
        AssertCompleteOperationLog(logger, eventId: 2301, expectedResult: "processed");
    }

    [Fact]
    public async Task ApplyAsyncShouldSupportEightDirectionDmi()
    {
        var inputPath = Path.Combine(_tempDirectory, "eight-dir.dmi");
        var outputPath = Path.Combine(_tempDirectory, "eight-dir-output.dmi");

        TestDmiFactory.CreateDmi(
            inputPath,
            TestDmiFactory.CreateState("hero", DirectionDepth.Eight, 1, 1, static direction => TestDmiFactory.CreateImage(
                direction == StateDirection.NorthEast
                    ? new Rgba32(10, 20, 30, 255)
                    : new Rgba32(200, 200, 200, 255))));

        var config = SpriteConfig.CreateEmpty(
                "config-eight",
                new SpriteResolution(1, 1),
                SupportedDirectionSet.Eight,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"))
            .SetMapping(SpriteDirection.NorthEast, new PixelCoordinate(0, 0), null);

        var logger = new RecordingLogger<DmiSharpConfigWriter>();
        var writer = new DmiSharpConfigWriter(logger);

        var result = await writer.ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using var outputFile = new DMIFile(outputPath);
        var northEastFrame = outputFile.States.First().GetFrame(StateDirection.NorthEast, 0);
        northEastFrame.Should().NotBeNull();
        TestDmiFactory.ReadPixel(northEastFrame!, 0, 0).A.Should().Be(0);
    }

    [Fact]
    public async Task ApplyAsyncShouldRejectEmptyDmiFiles()
    {
        var inputPath = Path.Combine(_tempDirectory, "empty.dmi");
        await File.WriteAllBytesAsync(inputPath, []);

        var logger = new RecordingLogger<DmiSharpConfigWriter>();
        var writer = new DmiSharpConfigWriter(logger);
        var config = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(1, 1),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"));

        var result = await writer.ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, Path.Combine(_tempDirectory, "out.dmi"), config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("empty");
        AssertCompleteOperationLog(logger, eventId: 2303, expectedResult: "validation_failed");
    }

    [Fact]
    public async Task ApplyAsyncShouldPreserveStateOrderAndSupportInPlaceSave()
    {
        var dmiPath = Path.Combine(_tempDirectory, "in-place.dmi");
        TestDmiFactory.CreateDmi(
            dmiPath,
            TestDmiFactory.CreateState("z-last", DirectionDepth.Four, 1, 1, static _ =>
                TestDmiFactory.CreateImage(new Rgba32(10, 20, 30, 255))),
            TestDmiFactory.CreateState("a-first", DirectionDepth.Four, 1, 1, static _ =>
                TestDmiFactory.CreateImage(new Rgba32(40, 50, 60, 255))));

        var config = SpriteConfig.CreateEmpty(
            "in-place",
            new SpriteResolution(1, 1),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"));

        var result = await new DmiSharpConfigWriter().ApplyAsync(
            new ApplyConfigToFileRequest(dmiPath, dmiPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using var reopened = new DMIFile(dmiPath);
        reopened.States.Select(static state => state.Name).Should().Equal("z-last", "a-first");
    }

    [Fact]
    public async Task ApplyAsyncShouldReplaceLongerExistingFileWithoutLeavingOldStates()
    {
        var inputPath = Path.Combine(_tempDirectory, "short-input.dmi");
        var outputPath = Path.Combine(_tempDirectory, "long-output.dmi");
        TestDmiFactory.CreateDmi(
            inputPath,
            TestDmiFactory.CreateState("only", DirectionDepth.Four, 1, 1, static _ =>
                TestDmiFactory.CreateImage(new Rgba32(1, 2, 3, 255))));
        TestDmiFactory.CreateDmi(
            outputPath,
            TestDmiFactory.CreateState("old-one", DirectionDepth.Four, 1, 1, static _ =>
                TestDmiFactory.CreateImage(new Rgba32(4, 5, 6, 255))),
            TestDmiFactory.CreateState("old-two", DirectionDepth.Four, 1, 1, static _ =>
                TestDmiFactory.CreateImage(new Rgba32(7, 8, 9, 255))));

        var config = SpriteConfig.CreateEmpty(
            "replace",
            new SpriteResolution(1, 1),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"));

        var result = await new DmiSharpConfigWriter().ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using var reopened = new DMIFile(outputPath);
        reopened.States.Select(static state => state.Name).Should().Equal("only");
    }

    [Fact]
    public async Task NoOpRoundTripShouldPreserveAsymmetricThreeByThreeStatesFramesMetadataAndRgbaHashes()
    {
        var inputPath = Path.Combine(_tempDirectory, "asymmetric-3x3-input.dmi");
        var outputPath = Path.Combine(_tempDirectory, "asymmetric-3x3-output.dmi");
        TestDmiFactory.CreateDmi(
            inputPath,
            CreateAsymmetricState("z-first", DirectionDepth.Four, 3, frameCount: 2, stateSeed: 0),
            CreateAsymmetricState("a-second", DirectionDepth.Four, 3, frameCount: 2, stateSeed: 1));
        var expected = CaptureDmiSnapshot(inputPath);
        var config = SpriteConfig.CreateEmpty(
            "no-op-3x3",
            new SpriteResolution(3, 3),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"));

        var result = await new DmiSharpConfigWriter().ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        CaptureDmiSnapshot(outputPath).Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ApplyAsyncShouldTransformEveryFrameOfAsymmetricFourByFourEightDirectionDmi()
    {
        const int stateSeed = 2;
        var inputPath = Path.Combine(_tempDirectory, "asymmetric-4x4-input.dmi");
        var outputPath = Path.Combine(_tempDirectory, "asymmetric-4x4-output.dmi");
        TestDmiFactory.CreateDmi(
            inputPath,
            CreateAsymmetricState("animated-eight", DirectionDepth.Eight, 4, frameCount: 2, stateSeed: stateSeed));
        var config = SpriteConfig.CreateEmpty(
                "transform-4x4",
                new SpriteResolution(4, 4),
                SupportedDirectionSet.Eight,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"))
            .SetMapping(SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(3, 3))
            .SetMapping(SpriteDirection.NorthEast, new PixelCoordinate(3, 0), null);
        var mappingHash = ComputeMappingHash(config);
        var assetResult = await new DmiSharpReader().LoadAsync(inputPath, CancellationToken.None);
        assetResult.IsSuccess.Should().BeTrue();
        var previewResult = await new DmiSharpPreviewBuilder().BuildAsync(
            new PreviewBuildRequest(
                assetResult.Value,
                config,
                new PreviewSelection("animated-eight", null, null),
                SpriteDirection.South),
            CancellationToken.None);
        previewResult.IsSuccess.Should().BeTrue();
        using var expectedPreview = CreateAsymmetricImage(4, stateSeed, StateDirection.South, frameIndex: 0);
        expectedPreview[0, 0] = CreatePatternPixel(stateSeed, StateDirection.South, 0, 3, 3);
        var expectedPreviewHash = ComputeRgbaHash(expectedPreview);
        ComputeRgbaHash(previewResult.Value.CompositeImage!).Should().Be(expectedPreviewHash);

        var result = await new DmiSharpConfigWriter().ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using var reopened = new DMIFile(outputPath);
        var state = reopened.States.Should().ContainSingle().Subject;
        state.Name.Should().Be("animated-eight");
        state.DirectionDepth.Should().Be(DirectionDepth.Eight);
        state.TotalFrames.Should().Be(16);
        state.Width.Should().Be(4);
        state.Height.Should().Be(4);
        ComputeMappingHash(config).Should().Be(mappingHash, "preview and DMI writing must not mutate the mapping plan");
        ComputeRgbaHash(state.GetFrame(StateDirection.South, 0)!).Should().Be(expectedPreviewHash);

        for (var frameIndex = 0; frameIndex < 2; frameIndex++)
        {
            foreach (var direction in TestDmiFactory.GetDirections(DirectionDepth.Eight))
            {
                var actual = state.GetFrame(direction, frameIndex);
                actual.Should().NotBeNull();
                using var expected = CreateAsymmetricImage(4, stateSeed, direction, frameIndex);
                if (direction == StateDirection.South)
                {
                    expected[0, 0] = CreatePatternPixel(stateSeed, direction, frameIndex, 3, 3);
                }
                else if (direction == StateDirection.NorthEast)
                {
                    expected[3, 0] = default;
                }

                ComputeRgbaHash(actual!).Should().Be(ComputeRgbaHash(expected), $"frame {frameIndex} in {direction} must match");
            }
        }
    }

    [Fact]
    public async Task VerificationFailureShouldLeaveExistingOutputUnchangedAndDeleteTemporaryFile()
    {
        var (inputPath, outputPath, config) = CreateFailureFixture("verification");
        var originalHash = await ComputeFileHashAsync(outputPath);
        var logger = new RecordingLogger<DmiSharpConfigWriter>();
        var writer = new DmiSharpConfigWriter(new RejectingArtifactValidator(), new DmiAtomicCommitter(), logger);

        var result = await writer.ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        (await ComputeFileHashAsync(outputPath)).Should().Be(originalHash);
        Directory.GetFiles(_tempDirectory, "*.tmp.dmi").Should().BeEmpty();
        AssertCompleteOperationLog(logger, eventId: 2303, expectedResult: "validation_failed");
    }

    [Fact]
    public async Task ReplacementFailureShouldLeaveExistingOutputUnchangedAndDeleteTemporaryFile()
    {
        var (inputPath, outputPath, config) = CreateFailureFixture("replacement");
        var originalHash = await ComputeFileHashAsync(outputPath);
        var writer = new DmiSharpConfigWriter(new DmiArtifactValidator(), new ThrowingAtomicCommitter());

        var result = await writer.ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        (await ComputeFileHashAsync(outputPath)).Should().Be(originalHash);
        Directory.GetFiles(_tempDirectory, "*.tmp.dmi").Should().BeEmpty();
    }

    [Fact]
    public async Task CancellationDuringVerificationShouldLeaveExistingOutputUnchangedAndDeleteTemporaryFile()
    {
        var (inputPath, outputPath, config) = CreateFailureFixture("cancellation");
        var originalHash = await ComputeFileHashAsync(outputPath);
        using var cancellationSource = new CancellationTokenSource();
        var logger = new RecordingLogger<DmiSharpConfigWriter>();
        var writer = new DmiSharpConfigWriter(
            new CancellingArtifactValidator(cancellationSource),
            new DmiAtomicCommitter(),
            logger);

        var result = await writer.ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            cancellationSource.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("cancelled");
        (await ComputeFileHashAsync(outputPath)).Should().Be(originalHash);
        Directory.GetFiles(_tempDirectory, "*.tmp.dmi").Should().BeEmpty();
        AssertCompleteOperationLog(logger, eventId: 2302, expectedResult: "cancelled");
    }

    private (string InputPath, string OutputPath, SpriteConfig Config) CreateFailureFixture(string prefix)
    {
        var inputPath = Path.Combine(_tempDirectory, $"{prefix}-input.dmi");
        var outputPath = Path.Combine(_tempDirectory, $"{prefix}-output.dmi");
        TestDmiFactory.CreateDmi(
            inputPath,
            TestDmiFactory.CreateState("input", DirectionDepth.Four, 1, 1, static _ =>
                TestDmiFactory.CreateImage(new Rgba32(1, 2, 3, 255))));
        TestDmiFactory.CreateDmi(
            outputPath,
            TestDmiFactory.CreateState("original", DirectionDepth.Four, 1, 1, static _ =>
                TestDmiFactory.CreateImage(new Rgba32(200, 201, 202, 255))));

        var config = SpriteConfig.CreateEmpty(
            prefix,
            new SpriteResolution(1, 1),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"));
        return (inputPath, outputPath, config);
    }

    private static async Task<string> ComputeFileHashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream));
    }

    private static DMIState CreateAsymmetricState(
        string name,
        DirectionDepth depth,
        int size,
        int frameCount,
        int stateSeed) =>
        TestDmiFactory.CreateState(
            name,
            depth,
            size,
            size,
            frameCount,
            (direction, frameIndex) => CreateAsymmetricImage(size, stateSeed, direction, frameIndex));

    private static Image<Rgba32> CreateAsymmetricImage(
        int size,
        int stateSeed,
        StateDirection direction,
        int frameIndex) =>
        TestDmiFactory.CreateImage(
            size,
            size,
            (x, y) => CreatePatternPixel(stateSeed, direction, frameIndex, x, y));

    private static Rgba32 CreatePatternPixel(
        int stateSeed,
        StateDirection direction,
        int frameIndex,
        int x,
        int y)
    {
        var directionSeed = direction switch
        {
            StateDirection.South => 0,
            StateDirection.North => 1,
            StateDirection.East => 2,
            StateDirection.West => 3,
            StateDirection.SouthEast => 4,
            StateDirection.SouthWest => 5,
            StateDirection.NorthEast => 6,
            StateDirection.NorthWest => 7,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        return new Rgba32(
            (byte)(10 + (stateSeed * 70) + (directionSeed * 9) + (frameIndex * 3) + x),
            (byte)(20 + (y * 20) + (frameIndex * 7) + stateSeed),
            (byte)(30 + (x * 15) + (y * 4) + directionSeed),
            (byte)(100 + (stateSeed * 30) + (frameIndex * 20) + (directionSeed * 2)));
    }

    private static DmiSnapshot CaptureDmiSnapshot(string path)
    {
        using var dmiFile = new DMIFile(path);
        return new DmiSnapshot(
            dmiFile.States.Select(state =>
            {
                var directions = TestDmiFactory.GetDirections(state.DirectionDepth);
                var frameCount = state.TotalFrames / directions.Count;
                var frames = new List<FrameSnapshot>(state.TotalFrames);
                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    foreach (var direction in directions)
                    {
                        var frame = state.GetFrame(direction, frameIndex);
                        frame.Should().NotBeNull();
                        frames.Add(new FrameSnapshot(direction, frameIndex, ComputeRgbaHash(frame!)));
                    }
                }

                return new StateSnapshot(
                    state.Name,
                    state.DirectionDepth,
                    state.TotalFrames,
                    state.Width,
                    state.Height,
                    frames);
            }).ToArray());
    }

    private static string ComputeRgbaHash(Image<Rgba32> image)
    {
        var rgba = new byte[checked(image.Width * image.Height * 4)];
        var offset = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    rgba[offset++] = pixel.R;
                    rgba[offset++] = pixel.G;
                    rgba[offset++] = pixel.B;
                    rgba[offset++] = pixel.A;
                }
            }
        });

        return Convert.ToHexString(SHA256.HashData(rgba));
    }

    private static string ComputeRgbaHash(SpriteImage image) =>
        Convert.ToHexString(SHA256.HashData(image.RgbaBytes));

    private static string ComputeMappingHash(SpriteConfig config)
    {
        var normalized = string.Join(
            "|",
            config.Directions
                .OrderBy(static direction => (int)direction)
                .SelectMany(direction => config.GetMappings(direction)
                    .OrderBy(static mapping => mapping.Source.Y)
                    .ThenBy(static mapping => mapping.Source.X)
                    .Select(mapping =>
                        $"{direction}:{mapping.Source.X},{mapping.Source.Y}->" +
                        $"{mapping.Target?.X.ToString(CultureInfo.InvariantCulture) ?? "null"}," +
                        $"{mapping.Target?.Y.ToString(CultureInfo.InvariantCulture) ?? "null"}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static void AssertCompleteOperationLog<T>(
        RecordingLogger<T> logger,
        int eventId,
        string expectedResult)
    {
        var entry = logger.Entries.Should().ContainSingle(item => item.EventId.Id == eventId).Subject;
        entry.Message.Should().Contain($"result={expectedResult}");
        entry.Properties.Should().ContainKey("OperationId").WhoseValue.Should().NotBeNull();
        entry.Properties.Should().Contain("Tool", "DmiWriter");
        entry.Properties.Should().Contain("State", "all");
        entry.Properties.Should().Contain("Direction", "all");
        entry.Properties.Should().Contain("Frame", "all");
        entry.Properties.Should().Contain("Scope", "Batch");
        entry.Properties.Should().ContainKey("Applied");
        entry.Properties.Should().Contain("Skipped", 0);
        entry.Properties.Should().ContainKey("InputPath");
        entry.Properties.Should().ContainKey("OutputPath");
    }

    private sealed record DmiSnapshot(IReadOnlyList<StateSnapshot> States);

    private sealed record StateSnapshot(
        string Name,
        DirectionDepth DirectionDepth,
        int TotalFrames,
        int Width,
        int Height,
        IReadOnlyList<FrameSnapshot> Frames);

    private sealed record FrameSnapshot(StateDirection Direction, int FrameIndex, string RgbaSha256);

    private sealed record RecordedLogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<RecordedLogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values
                    .Where(static pair => pair.Key != "{OriginalFormat}")
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);
            Entries.Add(new RecordedLogEntry(logLevel, eventId, formatter(state, exception), properties));
        }
    }

    private sealed class RejectingArtifactValidator : IDmiArtifactValidator
    {
        public Result Validate(string path, DmiArtifactFingerprint expected, CancellationToken cancellationToken) =>
            Result.Failure(Errors.Validation("Synthetic verification failure."));
    }

    private sealed class ThrowingAtomicCommitter : IDmiAtomicCommitter
    {
        public void Commit(string temporaryPath, string destinationPath) =>
            throw new IOException("Synthetic replacement failure.");
    }

    private sealed class CancellingArtifactValidator(CancellationTokenSource cancellationSource) : IDmiArtifactValidator
    {
        public Result Validate(string path, DmiArtifactFingerprint expected, CancellationToken cancellationToken)
        {
            cancellationSource.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Result.Success();
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
