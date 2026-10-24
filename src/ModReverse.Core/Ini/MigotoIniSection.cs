namespace ModReverse.Core.Ini;

public sealed class MigotoIniSection
{
    private readonly List<MigotoIniLine> _lines = [];

    internal MigotoIniSection(string name, MigotoIniLine? header)
    {
        Name = name;
        Header = header;
    }

    public string Name { get; }

    public MigotoIniLine? Header { get; }

    public IReadOnlyList<MigotoIniLine> Lines => _lines;

    public IEnumerable<MigotoIniLine> Assignments =>
        _lines.Where(line => line.Kind == MigotoIniLineKind.Assignment);

    public IEnumerable<MigotoIniLine> GetAssignments(string key) =>
        Assignments.Where(
            line => string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase));

    public bool TryGetLastValue(string key, out string value)
    {
        var assignment = GetAssignments(key).LastOrDefault();
        if (assignment?.Value is null)
        {
            value = string.Empty;
            return false;
        }

        value = assignment.Value;
        return true;
    }

    internal void Add(MigotoIniLine line) => _lines.Add(line);
}
