namespace ModReverse.Core.Models;

public sealed record DrawIndexedCall(
    int IndexCount,
    int StartIndex,
    int BaseVertex,
    int SourceLineNumber);
