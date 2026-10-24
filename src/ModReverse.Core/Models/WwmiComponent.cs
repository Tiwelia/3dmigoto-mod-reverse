namespace ModReverse.Core.Models;

public sealed record WwmiComponent(
    int ComponentIndex,
    string SourceSectionName,
    IReadOnlyList<DrawIndexedCall> DrawCalls);
