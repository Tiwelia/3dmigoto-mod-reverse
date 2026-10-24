using System.Globalization;
using ModReverse.Core.Ini;
using ModReverse.Core.Models;

namespace ModReverse.Core.Analysis;

public static class WwmiModAnalyzer
{
    private const string ComponentPrefix = "TextureOverrideComponent";

    public static WwmiModel Analyze(MigotoIniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var resources = MigotoModAnalyzer.FindResources(document);
        var bindings = document.Sections.FirstOrDefault(
            section => section.GetAssignments("ib").Any()
                && section.GetAssignments("vb0").Any()
                && section.GetAssignments("vb1").Any()
                && section.GetAssignments("vb2").Any()
                && section.GetAssignments("vb3").Any()
                && section.GetAssignments("vb4").Any())
            ?? throw new InvalidDataException(
                "WWMI shared resource binding command list was not found.");

        var indexBuffer = ResolveBinding(bindings, resources, "ib");
        var positionBuffer = ResolveBinding(bindings, resources, "vb0");
        var vectorBuffer = ResolveBinding(bindings, resources, "vb1");
        var texcoordBuffer = ResolveBinding(bindings, resources, "vb2");
        var colorBuffer = ResolveBinding(bindings, resources, "vb3");
        var blendBuffer = ResolveBinding(bindings, resources, "vb4");
        resources.TryGetValue("ResourceShapeKeyOffsetBuffer", out var shapeKeyOffsetBuffer);
        resources.TryGetValue("ResourceShapeKeyVertexIdBuffer", out var shapeKeyVertexIdBuffer);
        resources.TryGetValue(
            "ResourceShapeKeyVertexOffsetBuffer",
            out var shapeKeyVertexOffsetBuffer);

        var componentSections = document.Sections
            .Where(section => section.Name.StartsWith(
                ComponentPrefix,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (componentSections.Length == 0)
        {
            throw new InvalidDataException("WWMI component sections were not found.");
        }

        string? hash = null;
        var components = new List<WwmiComponent>(componentSections.Length);
        for (var ordinal = 0; ordinal < componentSections.Length; ordinal++)
        {
            var section = componentSections[ordinal];
            if (section.TryGetLastValue("hash", out var componentHash))
            {
                hash ??= componentHash.Trim().ToLowerInvariant();
            }

            var suffix = section.Name[ComponentPrefix.Length..];
            var componentIndex = int.TryParse(
                suffix,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedIndex)
                ? parsedIndex
                : ordinal;
            components.Add(
                new WwmiComponent(
                    componentIndex,
                    section.Name,
                    MigotoModAnalyzer.FindDrawCalls(document, section)));
        }

        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new InvalidDataException("WWMI component hash was not found.");
        }

        return new WwmiModel(
            hash,
            indexBuffer,
            positionBuffer,
            vectorBuffer,
            colorBuffer,
            blendBuffer,
            texcoordBuffer,
            shapeKeyOffsetBuffer,
            shapeKeyVertexIdBuffer,
            shapeKeyVertexOffsetBuffer,
            components);
    }

    private static MigotoResource ResolveBinding(
        MigotoIniSection section,
        IReadOnlyDictionary<string, MigotoResource> resources,
        string key)
    {
        foreach (var assignment in section.GetAssignments(key).Reverse())
        {
            var resourceName = RemoveReferencePrefix(assignment.Value);
            if (resources.TryGetValue(resourceName, out var resource))
            {
                return resource;
            }
        }

        throw new InvalidDataException(
            $"WWMI binding {key} does not reference a file-backed resource.");
    }

    private static string RemoveReferencePrefix(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.StartsWith("ref ", StringComparison.OrdinalIgnoreCase)
            ? trimmed[4..].Trim()
            : trimmed;
    }
}
