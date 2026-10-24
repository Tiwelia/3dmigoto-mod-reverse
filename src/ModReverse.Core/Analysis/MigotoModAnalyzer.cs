using System.Globalization;
using ModReverse.Core.Ini;
using ModReverse.Core.Models;

namespace ModReverse.Core.Analysis;

public static class MigotoModAnalyzer
{
    public static IReadOnlyList<ModModelPart> Analyze(MigotoIniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var resources = FindResources(document);
        var parts = new List<ModModelPart>();

        foreach (var section in document.Sections)
        {
            if (!section.TryGetLastValue("hash", out var hash))
            {
                continue;
            }

            foreach (var ibAssignment in section.GetAssignments("ib"))
            {
                var ibResourceName = ibAssignment.Value;
                if (string.IsNullOrWhiteSpace(ibResourceName)
                    || !resources.TryGetValue(ibResourceName, out var indexBuffer))
                {
                    continue;
                }

                var bufferSet = ResolveVertexBufferSet(indexBuffer, resources.Values);
                if (bufferSet is null)
                {
                    continue;
                }

                parts.Add(
                    new ModModelPart(
                        NormalizeHash(hash),
                        section.Name,
                        indexBuffer,
                        bufferSet.Value.Position,
                        bufferSet.Value.Texcoord,
                        bufferSet.Value.Blend,
                        FindDrawCalls(document, section)));
            }
        }

        return parts;
    }

    public static IReadOnlyDictionary<string, MigotoResource> FindResources(
        MigotoIniDocument document)
    {
        var resources = new Dictionary<string, MigotoResource>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var section in document.Sections)
        {
            if (!section.Name.StartsWith("Resource", StringComparison.OrdinalIgnoreCase)
                || !section.TryGetLastValue("filename", out var fileName))
            {
                continue;
            }

            int? stride = null;
            if (section.TryGetLastValue("stride", out var strideText)
                && int.TryParse(
                    strideText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedStride))
            {
                stride = parsedStride;
            }

            section.TryGetLastValue("type", out var type);
            section.TryGetLastValue("format", out var format);
            resources[section.Name] = new MigotoResource(
                section.Name,
                Unquote(fileName),
                stride,
                string.IsNullOrEmpty(type) ? null : type,
                string.IsNullOrEmpty(format) ? null : format);
        }

        return resources;
    }

    private static (
        MigotoResource Position,
        MigotoResource Texcoord,
        MigotoResource Blend)? ResolveVertexBufferSet(
        MigotoResource indexBuffer,
        IEnumerable<MigotoResource> resources)
    {
        var resourceList = resources.ToArray();
        var indexStem = RemoveSuffix(indexBuffer.Name, "IB");
        var baseName = resourceList
            .Where(resource => resource.Name.EndsWith("Position", StringComparison.OrdinalIgnoreCase))
            .Select(resource => RemoveSuffix(resource.Name, "Position"))
            .Where(candidate => indexStem.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.Length)
            .FirstOrDefault();

        if (baseName is null)
        {
            return null;
        }

        var position = FindResource(resourceList, baseName + "Position");
        var texcoord = FindResource(resourceList, baseName + "Texcoord");
        var blend = FindResource(resourceList, baseName + "Blend");
        if (position is null || texcoord is null || blend is null)
        {
            return null;
        }

        return (position, texcoord, blend);
    }

    public static IReadOnlyList<DrawIndexedCall> FindDrawCalls(
        MigotoIniDocument document,
        MigotoIniSection rootSection)
    {
        var result = new List<DrawIndexedCall>();
        var activeSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Expand(rootSection);
        return result;

        void Expand(MigotoIniSection section)
        {
            if (!activeSections.Add(section.Name))
            {
                return;
            }

            foreach (var line in section.Lines)
            {
                if (line.Kind != MigotoIniLineKind.Assignment)
                {
                    continue;
                }

                if (string.Equals(line.Key, "drawindexed", StringComparison.OrdinalIgnoreCase)
                    && TryParseDrawIndexed(line, out var drawCall))
                {
                    result.Add(drawCall);
                    continue;
                }

                if (!string.Equals(line.Key, "run", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(line.Value))
                {
                    continue;
                }

                foreach (var referencedSection in document.FindSections(line.Value))
                {
                    Expand(referencedSection);
                }
            }

            activeSections.Remove(section.Name);
        }
    }

    private static bool TryParseDrawIndexed(
        MigotoIniLine line,
        out DrawIndexedCall drawCall)
    {
        var parts = line.Value?.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts is null
            || parts.Length < 2
            || !int.TryParse(
                parts[0],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var indexCount)
            || !int.TryParse(
                parts[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var startIndex))
        {
            drawCall = null!;
            return false;
        }

        var baseVertex = 0;
        if (parts.Length >= 3
            && !int.TryParse(
                parts[2],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out baseVertex))
        {
            drawCall = null!;
            return false;
        }

        drawCall = new DrawIndexedCall(
            indexCount,
            startIndex,
            baseVertex,
            line.LineNumber);
        return true;
    }

    private static MigotoResource? FindResource(
        IEnumerable<MigotoResource> resources,
        string name) =>
        resources.FirstOrDefault(
            resource => string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase));

    private static string RemoveSuffix(string value, string suffix) =>
        value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? value[..^suffix.Length]
            : value;

    private static string NormalizeHash(string hash) =>
        hash.Trim().ToLowerInvariant();

    private static string Unquote(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2
            && ((trimmed[0] == '"' && trimmed[^1] == '"')
                || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }
}
