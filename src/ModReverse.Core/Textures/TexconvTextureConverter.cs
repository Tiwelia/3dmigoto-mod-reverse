using System.Diagnostics;
using System.Globalization;

namespace ModReverse.Core.Textures;

public sealed class TexconvTextureConverter
    : ITextureConverter
{
    private readonly string _executablePath;

    public TexconvTextureConverter(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _executablePath = Path.GetFullPath(executablePath);
        if (!File.Exists(_executablePath))
        {
            throw new FileNotFoundException(
                "DirectXTex texconv.exe was not found.",
                _executablePath);
        }
    }

    public async Task ConvertAsync(
        string sourcePath,
        string destinationDirectory,
        TextureFileType fileType,
        float quality,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        if (quality is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quality),
                quality,
                "WIC quality must be between 0 and 1.");
        }

        var fullSourcePath = Path.GetFullPath(sourcePath);
        var fullDestinationDirectory = Path.GetFullPath(destinationDirectory);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException("DDS source file was not found.", fullSourcePath);
        }

        Directory.CreateDirectory(fullDestinationDirectory);
        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-ft");
        startInfo.ArgumentList.Add(fileType == TextureFileType.Png ? "PNG" : "JPG");
        if (fileType == TextureFileType.Jpg)
        {
            startInfo.ArgumentList.Add("-wicq");
            startInfo.ArgumentList.Add(quality.ToString("0.00", CultureInfo.InvariantCulture));
        }

        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(fullDestinationDirectory);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(fullSourcePath);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start texconv.exe.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidDataException(
                $"texconv failed for {fullSourcePath} with exit code {process.ExitCode}."
                + Environment.NewLine
                + standardOutput
                + standardError);
        }
    }
}
