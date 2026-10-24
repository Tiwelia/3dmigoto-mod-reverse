namespace ModReverse.Cli;

internal sealed class CommandLineOptions
{
    private readonly Dictionary<string, string?> _values =
        new(StringComparer.OrdinalIgnoreCase);

    private CommandLineOptions()
    {
    }

    public static CommandLineOptions Parse(IEnumerable<string> arguments)
    {
        var result = new CommandLineOptions();
        var tokens = arguments.ToArray();
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"无法识别的参数：{token}");
            }

            var name = token[2..];
            if (name.Length == 0)
            {
                throw new ArgumentException("参数名不能为空。");
            }

            string? value = null;
            if (index + 1 < tokens.Length
                && !tokens[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = tokens[++index];
            }

            result._values[name] = value;
        }

        return result;
    }

    public bool HasFlag(string name) =>
        _values.TryGetValue(name, out var value) && value is null;

    public string? GetValue(string name) =>
        _values.TryGetValue(name, out var value) ? value : null;

    public string RequireValue(string name)
    {
        if (!_values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"缺少必需参数：--{name} <值>");
        }

        return value;
    }
}
