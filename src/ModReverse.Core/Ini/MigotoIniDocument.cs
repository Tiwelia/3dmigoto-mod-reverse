namespace ModReverse.Core.Ini;

public sealed class MigotoIniDocument
{
    internal MigotoIniDocument(
        string? sourcePath,
        string encodingName,
        string preferredNewLine,
        IReadOnlyList<MigotoIniLine> lines,
        MigotoIniSection preamble,
        IReadOnlyList<MigotoIniSection> sections)
    {
        SourcePath = sourcePath;
        EncodingName = encodingName;
        PreferredNewLine = preferredNewLine;
        Lines = lines;
        Preamble = preamble;
        Sections = sections;
    }

    public string? SourcePath { get; }

    public string EncodingName { get; }

    public string PreferredNewLine { get; }

    public IReadOnlyList<MigotoIniLine> Lines { get; }

    public MigotoIniSection Preamble { get; }

    public IReadOnlyList<MigotoIniSection> Sections { get; }

    public IEnumerable<MigotoIniSection> FindSections(string name) =>
        Sections.Where(
            section => string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase));
}
