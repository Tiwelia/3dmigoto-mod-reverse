namespace ModReverse.Core.Profiles;

public sealed record VertexLayout(
    string GameTypeName,
    int Stride,
    IReadOnlyList<VertexElement> Elements);
