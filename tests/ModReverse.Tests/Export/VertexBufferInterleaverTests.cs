using ModReverse.Core.Export;

namespace ModReverse.Tests.Export;

public sealed class VertexBufferInterleaverTests
{
    [Fact]
    public async Task InterleaveAsyncWritesPositionThenTexcoordThenBlendPerVertex()
    {
        await using var position = new MemoryStream([1, 2, 3, 4, 5, 6]);
        await using var texcoord = new MemoryStream([11, 12, 13, 14]);
        await using var blend = new MemoryStream([21, 22, 23, 24]);
        await using var output = new MemoryStream();

        var vertexCount = await VertexBufferInterleaver.InterleaveAsync(
            position,
            positionStride: 3,
            texcoord,
            texcoordStride: 2,
            blend,
            blendStride: 2,
            output);

        Assert.Equal(2, vertexCount);
        Assert.Equal(
            new byte[] { 1, 2, 3, 11, 12, 21, 22, 4, 5, 6, 13, 14, 23, 24 },
            output.ToArray());
    }

    [Fact]
    public async Task InterleaveAsyncRejectsDifferentVertexCounts()
    {
        await using var position = new MemoryStream(new byte[6]);
        await using var texcoord = new MemoryStream(new byte[2]);
        await using var blend = new MemoryStream(new byte[4]);
        await using var output = new MemoryStream();

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => VertexBufferInterleaver.InterleaveAsync(
                position,
                positionStride: 3,
                texcoord,
                texcoordStride: 2,
                blend,
                blendStride: 2,
                output));

        Assert.Contains("Vertex counts differ", exception.Message, StringComparison.Ordinal);
    }
}
