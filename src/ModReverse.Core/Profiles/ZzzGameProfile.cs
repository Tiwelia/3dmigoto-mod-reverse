namespace ModReverse.Core.Profiles;

public sealed class ZzzGameProfile : IGameProfile
{
    private static readonly IReadOnlyList<VertexLayout> Blend32Layouts =
    [
        CreateLayout(
            "GPU_P12_N12_TA16_C4_T4_T1-8_T2-4_BL16_BL16_33",
            92,
            ("POSITION", 0, "R32G32B32_FLOAT", 12),
            ("NORMAL", 0, "R32G32B32_FLOAT", 12),
            ("TANGENT", 0, "R32G32B32A32_FLOAT", 16),
            ("COLOR", 0, "R8G8B8A8_UNORM", 4),
            ("TEXCOORD", 0, "R16G16_FLOAT", 4),
            ("TEXCOORD", 1, "R32G32_FLOAT", 8),
            ("TEXCOORD", 2, "R16G16_FLOAT", 4),
            ("BLENDWEIGHTS", 0, "R32G32B32A32_FLOAT", 16),
            ("BLENDINDICES", 0, "R32G32B32A32_SINT", 16)),
        CreateLayout(
            "GPU_P12_N12_TA16_C4_T8_T1-4_T2-4_BL16_BL16_36",
            92,
            ("POSITION", 0, "R32G32B32_FLOAT", 12),
            ("NORMAL", 0, "R32G32B32_FLOAT", 12),
            ("TANGENT", 0, "R32G32B32A32_FLOAT", 16),
            ("COLOR", 0, "R8G8B8A8_UNORM", 4),
            ("TEXCOORD", 0, "R32G32_FLOAT", 8),
            ("TEXCOORD", 1, "R16G16_FLOAT", 4),
            ("TEXCOORD", 2, "R16G16_FLOAT", 4),
            ("BLENDWEIGHTS", 0, "R32G32B32A32_FLOAT", 16),
            ("BLENDINDICES", 0, "R32G32B32A32_SINT", 16)),
        CreateLayout(
            "GPU_P12_N12_TA16_C4_T8_T1-8_BL16_BL16_2",
            92,
            ("POSITION", 0, "R32G32B32_FLOAT", 12),
            ("NORMAL", 0, "R32G32B32_FLOAT", 12),
            ("TANGENT", 0, "R32G32B32A32_FLOAT", 16),
            ("COLOR", 0, "R8G8B8A8_UNORM", 4),
            ("TEXCOORD", 0, "R32G32_FLOAT", 8),
            ("TEXCOORD", 1, "R32G32_FLOAT", 8),
            ("BLENDWEIGHTS", 0, "R32G32B32A32_FLOAT", 16),
            ("BLENDINDICES", 0, "R32G32B32A32_SINT", 16)),
    ];

    private static readonly IReadOnlyList<VertexLayout> Blend8Layouts =
    [
        CreateLayout(
            "GPU_P12_N12_TA16_C4_T4_T1-8_T2-4_BL4_BL4_15",
            68,
            ("POSITION", 0, "R32G32B32_FLOAT", 12),
            ("NORMAL", 0, "R32G32B32_FLOAT", 12),
            ("TANGENT", 0, "R32G32B32A32_FLOAT", 16),
            ("COLOR", 0, "R8G8B8A8_UNORM", 4),
            ("TEXCOORD", 0, "R16G16_FLOAT", 4),
            ("TEXCOORD", 1, "R32G32_FLOAT", 8),
            ("TEXCOORD", 2, "R16G16_FLOAT", 4),
            ("BLENDINDICES", 0, "R32_UINT", 4),
            ("BLENDWEIGHTS", 0, "R8G8B8A8_UNORM", 4)),
        CreateLayout(
            "GPU_P12_N12_TA16_C4_T8_T1-8_BL4_BL4_45",
            68,
            ("POSITION", 0, "R32G32B32_FLOAT", 12),
            ("NORMAL", 0, "R32G32B32_FLOAT", 12),
            ("TANGENT", 0, "R32G32B32A32_FLOAT", 16),
            ("COLOR", 0, "R8G8B8A8_UNORM", 4),
            ("TEXCOORD", 0, "R32G32_FLOAT", 8),
            ("TEXCOORD", 1, "R32G32_FLOAT", 8),
            ("BLENDWEIGHTS", 0, "R8G8B8A8_SNORM", 4),
            ("BLENDINDICES", 0, "R8G8B8A8_SINT", 4)),
    ];

    public string Id => "ZZZ";

    public IReadOnlyList<VertexLayout> GetVertexLayouts(
        int positionStride,
        int texcoordStride,
        int blendStride)
    {
        if (positionStride != 40 || texcoordStride != 20)
        {
            return [];
        }

        return blendStride switch
        {
            32 => Blend32Layouts,
            8 => Blend8Layouts,
            _ => [],
        };
    }

    private static VertexLayout CreateLayout(
        string gameTypeName,
        int stride,
        params (string Semantic, int Index, string Format, int Width)[] definitions)
    {
        var offset = 0;
        var elements = new List<VertexElement>(definitions.Length);
        foreach (var definition in definitions)
        {
            elements.Add(
                new VertexElement(
                    definition.Semantic,
                    definition.Index,
                    definition.Format,
                    definition.Width,
                    offset));
            offset += definition.Width;
        }

        if (offset != stride)
        {
            throw new InvalidOperationException(
                $"Layout {gameTypeName} declares stride {stride}, but its elements total {offset}.");
        }

        return new VertexLayout(gameTypeName, stride, elements);
    }
}
