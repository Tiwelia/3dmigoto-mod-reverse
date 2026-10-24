using System.Security.Cryptography;
using ModReverse.Core.Export;

namespace ModReverse.Tests.Export;

public sealed class WwmiModExporterTests
{
    private const string ShorekeeperRoot =
        @"E:\Blender\MOD Sample\MOD of Mine\鸣潮\SakuraMod\ShoreKeeper";

    [Fact]
    public async Task ExportAsyncMatchesAllReferenceShorekeeperComponents()
    {
        var iniPath = Path.Combine(ShorekeeperRoot, "Shorekeeper", "mod.ini");
        var referencePath = Path.Combine(
            ShorekeeperRoot,
            "Shorekeeper-mod-SingleModIniReverse");
        if (!File.Exists(iniPath) || !Directory.Exists(referencePath))
        {
            return;
        }

        var outputPath = Path.Combine(
            Path.GetTempPath(),
            "ModReverse.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var results = await WwmiModExporter.ExportAsync(iniPath, outputPath);

            Assert.Equal(32, results.Count);
            foreach (var result in results)
            {
                var relativeIb = Path.GetRelativePath(outputPath, result.IndexBufferPath);
                var relativeVb = Path.GetRelativePath(outputPath, result.VertexBufferPath);
                var referenceIb = Path.Combine(referencePath, relativeIb);
                var referenceVb = Path.Combine(referencePath, relativeVb);

                Assert.True(File.Exists(referenceIb), $"Missing reference file: {referenceIb}");
                Assert.True(File.Exists(referenceVb), $"Missing reference file: {referenceVb}");
                Assert.Equal(await HashAsync(referenceIb), await HashAsync(result.IndexBufferPath));
                Assert.Equal(await HashAsync(referenceVb), await HashAsync(result.VertexBufferPath));
            }
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    private static async Task<string> HashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash);
    }
}
