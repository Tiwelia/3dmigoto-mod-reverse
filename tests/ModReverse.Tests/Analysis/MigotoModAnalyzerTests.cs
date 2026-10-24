using ModReverse.Core.Analysis;
using ModReverse.Core.Ini;

namespace ModReverse.Tests.Analysis;

public sealed class MigotoModAnalyzerTests
{
    [Fact]
    public void AnalyzeFindsResourceSetAndExpandsReferencedCustomShaderDrawCalls()
    {
        const string ini = """
            [TextureOverrideBodyBlend]
            vb2 = ResourceBodyBlend
            vb0 = ResourceBodyPosition

            [TextureOverrideBody]
            hash = 1122aabb
            ib = ResourceBodyAIB
            drawindexed = 30, 0, 0
            run = CustomShaderTransparent

            [CustomShaderTransparent]
            drawindexed = 6, 30, 0

            [ResourceBodyPosition]
            type = Buffer
            stride = 40
            filename = BodyPosition.buf

            [ResourceBodyTexcoord]
            type = Buffer
            stride = 20
            filename = BodyTexcoord.buf

            [ResourceBodyBlend]
            type = Buffer
            stride = 32
            filename = BodyBlend.buf

            [ResourceBodyAIB]
            type = Buffer
            format = DXGI_FORMAT_R32_UINT
            filename = Body.ib
            """;

        var parts = MigotoModAnalyzer.Analyze(MigotoIniParser.Parse(ini));

        var part = Assert.Single(parts);
        Assert.Equal("1122aabb", part.Hash);
        Assert.Equal("ResourceBodyPosition", part.PositionBuffer.Name);
        Assert.Equal("ResourceBodyTexcoord", part.TexcoordBuffer.Name);
        Assert.Equal("ResourceBodyBlend", part.BlendBuffer.Name);
        Assert.Equal(2, part.DrawCalls.Count);
        Assert.Collection(
            part.DrawCalls,
            call => Assert.Equal(30, call.IndexCount),
            call => Assert.Equal(6, call.IndexCount));
    }
}
