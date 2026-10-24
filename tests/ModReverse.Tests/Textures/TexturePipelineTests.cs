using ModReverse.Core.Textures;

namespace ModReverse.Tests.Textures;

public sealed class TexturePipelineTests
{
    private const string TexconvPath =
        @"E:\Game_Mod\WW\ShenShengMaoTou3-V3.3.9\current\Assets\texconv.exe";

    private const string SampleDdsPath =
        @"E:\Blender\MOD Sample\MOD of Mine\鸣潮\SakuraMod\Trigger-Mecha v1.0\Trigger-Mecha v1.0\0TriggerfaceADiffuse.dds";

    [Fact]
    public async Task ProcessAsyncPreservesDdsAndCreatesPngAndJpg()
    {
        if (!File.Exists(TexconvPath) || !File.Exists(SampleDdsPath))
        {
            return;
        }

        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "ModReverse.Tests",
            Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(temporaryRoot, "Input", "Nested");
        var outputDirectory = Path.Combine(temporaryRoot, "Output");
        Directory.CreateDirectory(sourceDirectory);
        var copiedSource = Path.Combine(sourceDirectory, "Face.DDS");

        try
        {
            File.Copy(SampleDdsPath, copiedSource);
            var pipeline = new TexturePipeline(
                new TexconvTextureConverter(TexconvPath));
            var result = await pipeline.ProcessAsync(
                Path.Combine(temporaryRoot, "Input"),
                outputDirectory,
                new TextureOutputOptions(
                    PreserveDds: true,
                    CreatePng: true,
                    CreateJpg: true));

            Assert.Equal(1, result.SourceTextureCount);
            Assert.Equal(1, result.DdsCopyCount);
            Assert.Equal(1, result.PngCount);
            Assert.Equal(1, result.JpgCount);
            Assert.True(File.Exists(
                Path.Combine(outputDirectory, "Textures", "DDS", "Nested", "Face.DDS")));
            Assert.True(File.Exists(
                Path.Combine(outputDirectory, "Textures", "PNG", "Nested", "Face.PNG")));
            Assert.True(File.Exists(
                Path.Combine(outputDirectory, "Textures", "JPG", "Nested", "Face.JPG")));
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }
}
