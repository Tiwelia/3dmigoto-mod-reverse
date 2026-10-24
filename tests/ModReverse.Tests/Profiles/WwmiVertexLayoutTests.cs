using ModReverse.Core.Profiles;

namespace ModReverse.Tests.Profiles;

public sealed class WwmiVertexLayoutTests
{
    [Theory]
    [InlineData(8, 48, 10)]
    [InlineData(16, 56, 12)]
    public void GetLayoutSupportsFourAndEightWeights(
        int blendStride,
        int expectedStride,
        int expectedElementCount)
    {
        var layout = WwmiVertexLayout.GetLayout(blendStride);

        Assert.Equal(expectedStride, layout.Stride);
        Assert.Equal(expectedElementCount, layout.Elements.Count);
        Assert.Equal(expectedStride, layout.Elements.Sum(element => element.ByteWidth));
    }
}
