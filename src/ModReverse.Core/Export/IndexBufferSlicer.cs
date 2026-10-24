using System.Buffers.Binary;

namespace ModReverse.Core.Export;

public static class IndexBufferSlicer
{
    public static async Task<IndexBufferSlice> SliceAsync(
        string path,
        string? declaredFormat,
        int indexCount,
        int startIndex,
        int baseVertex,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

        var (format, indexWidth) = NormalizeFormat(declaredFormat);
        var byteCount = checked(indexCount * indexWidth);
        var byteOffset = checked((long)startIndex * indexWidth);
        var bytes = new byte[byteCount];

        await using (var stream = File.OpenRead(path))
        {
            if (byteOffset > stream.Length - byteCount)
            {
                throw new InvalidDataException(
                    $"DrawIndexed range [{startIndex}, {startIndex + indexCount}) "
                    + $"exceeds index buffer length {stream.Length / indexWidth}.");
            }

            stream.Position = byteOffset;
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        }

        long minimum = long.MaxValue;
        long maximum = long.MinValue;
        for (var index = 0; index < indexCount; index++)
        {
            var rawIndex = ReadIndex(bytes, index * indexWidth, indexWidth);
            var actualIndex = checked((long)rawIndex + baseVertex);
            if (actualIndex < 0)
            {
                throw new InvalidDataException(
                    $"Index {rawIndex} with base vertex {baseVertex} is negative.");
            }

            minimum = Math.Min(minimum, actualIndex);
            maximum = Math.Max(maximum, actualIndex);
        }

        for (var index = 0; index < indexCount; index++)
        {
            var rawIndex = ReadIndex(bytes, index * indexWidth, indexWidth);
            var rebased = checked((long)rawIndex + baseVertex - minimum);
            WriteIndex(bytes, index * indexWidth, indexWidth, rebased);
        }

        return new IndexBufferSlice(
            bytes,
            minimum,
            checked(maximum - minimum + 1),
            format);
    }

    private static (string Format, int Width) NormalizeFormat(string? declaredFormat)
    {
        if (declaredFormat?.Contains("R16_UINT", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ("DXGI_FORMAT_R16_UINT", 2);
        }

        return ("DXGI_FORMAT_R32_UINT", 4);
    }

    private static uint ReadIndex(byte[] bytes, int offset, int width) =>
        width == 2
            ? BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, width))
            : BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, width));

    private static void WriteIndex(byte[] bytes, int offset, int width, long value)
    {
        if (width == 2)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(
                bytes.AsSpan(offset, width),
                checked((ushort)value));
            return;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(offset, width),
            checked((uint)value));
    }
}
