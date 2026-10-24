using ModReverse.Core.Analysis;
using ModReverse.Core.Ini;

namespace ModReverse.Tests.Analysis;

public sealed class WwmiModAnalyzerTests
{
    [Fact]
    public void AnalyzePrefersFileBackedBindingOverRuntimeReferenceOverride()
    {
        const string ini = """
            [CommandListOverrideSharedResources]
            ib = ResourceIndexBuffer
            vb0 = ResourcePositionBuffer
            vb1 = ResourceVectorBuffer
            vb2 = ResourceTexcoordBuffer
            vb3 = ResourceColorBuffer
            vb4 = ResourceBlendBuffer
            vb4 = ref ResourceBlendBufferOverride

            [TextureOverrideComponent0]
            hash = 0266ab77
            drawindexed = 3, 0, 0

            [ResourceIndexBuffer]
            filename = Index.buf

            [ResourcePositionBuffer]
            stride = 12
            filename = Position.buf

            [ResourceVectorBuffer]
            stride = 8
            filename = Vector.buf

            [ResourceTexcoordBuffer]
            stride = 16
            filename = Texcoord.buf

            [ResourceColorBuffer]
            stride = 4
            filename = Color.buf

            [ResourceBlendBuffer]
            stride = 16
            filename = Blend.buf

            [ResourceBlendBufferOverride]
            type = RWBuffer
            """;

        var model = WwmiModAnalyzer.Analyze(MigotoIniParser.Parse(ini));

        Assert.Equal("ResourceBlendBuffer", model.BlendBuffer.Name);
        Assert.Equal(16, model.BlendBuffer.Stride);
    }
}
