using System.Text;

namespace ModReverse.Core.Ini;

public static class MigotoIniParser
{
    private static readonly string[] ConditionalDirectives =
    [
        "else if",
        "endif",
        "else",
        "if",
    ];

    public static MigotoIniDocument Parse(
        string text,
        string? sourcePath = null,
        string encodingName = "utf-8")
    {
        ArgumentNullException.ThrowIfNull(text);

        var parsedLines = new List<MigotoIniLine>();
        var sections = new List<MigotoIniSection>();
        var preamble = new MigotoIniSection(string.Empty, header: null);
        var currentSection = preamble;
        var rawLines = SplitLines(text);

        for (var index = 0; index < rawLines.Count; index++)
        {
            var line = ParseLine(rawLines[index], index + 1);
            parsedLines.Add(line);

            if (line.Kind == MigotoIniLineKind.Section)
            {
                currentSection = new MigotoIniSection(line.SectionName!, line);
                sections.Add(currentSection);
                continue;
            }

            currentSection.Add(line);
        }

        return new MigotoIniDocument(
            sourcePath,
            encodingName,
            DetectPreferredNewLine(text),
            parsedLines,
            preamble,
            sections);
    }

    public static async Task<MigotoIniDocument> ParseFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var (text, encodingName) = Decode(bytes);
        return Parse(text, Path.GetFullPath(path), encodingName);
    }

    private static MigotoIniLine ParseLine(string rawText, int lineNumber)
    {
        var trimmed = rawText.Trim();
        if (trimmed.Length == 0)
        {
            return MigotoIniLine.Blank(lineNumber, rawText);
        }

        if (trimmed[0] is ';' or '#')
        {
            return MigotoIniLine.Comment(lineNumber, rawText);
        }

        if (TryParseSection(trimmed, out var sectionName))
        {
            return MigotoIniLine.Section(lineNumber, rawText, sectionName);
        }

        if (TryParseConditionalDirective(trimmed, out var directive, out var argument))
        {
            return MigotoIniLine.CreateDirective(lineNumber, rawText, directive, argument);
        }

        var equalsIndex = trimmed.IndexOf('=');
        if (equalsIndex > 0)
        {
            var key = trimmed[..equalsIndex].Trim();
            if (key.Length > 0)
            {
                var rawValue = trimmed[(equalsIndex + 1)..].Trim();
                var (value, inlineComment) = SplitInlineComment(rawValue);
                return MigotoIniLine.Assignment(
                    lineNumber,
                    rawText,
                    key,
                    value,
                    inlineComment);
            }
        }

        var firstWhitespace = IndexOfWhitespace(trimmed);
        if (firstWhitespace < 0)
        {
            return MigotoIniLine.CreateDirective(lineNumber, rawText, trimmed, argument: null);
        }

        return MigotoIniLine.CreateDirective(
            lineNumber,
            rawText,
            trimmed[..firstWhitespace],
            trimmed[(firstWhitespace + 1)..].Trim());
    }

    private static bool TryParseSection(string text, out string sectionName)
    {
        sectionName = string.Empty;
        if (text[0] != '[')
        {
            return false;
        }

        var closingBracket = text.IndexOf(']');
        if (closingBracket <= 1)
        {
            return false;
        }

        sectionName = text[1..closingBracket].Trim();
        return sectionName.Length > 0;
    }

    private static bool TryParseConditionalDirective(
        string text,
        out string directive,
        out string? argument)
    {
        foreach (var candidate in ConditionalDirectives)
        {
            if (text.Equals(candidate, StringComparison.OrdinalIgnoreCase))
            {
                directive = candidate;
                argument = null;
                return true;
            }

            if (text.StartsWith(candidate + " ", StringComparison.OrdinalIgnoreCase))
            {
                directive = candidate;
                argument = text[(candidate.Length + 1)..].Trim();
                return true;
            }
        }

        directive = string.Empty;
        argument = null;
        return false;
    }

    private static (string Value, string? InlineComment) SplitInlineComment(string value)
    {
        var quote = '\0';

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character is '"' or '\'')
            {
                if (quote == '\0')
                {
                    quote = character;
                }
                else if (quote == character)
                {
                    quote = '\0';
                }

                continue;
            }

            if (quote != '\0' || character is not (';' or '#'))
            {
                continue;
            }

            if (index == 0 || char.IsWhiteSpace(value[index - 1]))
            {
                return (value[..index].TrimEnd(), value[index..]);
            }
        }

        return (value, null);
    }

    private static int IndexOfWhitespace(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static List<string> SplitLines(string text)
    {
        var result = new List<string>();
        var start = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is not ('\r' or '\n'))
            {
                continue;
            }

            result.Add(text[start..index]);
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                index++;
            }

            start = index + 1;
        }

        if (start < text.Length)
        {
            result.Add(text[start..]);
        }
        else if (text.Length == 0 || text[^1] is '\r' or '\n')
        {
            result.Add(string.Empty);
        }

        return result;
    }

    private static string DetectPreferredNewLine(string text)
    {
        var crlf = text.IndexOf("\r\n", StringComparison.Ordinal);
        var lf = text.IndexOf('\n');
        var cr = text.IndexOf('\r');

        if (crlf >= 0 && (lf < 0 || crlf <= lf) && (cr < 0 || crlf <= cr))
        {
            return "\r\n";
        }

        if (lf >= 0 && (cr < 0 || lf < cr))
        {
            return "\n";
        }

        return cr >= 0 ? "\r" : Environment.NewLine;
    }

    private static (string Text, string EncodingName) Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Encoding.UTF8.Preamble))
        {
            return (Encoding.UTF8.GetString(bytes[Encoding.UTF8.Preamble.Length..]), "utf-8-bom");
        }

        if (bytes.StartsWith(Encoding.Unicode.Preamble))
        {
            return (
                Encoding.Unicode.GetString(bytes[Encoding.Unicode.Preamble.Length..]),
                Encoding.Unicode.WebName);
        }

        if (bytes.StartsWith(Encoding.BigEndianUnicode.Preamble))
        {
            return (
                Encoding.BigEndianUnicode.GetString(
                    bytes[Encoding.BigEndianUnicode.Preamble.Length..]),
                Encoding.BigEndianUnicode.WebName);
        }

        try
        {
            var strictUtf8 = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true);
            return (strictUtf8.GetString(bytes), strictUtf8.WebName);
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var simplifiedChinese = Encoding.GetEncoding(936);
            return (simplifiedChinese.GetString(bytes), simplifiedChinese.WebName);
        }
    }
}
