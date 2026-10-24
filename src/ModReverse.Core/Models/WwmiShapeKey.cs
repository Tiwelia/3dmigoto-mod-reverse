namespace ModReverse.Core.Models;

public sealed record WwmiShapeKey(
    int SemanticIndex,
    IReadOnlyDictionary<uint, byte[]> VertexOffsets);
