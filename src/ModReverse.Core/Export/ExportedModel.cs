namespace ModReverse.Core.Export;

public sealed record ExportedModel(
    string Hash,
    string GameTypeName,
    long VertexCount,
    string IndexBufferPath,
    string VertexBufferPath,
    string FormatPath);
