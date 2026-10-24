using System.Buffers.Binary;
using ModReverse.Core.Models;

namespace ModReverse.Core.Analysis;

public static class WwmiShapeKeyReader
{
    private const int ShapeKeyOutputWidth = 6;
    private const int ShapeKeySourceStride = 12;

    public static async Task<IReadOnlyList<WwmiShapeKey>> ReadAsync(
        string offsetTablePath,
        string vertexIdPath,
        string vertexOffsetPath,
        CancellationToken cancellationToken = default)
    {
        var offsetTableBytes = await File.ReadAllBytesAsync(
            offsetTablePath,
            cancellationToken).ConfigureAwait(false);
        var vertexIdBytes = await File.ReadAllBytesAsync(
            vertexIdPath,
            cancellationToken).ConfigureAwait(false);
        var vertexOffsetBytes = await File.ReadAllBytesAsync(
            vertexOffsetPath,
            cancellationToken).ConfigureAwait(false);

        if (offsetTableBytes.Length % sizeof(uint) != 0
            || vertexIdBytes.Length % sizeof(uint) != 0
            || vertexOffsetBytes.Length % ShapeKeySourceStride != 0)
        {
            throw new InvalidDataException("A WWMI shape-key buffer has an invalid byte length.");
        }

        var entryCount = vertexIdBytes.Length / sizeof(uint);
        if (vertexOffsetBytes.Length / ShapeKeySourceStride != entryCount)
        {
            throw new InvalidDataException(
                "WWMI shape-key vertex ID and offset counts differ.");
        }

        var shapeKeyCount = offsetTableBytes.Length / sizeof(uint);
        var starts = new uint[shapeKeyCount];
        for (var index = 0; index < starts.Length; index++)
        {
            starts[index] = BinaryPrimitives.ReadUInt32LittleEndian(
                offsetTableBytes.AsSpan(index * sizeof(uint), sizeof(uint)));
        }

        var result = new List<WwmiShapeKey>();
        for (var shapeKeyIndex = 0; shapeKeyIndex < starts.Length; shapeKeyIndex++)
        {
            var start = starts[shapeKeyIndex];
            var end = shapeKeyIndex + 1 < starts.Length
                ? starts[shapeKeyIndex + 1]
                : (uint)entryCount;
            if (start > end || end > entryCount || start == end)
            {
                continue;
            }

            var offsets = new Dictionary<uint, byte[]>();
            for (var entry = start; entry < end; entry++)
            {
                var vertexId = BinaryPrimitives.ReadUInt32LittleEndian(
                    vertexIdBytes.AsSpan(
                        checked((int)entry * sizeof(uint)),
                        sizeof(uint)));
                offsets[vertexId] = vertexOffsetBytes.AsSpan(
                    checked((int)entry * ShapeKeySourceStride),
                    ShapeKeyOutputWidth).ToArray();
            }

            result.Add(new WwmiShapeKey(shapeKeyIndex, offsets));
        }

        return result;
    }
}
