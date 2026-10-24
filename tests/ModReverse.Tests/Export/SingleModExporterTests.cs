using System.Security.Cryptography;
using ModReverse.Core.Export;
using ModReverse.Core.Profiles;

namespace ModReverse.Tests.Export;

public sealed class SingleModExporterTests
{
    private const string TriggerRoot =
        @"E:\Blender\MOD Sample\MOD of Mine\鸣潮\SakuraMod\Trigger-Mecha v1.0";

    [Fact]
    public async Task ExportAsyncMatchesReferenceIbAndVbBytesForTriggerMecha()
    {
        var iniPath = Path.Combine(TriggerRoot, "Trigger-Mecha v1.0", "10Trigger.ini");
        var referencePath = Path.Combine(
            TriggerRoot,
            "Trigger-Mecha v1.0-SingleModIniReverse");
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
            var exporter = new SingleModExporter(new ZzzGameProfile());
            var results = await exporter.ExportAsync(iniPath, outputPath);

            Assert.Equal(10, results.Count);
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
