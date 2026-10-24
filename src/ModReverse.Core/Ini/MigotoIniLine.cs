namespace ModReverse.Core.Ini;

public sealed record MigotoIniLine
{
    private MigotoIniLine(
        int lineNumber,
        string rawText,
        MigotoIniLineKind kind,
        string? sectionName = null,
        string? key = null,
        string? value = null,
        string? directive = null,
        string? argument = null,
        string? inlineComment = null)
    {
        LineNumber = lineNumber;
        RawText = rawText;
        Kind = kind;
        SectionName = sectionName;
        Key = key;
        Value = value;
        Directive = directive;
        Argument = argument;
        InlineComment = inlineComment;
    }

    public int LineNumber { get; }

    public string RawText { get; }

    public MigotoIniLineKind Kind { get; }

    public string? SectionName { get; }

    public string? Key { get; }

    public string? Value { get; }

    public string? Directive { get; }

    public string? Argument { get; }

    public string? InlineComment { get; }

    internal static MigotoIniLine Blank(int lineNumber, string rawText) =>
        new(lineNumber, rawText, MigotoIniLineKind.Blank);

    internal static MigotoIniLine Comment(int lineNumber, string rawText) =>
        new(lineNumber, rawText, MigotoIniLineKind.Comment);

    internal static MigotoIniLine Section(int lineNumber, string rawText, string sectionName) =>
        new(lineNumber, rawText, MigotoIniLineKind.Section, sectionName: sectionName);

    internal static MigotoIniLine Assignment(
        int lineNumber,
        string rawText,
        string key,
        string value,
        string? inlineComment) =>
        new(
            lineNumber,
            rawText,
            MigotoIniLineKind.Assignment,
            key: key,
            value: value,
            inlineComment: inlineComment);

    internal static MigotoIniLine CreateDirective(
        int lineNumber,
        string rawText,
        string directive,
        string? argument) =>
        new(
            lineNumber,
            rawText,
            MigotoIniLineKind.Directive,
            directive: directive,
            argument: argument);
}
