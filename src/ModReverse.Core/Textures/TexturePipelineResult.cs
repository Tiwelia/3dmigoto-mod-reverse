namespace ModReverse.Core.Textures;

public sealed record TexturePipelineResult(
    int SourceTextureCount,
    int DdsCopyCount,
    int PngCount,
    int JpgCount);
