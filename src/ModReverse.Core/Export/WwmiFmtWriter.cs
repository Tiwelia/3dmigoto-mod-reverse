using System.Globalization;
using System.Text;
using ModReverse.Core.Profiles;

namespace ModReverse.Core.Export;

public static class WwmiFmtWriter
{
    public static string Render(
        VertexLayout layout,
        string prefix,
        string indexFormat,
        IReadOnlyList<int> shapeKeyIndices)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFormat);
        ArgumentNullException.ThrowIfNull(shapeKeyIndices);

        var stride = checked(layout.Stride + shapeKeyIndices.Count * 6);
        var builder = new StringBuilder();
        AppendProperty(builder, "stride", stride.ToString(CultureInfo.InvariantCulture));
        AppendProperty(builder, "topology", "trianglelist");
        AppendProperty(builder, "format", indexFormat);
        AppendProperty(builder, "gametypename", string.Empty);
        AppendProperty(builder, "prefix", prefix);
        AppendProperty(builder, "scale", "0.01");
        AppendProperty(builder, "rotate_angle", "True");
        AppendProperty(builder, "rotate_angle_x", "0");
        AppendProperty(builder, "rotate_angle_y", "0");
        AppendProperty(builder, "rotate_angle_z", "180");
        AppendProperty(builder, "flip_face_orientation", "True");
        AppendProperty(builder, "apply_rotation_transformation", "True");

        for (var index = 0; index < layout.Elements.Count; index++)
        {
            var element = layout.Elements[index];
            builder.Append("element[")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .AppendLine("]:");
            AppendElementProperty(builder, "SemanticName", element.SemanticName);
            AppendElementProperty(
                builder,
                "SemanticIndex",
                element.SemanticIndex.ToString(CultureInfo.InvariantCulture));
            AppendElementProperty(builder, "Format", element.Format);
            AppendElementProperty(
                builder,
                "ByteWidth",
                element.ByteWidth.ToString(CultureInfo.InvariantCulture));
            AppendElementProperty(builder, "InputSlot", "0");
            AppendElementProperty(
                builder,
                "AlignedByteOffset",
                element.AlignedByteOffset.ToString(CultureInfo.InvariantCulture));
            AppendElementProperty(builder, "InputSlotClass", "per-vertex");
            AppendElementProperty(builder, "InstanceDataStepRate", "0");
        }

        for (var shapeKeyOrdinal = 0;
             shapeKeyOrdinal < shapeKeyIndices.Count;
             shapeKeyOrdinal++)
        {
            var elementIndex = layout.Elements.Count + shapeKeyOrdinal;
            builder.Append("element[")
                .Append(elementIndex.ToString(CultureInfo.InvariantCulture))
                .AppendLine("]:");
            AppendElementProperty(builder, "SemanticName", "SHAPEKEY");
            AppendElementProperty(
                builder,
                "SemanticIndex",
                shapeKeyIndices[shapeKeyOrdinal].ToString(CultureInfo.InvariantCulture));
            AppendElementProperty(builder, "Format", "R16G16B16_FLOAT");
            AppendElementProperty(builder, "ByteWidth", "6");
            AppendElementProperty(builder, "InputSlot", "0");
            AppendElementProperty(
                builder,
                "AlignedByteOffset",
                (layout.Stride + shapeKeyOrdinal * 6).ToString(
                    CultureInfo.InvariantCulture));
            AppendElementProperty(builder, "InputSlotClass", "per-vertex");
            AppendElementProperty(builder, "InstanceDataStepRate", "0");
        }

        return builder.ToString();
    }

    private static void AppendProperty(StringBuilder builder, string name, string value) =>
        builder.Append(name).Append(": ").AppendLine(value);

    private static void AppendElementProperty(
        StringBuilder builder,
        string name,
        string value) =>
        builder.Append("  ").Append(name).Append(": ").AppendLine(value);
}
