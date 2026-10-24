namespace ModReverse.Core.Profiles;

public interface IGameProfile
{
    string Id { get; }

    IReadOnlyList<VertexLayout> GetVertexLayouts(
        int positionStride,
        int texcoordStride,
        int blendStride);
}
