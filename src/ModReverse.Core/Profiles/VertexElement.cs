namespace ModReverse.Core.Profiles;

public sealed record VertexElement(
    string SemanticName,
    int SemanticIndex,
    string Format,
    int ByteWidth,
    int AlignedByteOffset);
