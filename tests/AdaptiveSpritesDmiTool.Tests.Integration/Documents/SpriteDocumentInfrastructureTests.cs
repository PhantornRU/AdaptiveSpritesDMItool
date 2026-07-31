using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using AdaptiveSpritesDmiTool.Infrastructure.Documents;
using DMISharp;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Png.Chunks;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Documents;

public sealed class SpriteDocumentInfrastructureTests : IDisposable
{
    [Fact]
    public async Task WorkflowImportTargetsShouldMergeOrReturnAuxiliaryWithoutPartialSessionMutation()
    {
        var idlePath = Path.Combine(_tempDirectory, "idle.png");
        var runPath = Path.Combine(_tempDirectory, "run.png");
        await SaveSolidPngAsync(idlePath, new Rgba32(10, 20, 30, 255));
        await SaveSolidPngAsync(runPath, new Rgba32(30, 20, 10, 255));
        var probe = new AssetProbeService();
        var importer = new SpriteDocumentImporter(probe);
        var repository = new SpriteDocumentSidecarRepository(probe);
        var frameSource = new SpriteFrameSource(probe);
        var session = new SpriteDocumentSession();
        var workflow = new SpriteDocumentWorkflow(
            probe,
            importer,
            repository,
            frameSource,
            new SpriteDocumentExporter(frameSource, probe, repository),
            session);

        var first = await workflow.ImportAsync(
            new SpriteDocumentImportRequest(
                [idlePath],
                "character",
                SpriteDocumentImportKind.SingleRaster,
                StateName: "idle"),
            SpriteDocumentImportTarget.NewDocument,
            CancellationToken.None);
        var added = await workflow.ImportAsync(
            new SpriteDocumentImportRequest(
                [runPath],
                "character",
                SpriteDocumentImportKind.SingleRaster,
                StateName: "run"),
            SpriteDocumentImportTarget.AddStates,
            CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        added.IsSuccess.Should().BeTrue();
        session.CurrentDocument!.States.Select(static state => state.Name).Should().Equal("idle", "run");
        var mergedDocument = session.CurrentDocument;

        var auxiliary = await workflow.ImportAsync(
            new SpriteDocumentImportRequest(
                [runPath],
                "overlay",
                SpriteDocumentImportKind.SingleRaster,
                StateName: "overlay"),
            SpriteDocumentImportTarget.AuxiliaryLayer,
            CancellationToken.None);

        auxiliary.IsSuccess.Should().BeTrue();
        auxiliary.Value.ActiveDocument.Should().BeSameAs(mergedDocument);
        auxiliary.Value.AuxiliaryDocument!.States.Should().ContainSingle(static state => state.Name == "overlay");
        session.CurrentDocument.Should().BeSameAs(mergedDocument);

        var duplicate = await workflow.ImportAsync(
            new SpriteDocumentImportRequest(
                [runPath],
                "duplicate",
                SpriteDocumentImportKind.SingleRaster,
                StateName: "run"),
            SpriteDocumentImportTarget.AddStates,
            CancellationToken.None);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Code.Should().Be("conflict");
        session.CurrentDocument.Should().BeSameAs(mergedDocument);
        session.CurrentDocument.States.Select(static state => state.Name).Should().Equal("idle", "run");
    }

    [Fact]
    public async Task DmiDocumentRoundTripShouldPreserveHotspots()
    {
        var sourcePath = Path.Combine(_tempDirectory, "hotspots-source.dmi");
        var outputPath = Path.Combine(_tempDirectory, "hotspots-output.dmi");
        using (var image = TestDmiFactory.CreateImage(
                   2,
                   2,
                   static (_, _) => new Rgba32(1, 2, 3, 255)))
        {
            image.Metadata.GetFormatMetadata(PngFormat.Instance).TextData.Add(new PngTextData(
                "Description",
                "# BEGIN DMI\nversion = 4.0\n\twidth = 2\n\theight = 2\nstate = \"cursor\"\n\tdirs = 1\n\tframes = 1\n\thotspot = 1,2\n\thotspot = 3,4\n# END DMI\n",
                string.Empty,
                string.Empty));
            await image.SaveAsPngAsync(sourcePath);
        }

        var probe = new AssetProbeService();
        var importer = new SpriteDocumentImporter(probe);
        var importResult = await importer.ImportAsync(
            new SpriteDocumentImportRequest(
                [sourcePath],
                "hotspots",
                SpriteDocumentImportKind.NativeDmi),
            CancellationToken.None);

        importResult.IsSuccess.Should().BeTrue();
        importResult.Value.States.Single().Animation.Hotspots.Should().Equal(
            new SpriteHotspot(1, 2),
            new SpriteHotspot(3, 4));

        var repository = new SpriteDocumentSidecarRepository(probe);
        var exporter = new SpriteDocumentExporter(new SpriteFrameSource(probe), probe, repository);
        var exportResult = await exporter.ExportAsync(
            new SpriteDocumentExportRequest(
                importResult.Value,
                outputPath,
                SpriteDocumentExportFormat.Dmi,
                OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        exportResult.IsSuccess.Should().BeTrue();
        using var reopened = new DMIFile(outputPath);
        reopened.States.Single().Data.Hotspots.Should().NotBeNull();
        reopened.States.Single().Data.Hotspots!.Select(static coordinates => (coordinates[0], coordinates[1]))
            .Should().Equal((1, 2), (3, 4));
    }

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "AdaptiveSpritesDmiTool.DocumentTests",
        Guid.NewGuid().ToString("N"));

    public SpriteDocumentInfrastructureTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ProbeShouldUsePngContentAndNotDmiExtension()
    {
        var path = Path.Combine(_tempDirectory, "renamed.dmi");
        using (var image = new Image<Rgba32>(2, 3, new Rgba32(1, 2, 3, 255)))
        {
            await image.SaveAsPngAsync(path);
        }

        var result = await new AssetProbeService().ProbeAsync(path, AssetImportLimits.Default, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DetectedFormat.Should().Be(SpriteSourceFormat.Png);
        result.Value.HasDmiMetadata.Should().BeFalse();
        result.Value.ExtensionMatchesContent.Should().BeFalse();
        result.Value.Width.Should().Be(2);
        result.Value.Height.Should().Be(3);
    }

    [Fact]
    public async Task ProbeShouldDetectDmiMetadataBehindPngExtension()
    {
        var dmiPath = Path.Combine(_tempDirectory, "native.dmi");
        var pngPath = Path.Combine(_tempDirectory, "native.png");
        using var state = TestDmiFactory.CreateState(
            "idle",
            DirectionDepth.One,
            2,
            2,
            1,
            static (_, _) => TestDmiFactory.CreateImage(2, 2, static (x, y) => new Rgba32((byte)x, (byte)y, 5, 255)));
        TestDmiFactory.CreateDmi(dmiPath, state);
        File.Copy(dmiPath, pngPath);

        var result = await new AssetProbeService().ProbeAsync(pngPath, AssetImportLimits.Default, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DetectedFormat.Should().Be(SpriteSourceFormat.Dmi);
        result.Value.HasDmiMetadata.Should().BeTrue();
        result.Value.ExtensionMatchesContent.Should().BeFalse();
    }

    [Fact]
    public async Task SpriteSheetImportShouldSliceInRowsAndReadFramesLazily()
    {
        var sourcePath = Path.Combine(_tempDirectory, "sheet.png");
        using (var image = TestDmiFactory.CreateImage(4, 2, static (x, _) => x switch
        {
            0 => new Rgba32(255, 0, 0, 255),
            1 => new Rgba32(0, 255, 0, 255),
            2 => new Rgba32(0, 0, 255, 255),
            _ => new Rgba32(255, 255, 0, 255)
        }))
        {
            await image.SaveAsPngAsync(sourcePath);
        }

        var probeService = new AssetProbeService();
        var importer = new SpriteDocumentImporter(probeService);
        var recipe = new SpriteSheetSlicingRecipe(
            CellWidth: 2,
            CellHeight: 2,
            MarginLeft: 0,
            MarginTop: 0,
            HorizontalSpacing: 0,
            VerticalSpacing: 0,
            Columns: 2,
            Rows: 1,
            ReadingOrder: SpriteSheetReadingOrder.RowsFirst,
            DirectionOrder: [SpriteDirection.South],
            FramesPerDirection: 2);

        var importResult = await importer.ImportAsync(
            new SpriteDocumentImportRequest(
                [sourcePath],
                "sheet-project",
                SpriteDocumentImportKind.SpriteSheet,
                StateName: "walk",
                SlicingRecipe: recipe,
                ProjectPath: Path.Combine(_tempDirectory, "sheet.adaptive-dmi.json")),
            CancellationToken.None);

        importResult.IsSuccess.Should().BeTrue();
        importResult.Value.Resolution.Should().Be(new SpriteResolution(2, 2));
        importResult.Value.States.Should().ContainSingle();
        importResult.Value.States[0].Frames.Should().HaveCount(2);

        var frameResult = await new SpriteFrameSource(probeService).ReadAsync(
            new SpriteFrameReadRequest(importResult.Value, "walk", SpriteDirection.South, 1),
            CancellationToken.None);

        frameResult.IsSuccess.Should().BeTrue();
        ReadPixel(frameResult.Value, 0, 0).Should().Be(new Rgba32(0, 0, 255, 255));
        ReadPixel(frameResult.Value, 1, 0).Should().Be(new Rgba32(255, 255, 0, 255));
    }

    [Fact]
    public async Task SidecarShouldRoundTripAndRejectChangedSource()
    {
        var sourcePath = Path.Combine(_tempDirectory, "source.png");
        var projectPath = Path.Combine(_tempDirectory, "source.adaptive-dmi.json");
        await SaveSolidPngAsync(sourcePath, new Rgba32(10, 20, 30, 255));

        var probeService = new AssetProbeService();
        var importer = new SpriteDocumentImporter(probeService);
        var importResult = await importer.ImportAsync(
            new SpriteDocumentImportRequest(
                [sourcePath],
                "source-project",
                SpriteDocumentImportKind.SingleRaster,
                ProjectPath: projectPath),
            CancellationToken.None);
        importResult.IsSuccess.Should().BeTrue();

        var repository = new SpriteDocumentSidecarRepository(probeService);
        (await repository.SaveAsync(projectPath, importResult.Value, CancellationToken.None)).IsSuccess.Should().BeTrue();

        var loadResult = await repository.LoadAsync(new SpriteDocumentLoadRequest(projectPath), CancellationToken.None);
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.Id.Should().Be(importResult.Value.Id);
        loadResult.Value.Sources.Should().ContainSingle();
        loadResult.Value.Sources[0].RelativePath.Should().Be("source.png");

        await SaveSolidPngAsync(sourcePath, new Rgba32(30, 20, 10, 255));
        var changedResult = await repository.LoadAsync(new SpriteDocumentLoadRequest(projectPath), CancellationToken.None);

        changedResult.IsFailure.Should().BeTrue();
        changedResult.Error.Code.Should().Be("conflict");
        changedResult.Error.Message.Should().Contain("changed after the project was saved");
    }

    [Fact]
    public async Task SidecarShouldRejectCanvasOutsideSafetyLimitsBeforeMaterializingDocument()
    {
        var sourcePath = Path.Combine(_tempDirectory, "bounded-source.png");
        var projectPath = Path.Combine(_tempDirectory, "bounded.adaptive-dmi.json");
        await SaveSolidPngAsync(sourcePath, new Rgba32(10, 20, 30, 255));
        var probe = new AssetProbeService();
        var document = (await new SpriteDocumentImporter(probe).ImportAsync(
            new SpriteDocumentImportRequest(
                [sourcePath],
                "bounded",
                SpriteDocumentImportKind.SingleRaster,
                ProjectPath: projectPath),
            CancellationToken.None)).Value;
        var repository = new SpriteDocumentSidecarRepository(probe);
        (await repository.SaveAsync(projectPath, document, CancellationToken.None)).IsSuccess.Should().BeTrue();
        var json = await File.ReadAllTextAsync(projectPath);
        json = json.Replace("\"canvasWidth\": 2", "\"canvasWidth\": 20000", StringComparison.Ordinal);
        await File.WriteAllTextAsync(projectPath, json);

        var result = await repository.LoadAsync(
            new SpriteDocumentLoadRequest(projectPath),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("canvas dimensions");
    }

    [Fact]
    public async Task ProbeShouldRejectDecodedPixelBudgetBeforeImport()
    {
        var path = Path.Combine(_tempDirectory, "large.png");
        using (var image = new Image<Rgba32>(4, 4))
        {
            await image.SaveAsPngAsync(path);
        }

        var limits = AssetImportLimits.Default with { MaximumDecodedPixels = 15 };
        var result = await new AssetProbeService().ProbeAsync(path, limits, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Decoded image exceeds");
    }

    [Fact]
    public async Task DocumentExporterShouldWriteVerifiedDmiFromPngDocument()
    {
        var sourcePath = Path.Combine(_tempDirectory, "dmi-source.png");
        var outputPath = Path.Combine(_tempDirectory, "exported.dmi");
        using (var image = TestDmiFactory.CreateImage(
            new Rgba32(11, 12, 13, 255),
            new Rgba32(21, 22, 23, 128)))
        {
            await image.SaveAsPngAsync(sourcePath);
        }

        var probeService = new AssetProbeService();
        var importer = new SpriteDocumentImporter(probeService);
        var document = (await importer.ImportAsync(
            new SpriteDocumentImportRequest([sourcePath], "exported", SpriteDocumentImportKind.SingleRaster, StateName: "idle"),
            CancellationToken.None)).Value;
        var repository = new SpriteDocumentSidecarRepository(probeService);
        var exporter = new SpriteDocumentExporter(new SpriteFrameSource(probeService), probeService, repository);

        var result = await exporter.ExportAsync(
            new SpriteDocumentExportRequest(document, outputPath, SpriteDocumentExportFormat.Dmi, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using var reopened = new DMIFile(outputPath);
        reopened.States.Should().ContainSingle();
        var reopenedState = reopened.States.Single();
        reopenedState.Name.Should().Be("idle");
        reopenedState.DirectionDepth.Should().Be(DirectionDepth.One);
        using var frame = reopenedState.GetFrame(StateDirection.South, 0)!.Clone();
        frame[0, 0].Should().Be(new Rgba32(11, 12, 13, 255));
        frame[1, 0].Should().Be(new Rgba32(21, 22, 23, 128));
        Directory.GetFiles(_tempDirectory, "*.tmp.dmi").Should().BeEmpty();
    }

    [Fact]
    public async Task PngSheetExportShouldCommitManagedFolderWithReloadableSidecar()
    {
        var sourcePath = Path.Combine(_tempDirectory, "managed-source.png");
        var outputDirectory = Path.Combine(_tempDirectory, "managed-export");
        using (var image = TestDmiFactory.CreateImage(
            new Rgba32(41, 42, 43, 255),
            new Rgba32(51, 52, 53, 255)))
        {
            await image.SaveAsPngAsync(sourcePath);
        }

        var probeService = new AssetProbeService();
        var importer = new SpriteDocumentImporter(probeService);
        var document = (await importer.ImportAsync(
            new SpriteDocumentImportRequest([sourcePath], "managed", SpriteDocumentImportKind.SingleRaster),
            CancellationToken.None)).Value;
        var repository = new SpriteDocumentSidecarRepository(probeService);
        var exporter = new SpriteDocumentExporter(new SpriteFrameSource(probeService), probeService, repository);

        var result = await exporter.ExportAsync(
            new SpriteDocumentExportRequest(document, outputDirectory, SpriteDocumentExportFormat.PngSheet, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(outputDirectory, "managed.png")).Should().BeTrue();
        File.Exists(Path.Combine(outputDirectory, ".adaptive-dmi-export.json")).Should().BeTrue();
        result.Value.SidecarPath.Should().NotBeNull();

        var reloaded = await repository.LoadAsync(
            new SpriteDocumentLoadRequest(result.Value.SidecarPath!),
            CancellationToken.None);
        reloaded.IsSuccess.Should().BeTrue();
        var reloadedFrame = await new SpriteFrameSource(probeService).ReadAsync(
            new SpriteFrameReadRequest(reloaded.Value, "managed-source", SpriteDirection.South, 0),
            CancellationToken.None);
        reloadedFrame.IsSuccess.Should().BeTrue();
        ReadPixel(reloadedFrame.Value, 0, 0).Should().Be(new Rgba32(41, 42, 43, 255));
    }

    [Fact]
    public async Task PngExportShouldNotOverwriteUnmanagedDirectory()
    {
        var sourcePath = Path.Combine(_tempDirectory, "guard-source.png");
        var outputDirectory = Path.Combine(_tempDirectory, "unmanaged");
        Directory.CreateDirectory(outputDirectory);
        var protectedPath = Path.Combine(outputDirectory, "keep.txt");
        await File.WriteAllTextAsync(protectedPath, "keep");
        await SaveSolidPngAsync(sourcePath, new Rgba32(1, 2, 3, 255));

        var probeService = new AssetProbeService();
        var document = (await new SpriteDocumentImporter(probeService).ImportAsync(
            new SpriteDocumentImportRequest([sourcePath], "guard", SpriteDocumentImportKind.SingleRaster),
            CancellationToken.None)).Value;
        var repository = new SpriteDocumentSidecarRepository(probeService);
        var exporter = new SpriteDocumentExporter(new SpriteFrameSource(probeService), probeService, repository);

        var result = await exporter.ExportAsync(
            new SpriteDocumentExportRequest(document, outputDirectory, SpriteDocumentExportFormat.PngSequence, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        (await File.ReadAllTextAsync(protectedPath)).Should().Be("keep");
    }

    private static async Task SaveSolidPngAsync(string path, Rgba32 color)
    {
        using var image = new Image<Rgba32>(2, 2, color);
        await image.SaveAsPngAsync(path);
    }

    private static Rgba32 ReadPixel(SpriteImage image, int x, int y)
    {
        var index = ((y * image.Width) + x) * 4;
        return new Rgba32(
            image.RgbaBytes[index],
            image.RgbaBytes[index + 1],
            image.RgbaBytes[index + 2],
            image.RgbaBytes[index + 3]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
