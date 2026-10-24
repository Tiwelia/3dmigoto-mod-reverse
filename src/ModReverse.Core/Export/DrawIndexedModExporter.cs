using System.Text;
using ModReverse.Core.Analysis;
using ModReverse.Core.Ini;
using ModReverse.Core.Models;
using ModReverse.Core.Profiles;

namespace ModReverse.Core.Export;

public sealed class DrawIndexedModExporter(IGameProfile gameProfile)
{
    private readonly IGameProfile _gameProfile =
        gameProfile ?? throw new ArgumentNullException(nameof(gameProfile));

    public async Task<IReadOnlyList<ExportedModel>> ExportAsync(
        string iniPath,
        string outputDirectory,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullIniPath = Path.GetFullPath(iniPath);
        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        if (!File.Exists(fullIniPath))
        {
            throw new FileNotFoundException("The selected INI file does not exist.", fullIniPath);
        }

        if (Directory.Exists(fullOutputDirectory)
            && Directory.EnumerateFileSystemEntries(fullOutputDirectory).Any()
            && !overwrite)
        {
            throw new IOException(
                $"Output directory is not empty: {fullOutputDirectory}");
        }

        Directory.CreateDirectory(fullOutputDirectory);
        var document = await MigotoIniParser.ParseFileAsync(
            fullIniPath,
            cancellationToken).ConfigureAwait(false);
        var parts = MigotoModAnalyzer.Analyze(document);
        if (parts.Count == 0)
        {
            throw new InvalidDataException(
                "No model resource set containing IB, Position, Texcoord and Blend was found.");
        }

        var iniDirectory = Path.GetDirectoryName(fullIniPath)!;
        var hashOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var results = new List<ExportedModel>();
        foreach (var part in parts)
        {
            hashOrdinals.TryGetValue(part.Hash, out var ordinal);
            hashOrdinals[part.Hash] = ordinal + 1;
            var partResults = await ExportPartAsync(
                part,
                ordinal,
                iniDirectory,
                fullOutputDirectory,
                cancellationToken).ConfigureAwait(false);
            results.AddRange(partResults);
        }

        return results;
    }

    private async Task<IReadOnlyList<ExportedModel>> ExportPartAsync(
        ModModelPart part,
        int partOrdinal,
        string iniDirectory,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        if (part.DrawCalls.Count == 0)
        {
            return [];
        }

        var positionStride = RequireStride(part.PositionBuffer);
        var texcoordStride = RequireStride(part.TexcoordBuffer);
        var blendStride = RequireStride(part.BlendBuffer);
        var layouts = _gameProfile.GetVertexLayouts(
            positionStride,
            texcoordStride,
            blendStride);
        if (layouts.Count == 0)
        {
            throw new InvalidDataException(
                $"Game profile {_gameProfile.Id} has no layout for "
                + $"Position={positionStride}, Texcoord={texcoordStride}, Blend={blendStride}.");
        }

        var positionPath = ResolveResourcePath(iniDirectory, part.PositionBuffer);
        var texcoordPath = ResolveResourcePath(iniDirectory, part.TexcoordBuffer);
        var blendPath = ResolveResourcePath(iniDirectory, part.BlendBuffer);
        var indexPath = ResolveResourcePath(iniDirectory, part.IndexBuffer);
        EnsureFileExists(positionPath, part.PositionBuffer.Name);
        EnsureFileExists(texcoordPath, part.TexcoordBuffer.Name);
        EnsureFileExists(blendPath, part.BlendBuffer.Name);
        EnsureFileExists(indexPath, part.IndexBuffer.Name);

        var results = new List<ExportedModel>();
        foreach (var drawCall in part.DrawCalls)
        {
            var slice = await IndexBufferSlicer.SliceAsync(
                indexPath,
                part.IndexBuffer.Format,
                drawCall.IndexCount,
                drawCall.StartIndex,
                drawCall.BaseVertex,
                cancellationToken).ConfigureAwait(false);
            var prefix =
                $"{part.Hash}-1-{drawCall.IndexCount}_{drawCall.StartIndex}";
            var firstLayout = layouts[0];
            var firstPaths = CreateOutputPaths(
                outputDirectory,
                part.Hash,
                firstLayout,
                partOrdinal,
                prefix);
            Directory.CreateDirectory(firstPaths.Directory);
            await File.WriteAllBytesAsync(
                firstPaths.IndexBuffer,
                slice.RebasedIndices,
                cancellationToken).ConfigureAwait(false);

            await using (var position = File.OpenRead(positionPath))
            await using (var texcoord = File.OpenRead(texcoordPath))
            await using (var blend = File.OpenRead(blendPath))
            await using (var output = File.Create(firstPaths.VertexBuffer))
            {
                await VertexBufferInterleaver.InterleaveRangeAsync(
                    position,
                    positionStride,
                    texcoord,
                    texcoordStride,
                    blend,
                    blendStride,
                    output,
                    slice.FirstVertex,
                    slice.VertexCount,
                    cancellationToken).ConfigureAwait(false);
            }

            await File.WriteAllTextAsync(
                firstPaths.Format,
                FmtWriter.Render(firstLayout, prefix, slice.Format),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken).ConfigureAwait(false);
            results.Add(
                new ExportedModel(
                    part.Hash,
                    firstLayout.GameTypeName,
                    slice.VertexCount,
                    firstPaths.IndexBuffer,
                    firstPaths.VertexBuffer,
                    firstPaths.Format));

            foreach (var layout in layouts.Skip(1))
            {
                var paths = CreateOutputPaths(
                    outputDirectory,
                    part.Hash,
                    layout,
                    partOrdinal,
                    prefix);
                Directory.CreateDirectory(paths.Directory);
                File.Copy(firstPaths.IndexBuffer, paths.IndexBuffer, overwrite: true);
                File.Copy(firstPaths.VertexBuffer, paths.VertexBuffer, overwrite: true);
                await File.WriteAllTextAsync(
                    paths.Format,
                    FmtWriter.Render(layout, prefix, slice.Format),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    cancellationToken).ConfigureAwait(false);
                results.Add(
                    new ExportedModel(
                        part.Hash,
                        layout.GameTypeName,
                        slice.VertexCount,
                        paths.IndexBuffer,
                        paths.VertexBuffer,
                        paths.Format));
            }
        }

        return results;
    }

    private static int RequireStride(MigotoResource resource) =>
        resource.Stride is > 0
            ? resource.Stride.Value
            : throw new InvalidDataException(
                $"Resource {resource.Name} has no valid stride.");

    private static string ResolveResourcePath(
        string iniDirectory,
        MigotoResource resource) =>
        Path.GetFullPath(Path.Combine(iniDirectory, resource.FileName));

    private static void EnsureFileExists(string path, string resourceName)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"File for resource {resourceName} was not found.",
                path);
        }
    }

    private static (
        string Directory,
        string IndexBuffer,
        string VertexBuffer,
        string Format) CreateOutputPaths(
        string outputDirectory,
        string hash,
        VertexLayout layout,
        int partOrdinal,
        string prefix)
    {
        var directory = Path.Combine(
            outputDirectory,
            $"{hash}_{layout.GameTypeName}-{partOrdinal}");
        return (
            directory,
            Path.Combine(directory, prefix + ".ib"),
            Path.Combine(directory, prefix + ".vb"),
            Path.Combine(directory, prefix + ".fmt"));
    }
}
