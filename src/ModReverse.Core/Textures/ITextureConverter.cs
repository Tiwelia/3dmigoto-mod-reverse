namespace ModReverse.Core.Textures;

public interface ITextureConverter
{
    Task ConvertAsync(
        string sourcePath,
        string destinationDirectory,
        TextureFileType fileType,
        float quality,
        CancellationToken cancellationToken = default);
}
