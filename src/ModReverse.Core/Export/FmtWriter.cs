using System.Globalization;
using System.Text;
using ModReverse.Core.Profiles;

namespace ModReverse.Core.Export;

public static class FmtWriter
{
    public static string Render(
        VertexLayout layout,
        string prefix,
        string indexFormat = "DXGI_FORMAT_R32_UINT")
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexFormat);

        var builder = new StringBuilder();
        AppendProperty(builder, "stride", layout.Stride.ToString(CultureInfo.InvariantCulture));
        AppendProperty(builder, "topology", "trianglelist");
        AppendProperty(builder, "format", indexFormat);
        AppendProperty(builder, "gametypename", layout.GameTypeName);
        AppendProperty(builder, "prefix", prefix);
        AppendProperty(builder, "scale", "1.0");
        AppendProperty(builder, "logic_name", string.Empty);
        AppendProperty(builder, "rotate_angle", "True");
        AppendProperty(builder, "rotate_angle_x", "0");
        AppendProperty(builder, "rotate_angle_y", "0");
        AppendProperty(builder, "rotate_angle_z", "0");
        AppendProperty(builder, "flip_face_orientation", "False");
        AppendProperty(builder, "apply_rotation_transformation", "False");

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
