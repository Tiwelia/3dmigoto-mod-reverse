namespace ModReverse.Core.Models;

public sealed record WwmiModel(
    string Hash,
    MigotoResource IndexBuffer,
    MigotoResource PositionBuffer,
    MigotoResource VectorBuffer,
    MigotoResource ColorBuffer,
    MigotoResource BlendBuffer,
    MigotoResource TexcoordBuffer,
    MigotoResource? ShapeKeyOffsetBuffer,
    MigotoResource? ShapeKeyVertexIdBuffer,
    MigotoResource? ShapeKeyVertexOffsetBuffer,
    IReadOnlyList<WwmiComponent> Components);
