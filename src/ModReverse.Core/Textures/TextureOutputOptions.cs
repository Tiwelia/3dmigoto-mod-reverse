namespace ModReverse.Core.Textures;

public sealed record TextureOutputOptions(
    bool PreserveDds = true,
    bool CreatePng = true,
    bool CreateJpg = false,
    float JpgQuality = 0.95f);
