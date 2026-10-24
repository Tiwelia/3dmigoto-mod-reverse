namespace ModReverse.Core.Export;

public static class MultiBufferInterleaver
{
    private const int VerticesPerBlock = 16_384;

    public static async Task InterleaveRangeAsync(
        IReadOnlyList<VertexStreamInput> inputs,
        Stream output,
        long firstVertex,
        long vertexCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegative(firstVertex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
        if (inputs.Count == 0)
        {
            throw new ArgumentException("At least one vertex stream is required.", nameof(inputs));
        }

        long? totalVertexCount = null;
        foreach (var input in inputs)
        {
            ArgumentNullException.ThrowIfNull(input.Stream);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.Stride);
            if (!input.Stream.CanRead || !input.Stream.CanSeek)
            {
                throw new ArgumentException(
                    "Every vertex stream must be readable and seekable.",
                    nameof(inputs));
            }

            if (input.Stream.Length % input.Stride != 0)
            {
                throw new InvalidDataException(
                    $"Buffer length {input.Stream.Length} is not divisible by stride {input.Stride}.");
            }

            var count = input.Stream.Length / input.Stride;
            totalVertexCount ??= count;
            if (count != totalVertexCount)
            {
                throw new InvalidDataException(
                    $"Vertex stream counts differ: expected {totalVertexCount}, got {count}.");
            }
        }

        if (firstVertex > totalVertexCount - vertexCount)
        {
            throw new InvalidDataException(
                $"Requested vertex range [{firstVertex}, {firstVertex + vertexCount}) "
                + $"exceeds the available count {totalVertexCount}.");
        }

        var outputStride = inputs.Sum(input => input.Stride);
        var inputBuffers = inputs
            .Select(input => new byte[checked(input.Stride * VerticesPerBlock)])
            .ToArray();
        var outputBuffer = new byte[checked(outputStride * VerticesPerBlock)];
        foreach (var input in inputs)
        {
            input.Stream.Position = checked(firstVertex * input.Stride);
        }

        long processed = 0;
        while (processed < vertexCount)
        {
            var blockVertexCount = (int)Math.Min(
                VerticesPerBlock,
                vertexCount - processed);
            for (var inputIndex = 0; inputIndex < inputs.Count; inputIndex++)
            {
                var byteCount = checked(blockVertexCount * inputs[inputIndex].Stride);
                await inputs[inputIndex].Stream.ReadExactlyAsync(
                    inputBuffers[inputIndex].AsMemory(0, byteCount),
                    cancellationToken).ConfigureAwait(false);
            }

            for (var vertex = 0; vertex < blockVertexCount; vertex++)
            {
                var destinationOffset = vertex * outputStride;
                var elementOffset = 0;
                for (var inputIndex = 0; inputIndex < inputs.Count; inputIndex++)
                {
                    var stride = inputs[inputIndex].Stride;
                    inputBuffers[inputIndex].AsSpan(vertex * stride, stride)
                        .CopyTo(outputBuffer.AsSpan(destinationOffset + elementOffset, stride));
                    elementOffset += stride;
                }
            }

            await output.WriteAsync(
                outputBuffer.AsMemory(0, blockVertexCount * outputStride),
                cancellationToken).ConfigureAwait(false);
            processed += blockVertexCount;
        }
    }
}
