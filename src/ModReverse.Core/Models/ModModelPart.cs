namespace ModReverse.Core.Models;

public sealed record ModModelPart(
    string Hash,
    string SourceSectionName,
    MigotoResource IndexBuffer,
    MigotoResource PositionBuffer,
    MigotoResource TexcoordBuffer,
    MigotoResource BlendBuffer,
    IReadOnlyList<DrawIndexedCall> DrawCalls);
