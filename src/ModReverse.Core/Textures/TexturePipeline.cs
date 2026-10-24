namespace ModReverse.Core.Textures;

public sealed class TexturePipeline(ITextureConverter converter)
{
    private readonly ITextureConverter _converter =
        converter ?? throw new ArgumentNullException(nameof(converter));

    public async Task<TexturePipelineResult> ProcessAsync(
        string sourceDirectory,
        string outputDirectory,
        TextureOutputOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        options ??= new TextureOutputOptions();

        var fullSourceDirectory = Path.GetFullPath(sourceDirectory);
        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        if (!Directory.Exists(fullSourceDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Texture source directory was not found: {fullSourceDirectory}");
        }

        var sourceFiles = Directory
            .EnumerateFiles(fullSourceDirectory, "*", SearchOption.AllDirectories)
            .Where(path => string.Equals(
                Path.GetExtension(path),
                ".dds",
                StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var ddsCount = 0;
        var pngCount = 0;
        var jpgCount = 0;
        foreach (var sourcePath in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(fullSourceDirectory, sourcePath);
            var relativeDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;

            if (options.PreserveDds)
            {
                var ddsDestination = Path.Combine(
                    fullOutputDirectory,
                    "Textures",
                    "DDS",
                    relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(ddsDestination)!);
                File.Copy(sourcePath, ddsDestination, overwrite: true);
                ddsCount++;
            }

            if (options.CreatePng)
            {
                var pngDirectory = Path.Combine(
                    fullOutputDirectory,
                    "Textures",
                    "PNG",
                    relativeDirectory);
                await _converter.ConvertAsync(
                    sourcePath,
                    pngDirectory,
                    TextureFileType.Png,
                    quality: 1,
                    cancellationToken).ConfigureAwait(false);
                pngCount++;
            }

            if (options.CreateJpg)
            {
                var jpgDirectory = Path.Combine(
                    fullOutputDirectory,
                    "Textures",
                    "JPG",
                    relativeDirectory);
                await _converter.ConvertAsync(
                    sourcePath,
                    jpgDirectory,
                    TextureFileType.Jpg,
                    options.JpgQuality,
                    cancellationToken).ConfigureAwait(false);
                jpgCount++;
            }
        }

        return new TexturePipelineResult(
            sourceFiles.Length,
            ddsCount,
            pngCount,
            jpgCount);
    }
}
