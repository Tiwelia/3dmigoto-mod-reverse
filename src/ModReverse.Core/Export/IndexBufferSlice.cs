namespace ModReverse.Core.Export;

public sealed record IndexBufferSlice(
    byte[] RebasedIndices,
    long FirstVertex,
    long VertexCount,
    string Format);
