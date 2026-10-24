using System.Buffers.Binary;
using ModReverse.Core.Export;

namespace ModReverse.Tests.Export;

public sealed class IndexBufferSlicerTests
{
    [Fact]
    public async Task SliceAsyncRebasesIndicesAndReportsContiguousVertexRange()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "ModReverse.Tests",
            Guid.NewGuid().ToString("N") + ".ib");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = new byte[6 * sizeof(uint)];
        uint[] indices = [99, 88, 10, 12, 11, 12];
        for (var index = 0; index < indices.Length; index++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(
                bytes.AsSpan(index * sizeof(uint), sizeof(uint)),
                indices[index]);
        }

        try
        {
            await File.WriteAllBytesAsync(path, bytes);
            var result = await IndexBufferSlicer.SliceAsync(
                path,
                "DXGI_FORMAT_R32_UINT",
                indexCount: 4,
                startIndex: 2,
                baseVertex: 5);

            Assert.Equal(15, result.FirstVertex);
            Assert.Equal(3, result.VertexCount);
            Assert.Equal("DXGI_FORMAT_R32_UINT", result.Format);
            Assert.Equal(
                new uint[] { 0, 2, 1, 2 },
                ReadUInt32(result.RebasedIndices));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static uint[] ReadUInt32(byte[] bytes)
    {
        var result = new uint[bytes.Length / sizeof(uint)];
        for (var index = 0; index < result.Length; index++)
        {
            result[index] = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.AsSpan(index * sizeof(uint), sizeof(uint)));
        }

        return result;
    }
}
