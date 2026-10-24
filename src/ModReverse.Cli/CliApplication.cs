using ModReverse.Core.Analysis;
using ModReverse.Core.Export;
using ModReverse.Core.Ini;
using ModReverse.Core.Profiles;
using ModReverse.Core.Textures;

namespace ModReverse.Cli;

internal static class CliApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            var command = args[0].ToLowerInvariant();
            var options = CommandLineOptions.Parse(args[1..]);
            return command switch
            {
                "inspect" => await InspectAsync(options, cancellation.Token),
                "single" => await ExportAsync(
                    options,
                    drawIndexed: false,
                    cancellation.Token),
                "drawindexed" => await ExportAsync(
                    options,
                    drawIndexed: true,
                    cancellation.Token),
                _ => UnknownCommand(args[0]),
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("操作已取消。");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"失败：{exception.Message}");
            return 1;
        }
    }

    private static async Task<int> InspectAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken)
    {
        var iniPath = options.RequireValue("ini");
        var document = await MigotoIniParser.ParseFileAsync(
            iniPath,
            cancellationToken);
        var parts = MigotoModAnalyzer.Analyze(document);

        Console.WriteLine($"INI：{document.SourcePath}");
        Console.WriteLine($"编码：{document.EncodingName}");
        Console.WriteLine($"区段：{document.Sections.Count}");
        Console.WriteLine($"模型资源组：{parts.Count}");
        foreach (var part in parts)
        {
            Console.WriteLine();
            Console.WriteLine($"[{part.Hash}] {part.SourceSectionName}");
            Console.WriteLine(
                $"  Position: {part.PositionBuffer.FileName} "
                + $"(stride {part.PositionBuffer.Stride})");
            Console.WriteLine(
                $"  Texcoord: {part.TexcoordBuffer.FileName} "
                + $"(stride {part.TexcoordBuffer.Stride})");
            Console.WriteLine(
                $"  Blend:    {part.BlendBuffer.FileName} "
                + $"(stride {part.BlendBuffer.Stride})");
            Console.WriteLine($"  IB:       {part.IndexBuffer.FileName}");
            Console.WriteLine($"  DrawIndexed: {part.DrawCalls.Count}");
        }

        return parts.Count > 0 ? 0 : 1;
    }

    private static async Task<int> ExportAsync(
        CommandLineOptions options,
        bool drawIndexed,
        CancellationToken cancellationToken)
    {
        var iniPath = Path.GetFullPath(options.RequireValue("ini"));
        var outputPath = Path.GetFullPath(
            options.GetValue("output")
            ?? CreateDefaultOutputPath(iniPath, drawIndexed));
        var gameName = (options.GetValue("game") ?? "ZZZ").ToUpperInvariant();
        if (gameName is not ("ZZZ" or "WWMI"))
        {
            throw new NotSupportedException(
                $"当前里程碑尚未启用游戏格式“{gameName}”。目前可用：ZZZ、WWMI。");
        }

        var overwrite = options.HasFlag("overwrite");

        Console.WriteLine(drawIndexed ? "模式：DrawIndexed 分支" : "模式：普通单模");
        Console.WriteLine($"游戏：{gameName}");
        Console.WriteLine($"输入：{iniPath}");
        Console.WriteLine($"输出：{outputPath}");

        IReadOnlyList<ExportedModel> models;
        if (gameName == "WWMI")
        {
            models = await WwmiModExporter.ExportAsync(
                iniPath,
                outputPath,
                overwrite,
                cancellationToken);
        }
        else if (drawIndexed)
        {
            var exporter = new DrawIndexedModExporter(new ZzzGameProfile());
            models = await exporter.ExportAsync(
                iniPath,
                outputPath,
                overwrite,
                cancellationToken);
        }
        else
        {
            var exporter = new SingleModExporter(new ZzzGameProfile());
            models = await exporter.ExportAsync(
                iniPath,
                outputPath,
                overwrite,
                cancellationToken);
        }

        Console.WriteLine($"模型输出：{models.Count} 套 IB/VB/FMT");

        if (!options.HasFlag("no-textures"))
        {
            var texconvPath = options.GetValue("texconv") ?? LocateTexconv();
            var texturePipeline = new TexturePipeline(
                new TexconvTextureConverter(texconvPath));
            var textureResult = await texturePipeline.ProcessAsync(
                Path.GetDirectoryName(iniPath)!,
                outputPath,
                new TextureOutputOptions(
                    PreserveDds: true,
                    CreatePng: true,
                    CreateJpg: options.HasFlag("jpg")),
                cancellationToken);
            Console.WriteLine(
                $"贴图输出：DDS {textureResult.DdsCopyCount}，"
                + $"PNG {textureResult.PngCount}，JPG {textureResult.JpgCount}");
        }

        Console.WriteLine("完成。");
        return 0;
    }

    private static string CreateDefaultOutputPath(string iniPath, bool drawIndexed)
    {
        var directory = Path.GetDirectoryName(iniPath)!;
        var name = Path.GetFileName(directory);
        var parent = Path.GetDirectoryName(directory)!;
        var suffix = drawIndexed
            ? "-DrawIndexedBasedToggleModIniReverse"
            : "-SingleModIniReverse";
        return Path.Combine(parent, name + suffix);
    }

    private static string LocateTexconv()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "tools",
                "DirectXTex",
                "texconv.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "未找到 tools\\DirectXTex\\texconv.exe；请使用 --texconv 指定路径。");
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"未知命令：{command}");
        PrintHelp();
        return 1;
    }

    private static bool IsHelp(string value) =>
        value is "-h" or "--help" or "help";

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            3Dmigoto Mod Reverse

            用法：
              ModReverse.Cli inspect --ini <文件>
              ModReverse.Cli single --game ZZZ --ini <文件> [--output <目录>]
              ModReverse.Cli drawindexed --game ZZZ --ini <文件> [--output <目录>]

            选项：
              --overwrite       允许写入非空输出目录
              --no-textures     不处理贴图
              --jpg             除 DDS 和 PNG 外再生成 JPG
              --texconv <路径>  手动指定 texconv.exe
            """);
    }
}
