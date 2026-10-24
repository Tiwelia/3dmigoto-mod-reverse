using ModReverse.Core.Models;

namespace ModReverse.Core.Export;

public static class WwmiShapeKeyComposer
{
    private const int ShapeKeyWidth = 6;

    public static WwmiComposedVertexBuffer Compose(
        byte[] baseVertexBuffer,
        int baseStride,
        long firstVertex,
        long vertexCount,
        IReadOnlyList<WwmiShapeKey> shapeKeys)
    {
        ArgumentNullException.ThrowIfNull(baseVertexBuffer);
        ArgumentNullException.ThrowIfNull(shapeKeys);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseStride);
        ArgumentOutOfRangeException.ThrowIfNegative(firstVertex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
        if (baseVertexBuffer.LongLength != vertexCount * baseStride)
        {
            throw new InvalidDataException(
                $"Expected {vertexCount * baseStride} base vertex bytes, "
                + $"got {baseVertexBuffer.LongLength}.");
        }

        var lastVertex = checked(firstVertex + vertexCount - 1);
        var activeShapeKeys = shapeKeys
            .Where(shapeKey => shapeKey.VertexOffsets.Keys.Any(
                vertexId => vertexId >= firstVertex && vertexId <= lastVertex))
            .OrderBy(shapeKey => shapeKey.SemanticIndex)
            .ToArray();
        if (activeShapeKeys.Length == 0)
        {
            return new WwmiComposedVertexBuffer(
                baseVertexBuffer,
                baseStride,
                []);
        }

        var stride = checked(baseStride + activeShapeKeys.Length * ShapeKeyWidth);
        var result = new byte[checked((int)vertexCount * stride)];
        for (var vertex = 0; vertex < vertexCount; vertex++)
        {
            baseVertexBuffer.AsSpan(checked((int)vertex * baseStride), baseStride)
                .CopyTo(result.AsSpan(checked((int)vertex * stride), baseStride));
            var vertexId = checked((uint)(firstVertex + vertex));
            for (var shapeKeyOrdinal = 0;
                 shapeKeyOrdinal < activeShapeKeys.Length;
                 shapeKeyOrdinal++)
            {
                if (activeShapeKeys[shapeKeyOrdinal].VertexOffsets.TryGetValue(
                    vertexId,
                    out var offset))
                {
                    offset.CopyTo(
                        result,
                        checked((int)vertex * stride
                            + baseStride
                            + shapeKeyOrdinal * ShapeKeyWidth));
                }
            }
        }

        return new WwmiComposedVertexBuffer(
            result,
            stride,
            activeShapeKeys.Select(shapeKey => shapeKey.SemanticIndex).ToArray());
    }
}
