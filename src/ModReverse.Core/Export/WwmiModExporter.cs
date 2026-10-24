using System.Text;
using ModReverse.Core.Analysis;
using ModReverse.Core.Ini;
using ModReverse.Core.Models;
using ModReverse.Core.Profiles;

namespace ModReverse.Core.Export;

public static class WwmiModExporter
{
    public static async Task<IReadOnlyList<ExportedModel>> ExportAsync(
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
        var model = WwmiModAnalyzer.Analyze(document);
        var iniDirectory = Path.GetDirectoryName(fullIniPath)!;
        return await ExportModelAsync(
            model,
            iniDirectory,
            fullOutputDirectory,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<ExportedModel>> ExportModelAsync(
        WwmiModel model,
        string iniDirectory,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var indexPath = ResolveResourcePath(iniDirectory, model.IndexBuffer);
        var positionPath = ResolveResourcePath(iniDirectory, model.PositionBuffer);
        var vectorPath = ResolveResourcePath(iniDirectory, model.VectorBuffer);
        var colorPath = ResolveResourcePath(iniDirectory, model.ColorBuffer);
        var blendPath = ResolveResourcePath(iniDirectory, model.BlendBuffer);
        var texcoordPath = ResolveResourcePath(iniDirectory, model.TexcoordBuffer);
        var layout = WwmiVertexLayout.GetLayout(RequireStride(model.BlendBuffer));
        EnsureFileExists(indexPath, model.IndexBuffer.Name);
        EnsureFileExists(positionPath, model.PositionBuffer.Name);
        EnsureFileExists(vectorPath, model.VectorBuffer.Name);
        EnsureFileExists(colorPath, model.ColorBuffer.Name);
        EnsureFileExists(blendPath, model.BlendBuffer.Name);
        EnsureFileExists(texcoordPath, model.TexcoordBuffer.Name);
        var shapeKeys = await ReadShapeKeysAsync(
            model,
            iniDirectory,
            cancellationToken).ConfigureAwait(false);

        var folder = Path.Combine(
            outputDirectory,
            $"{model.Hash}-{layout.GameTypeName}_0");
        Directory.CreateDirectory(folder);
        var results = new List<ExportedModel>();
        foreach (var component in model.Components)
        {
            for (var drawOrdinal = 0; drawOrdinal < component.DrawCalls.Count; drawOrdinal++)
            {
                var drawCall = component.DrawCalls[drawOrdinal];
                var slice = await IndexBufferSlicer.SliceAsync(
                    indexPath,
                    model.IndexBuffer.Format,
                    drawCall.IndexCount,
                    drawCall.StartIndex,
                    drawCall.BaseVertex,
                    cancellationToken).ConfigureAwait(false);
                var prefix =
                    $"Component {component.ComponentIndex}.{drawOrdinal}"
                    + $"_{drawCall.IndexCount}_{drawCall.StartIndex}";
                var indexOutput = Path.Combine(folder, prefix + ".ib");
                var vertexOutput = Path.Combine(folder, prefix + ".vb");
                var formatOutput = Path.Combine(folder, prefix + ".fmt");
                await File.WriteAllBytesAsync(
                    indexOutput,
                    slice.RebasedIndices,
                    cancellationToken).ConfigureAwait(false);

                await using var baseVertexBuffer = new MemoryStream();
                await using (var position = File.OpenRead(positionPath))
                await using (var vector = File.OpenRead(vectorPath))
                await using (var color = File.OpenRead(colorPath))
                await using (var blend = File.OpenRead(blendPath))
                await using (var texcoord = File.OpenRead(texcoordPath))
                {
                    await MultiBufferInterleaver.InterleaveRangeAsync(
                        [
                            new(position, RequireStride(model.PositionBuffer)),
                            new(vector, RequireStride(model.VectorBuffer)),
                            new(color, RequireStride(model.ColorBuffer)),
                            new(blend, RequireStride(model.BlendBuffer)),
                            new(texcoord, RequireStride(model.TexcoordBuffer)),
                        ],
                        baseVertexBuffer,
                        slice.FirstVertex,
                        slice.VertexCount,
                        cancellationToken).ConfigureAwait(false);
                }

                var composed = WwmiShapeKeyComposer.Compose(
                    baseVertexBuffer.ToArray(),
                    layout.Stride,
                    slice.FirstVertex,
                    slice.VertexCount,
                    shapeKeys);
                await File.WriteAllBytesAsync(
                    vertexOutput,
                    composed.Bytes,
                    cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(
                    formatOutput,
                    WwmiFmtWriter.Render(
                        layout,
                        prefix,
                        slice.Format,
                        composed.ShapeKeyIndices),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    cancellationToken).ConfigureAwait(false);
                results.Add(
                    new ExportedModel(
                        model.Hash,
                        layout.GameTypeName,
                        slice.VertexCount,
                        indexOutput,
                        vertexOutput,
                        formatOutput));
            }
        }

        return results;
    }

    private static async Task<IReadOnlyList<WwmiShapeKey>> ReadShapeKeysAsync(
        WwmiModel model,
        string iniDirectory,
        CancellationToken cancellationToken)
    {
        if (model.ShapeKeyOffsetBuffer is null
            || model.ShapeKeyVertexIdBuffer is null
            || model.ShapeKeyVertexOffsetBuffer is null)
        {
            return [];
        }

        var offsetTablePath = ResolveResourcePath(
            iniDirectory,
            model.ShapeKeyOffsetBuffer);
        var vertexIdPath = ResolveResourcePath(
            iniDirectory,
            model.ShapeKeyVertexIdBuffer);
        var vertexOffsetPath = ResolveResourcePath(
            iniDirectory,
            model.ShapeKeyVertexOffsetBuffer);
        EnsureFileExists(offsetTablePath, model.ShapeKeyOffsetBuffer.Name);
        EnsureFileExists(vertexIdPath, model.ShapeKeyVertexIdBuffer.Name);
        EnsureFileExists(vertexOffsetPath, model.ShapeKeyVertexOffsetBuffer.Name);
        return await WwmiShapeKeyReader.ReadAsync(
            offsetTablePath,
            vertexIdPath,
            vertexOffsetPath,
            cancellationToken).ConfigureAwait(false);
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
}
