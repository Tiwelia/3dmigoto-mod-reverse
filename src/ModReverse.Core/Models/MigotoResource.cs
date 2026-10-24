namespace ModReverse.Core.Models;

public sealed record MigotoResource(
    string Name,
    string FileName,
    int? Stride,
    string? Type,
    string? Format);
