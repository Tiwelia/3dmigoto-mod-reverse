namespace ModReverse.Core.Models;

public sealed record WwmiComposedVertexBuffer(
    byte[] Bytes,
    int Stride,
    IReadOnlyList<int> ShapeKeyIndices);
