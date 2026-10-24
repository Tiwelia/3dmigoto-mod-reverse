namespace ModReverse.Core.Export;

public static class VertexBufferInterleaver
{
    private const int DefaultVerticesPerBlock = 16_384;

    public static async Task<long> InterleaveAsync(
        Stream position,
        int positionStride,
        Stream texcoord,
        int texcoordStride,
        Stream blend,
        int blendStride,
        Stream output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(texcoord);
        ArgumentNullException.ThrowIfNull(blend);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(positionStride);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(texcoordStride);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blendStride);

        if (!position.CanRead || !texcoord.CanRead || !blend.CanRead)
        {
            throw new ArgumentException("All source streams must be readable.");
        }

        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", nameof(output));
        }

        var vertexCount = ValidateAndGetVertexCount(
            position,
            positionStride,
            texcoord,
            texcoordStride,
            blend,
            blendStride);
        position.Position = 0;
        texcoord.Position = 0;
        blend.Position = 0;
        await InterleaveCoreAsync(
            position,
            positionStride,
            texcoord,
            texcoordStride,
            blend,
            blendStride,
            output,
            vertexCount,
            cancellationToken).ConfigureAwait(false);
        return vertexCount;
    }

    public static async Task InterleaveRangeAsync(
        Stream position,
        int positionStride,
        Stream texcoord,
        int texcoordStride,
        Stream blend,
        int blendStride,
        Stream output,
        long firstVertex,
        long vertexCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(texcoord);
        ArgumentNullException.ThrowIfNull(blend);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(positionStride);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(texcoordStride);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blendStride);
        ArgumentOutOfRangeException.ThrowIfNegative(firstVertex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);

        var totalVertexCount = ValidateAndGetVertexCount(
            position,
            positionStride,
            texcoord,
            texcoordStride,
            blend,
            blendStride);
        if (firstVertex > totalVertexCount - vertexCount)
        {
            throw new InvalidDataException(
                $"Requested vertex range [{firstVertex}, {firstVertex + vertexCount}) "
                + $"exceeds the available count {totalVertexCount}.");
        }

        position.Position = checked(firstVertex * positionStride);
        texcoord.Position = checked(firstVertex * texcoordStride);
        blend.Position = checked(firstVertex * blendStride);
        await InterleaveCoreAsync(
            position,
            positionStride,
            texcoord,
            texcoordStride,
            blend,
            blendStride,
            output,
            vertexCount,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task InterleaveCoreAsync(
        Stream position,
        int positionStride,
        Stream texcoord,
        int texcoordStride,
        Stream blend,
        int blendStride,
        Stream output,
        long vertexCount,
        CancellationToken cancellationToken)
    {
        var outputStride = checked(positionStride + texcoordStride + blendStride);
        var positionBuffer = new byte[checked(positionStride * DefaultVerticesPerBlock)];
        var texcoordBuffer = new byte[checked(texcoordStride * DefaultVerticesPerBlock)];
        var blendBuffer = new byte[checked(blendStride * DefaultVerticesPerBlock)];
        var outputBuffer = new byte[checked(outputStride * DefaultVerticesPerBlock)];

        long processed = 0;
        while (processed < vertexCount)
        {
            var blockVertexCount = (int)Math.Min(
                DefaultVerticesPerBlock,
                vertexCount - processed);
            var positionBytes = checked(blockVertexCount * positionStride);
            var texcoordBytes = checked(blockVertexCount * texcoordStride);
            var blendBytes = checked(blockVertexCount * blendStride);
            var outputBytes = checked(blockVertexCount * outputStride);

            await position.ReadExactlyAsync(
                positionBuffer.AsMemory(0, positionBytes),
                cancellationToken).ConfigureAwait(false);
            await texcoord.ReadExactlyAsync(
                texcoordBuffer.AsMemory(0, texcoordBytes),
                cancellationToken).ConfigureAwait(false);
            await blend.ReadExactlyAsync(
                blendBuffer.AsMemory(0, blendBytes),
                cancellationToken).ConfigureAwait(false);

            for (var vertex = 0; vertex < blockVertexCount; vertex++)
            {
                var destinationOffset = vertex * outputStride;
                positionBuffer.AsSpan(vertex * positionStride, positionStride)
                    .CopyTo(outputBuffer.AsSpan(destinationOffset, positionStride));
                texcoordBuffer.AsSpan(vertex * texcoordStride, texcoordStride)
                    .CopyTo(outputBuffer.AsSpan(
                        destinationOffset + positionStride,
                        texcoordStride));
                blendBuffer.AsSpan(vertex * blendStride, blendStride)
                    .CopyTo(outputBuffer.AsSpan(
                        destinationOffset + positionStride + texcoordStride,
                        blendStride));
            }

            await output.WriteAsync(
                outputBuffer.AsMemory(0, outputBytes),
                cancellationToken).ConfigureAwait(false);
            processed += blockVertexCount;
        }

    }

    private static long ValidateAndGetVertexCount(
        Stream position,
        int positionStride,
        Stream texcoord,
        int texcoordStride,
        Stream blend,
        int blendStride)
    {
        if (!position.CanSeek || !texcoord.CanSeek || !blend.CanSeek)
        {
            throw new ArgumentException(
                "Source streams must support seeking so their vertex counts can be validated.");
        }

        var positionLength = position.Length;
        var texcoordLength = texcoord.Length;
        var blendLength = blend.Length;

        if (positionLength % positionStride != 0
            || texcoordLength % texcoordStride != 0
            || blendLength % blendStride != 0)
        {
            throw new InvalidDataException(
                "A source buffer length is not divisible by its declared stride.");
        }

        var positionCount = positionLength / positionStride;
        var texcoordCount = texcoordLength / texcoordStride;
        var blendCount = blendLength / blendStride;
        if (positionCount != texcoordCount || positionCount != blendCount)
        {
            throw new InvalidDataException(
                $"Vertex counts differ: position={positionCount}, "
                + $"texcoord={texcoordCount}, blend={blendCount}.");
        }

        return positionCount;
    }
}
