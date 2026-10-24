namespace ModReverse.Core.Profiles;

public static class WwmiVertexLayout
{
    public const string FourWeightName =
        "GPU_P12_TA4_N4_C4_BL4_BL4_T4_CO4_T1-4_T2-4";

    public const string EightWeightName =
        "GPU_P12_TA4_N4_C4_BL4_BL4_BL4_BL4_T4_CO4_T1-4_T2-4";

    public static VertexLayout FourWeightLayout { get; } = new(
        FourWeightName,
        48,
        [
            new("POSITION", 0, "R32G32B32_FLOAT", 12, 0),
            new("TANGENT", 0, "R8G8B8A8_SNORM", 4, 12),
            new("NORMAL", 0, "R8G8B8A8_SNORM", 4, 16),
            new("COLOR", 0, "R8G8B8A8_UNORM", 4, 20),
            new("BLENDINDICES", 0, "R8G8B8A8_UINT", 4, 24),
            new("BLENDWEIGHTS", 0, "R8G8B8A8_UNORM", 4, 28),
            new("TEXCOORD", 0, "R16G16_FLOAT", 4, 32),
            new("COLOR1", 0, "R16G16_UNORM", 4, 36),
            new("TEXCOORD", 1, "R16G16_FLOAT", 4, 40),
            new("TEXCOORD", 2, "R16G16_FLOAT", 4, 44),
        ]);

    public static VertexLayout EightWeightLayout { get; } = new(
        EightWeightName,
        56,
        [
            new("POSITION", 0, "R32G32B32_FLOAT", 12, 0),
            new("TANGENT", 0, "R8G8B8A8_SNORM", 4, 12),
            new("NORMAL", 0, "R8G8B8A8_SNORM", 4, 16),
            new("COLOR", 0, "R8G8B8A8_UNORM", 4, 20),
            new("BLENDINDICES", 0, "R8G8B8A8_UINT", 4, 24),
            new("BLENDWEIGHTS", 0, "R8G8B8A8_UNORM", 4, 28),
            new("BLENDINDICES", 1, "R8G8B8A8_UINT", 4, 32),
            new("BLENDWEIGHTS", 1, "R8G8B8A8_UNORM", 4, 36),
            new("TEXCOORD", 0, "R16G16_FLOAT", 4, 40),
            new("COLOR1", 0, "R16G16_UNORM", 4, 44),
            new("TEXCOORD", 1, "R16G16_FLOAT", 4, 48),
            new("TEXCOORD", 2, "R16G16_FLOAT", 4, 52),
        ]);

    public static VertexLayout GetLayout(int blendStride) =>
        blendStride switch
        {
            8 => FourWeightLayout,
            16 => EightWeightLayout,
            _ => throw new InvalidDataException(
                $"Unsupported WWMI blend stride {blendStride}; expected 8 or 16."),
        };
}
