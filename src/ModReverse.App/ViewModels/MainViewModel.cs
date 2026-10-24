using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ModReverse.App.Commands;
using ModReverse.Core.Export;
using ModReverse.Core.Profiles;
using ModReverse.Core.Textures;

namespace ModReverse.App.ViewModels;

internal sealed class MainViewModel
    : INotifyPropertyChanged
{
    private readonly RelayCommand _browseIniCommand;
    private readonly RelayCommand _browseOutputCommand;
    private readonly AsyncRelayCommand _reverseSingleCommand;
    private readonly AsyncRelayCommand _reverseDrawIndexedCommand;
    private string _selectedGame = "WWMI";
    private string _iniPath = string.Empty;
    private string _outputPath = string.Empty;
    private bool _createPng = true;
    private bool _createJpg;
    private bool _isBusy;
    private bool _customOutput;
    private string _statusText = "等待输入";
    private string _logText = "请选择模型 INI。\r\n";

    public MainViewModel()
    {
        _browseIniCommand = new RelayCommand(BrowseIni, () => !IsBusy);
        _browseOutputCommand = new RelayCommand(BrowseOutput, () => !IsBusy);
        _reverseSingleCommand = new AsyncRelayCommand(
            () => ReverseAsync(drawIndexed: false),
            CanReverse);
        _reverseDrawIndexedCommand = new AsyncRelayCommand(
            () => ReverseAsync(drawIndexed: true),
            CanReverse);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> Games { get; } = ["WWMI", "ZZZ"];

    public string SelectedGame
    {
        get => _selectedGame;
        set => SetField(ref _selectedGame, value);
    }

    public string IniPath
    {
        get => _iniPath;
        private set
        {
            if (SetField(ref _iniPath, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string OutputPath
    {
        get => _outputPath;
        private set => SetField(ref _outputPath, value);
    }

    public bool CreatePng
    {
        get => _createPng;
        set => SetField(ref _createPng, value);
    }

    public bool CreateJpg
    {
        get => _createJpg;
        set => SetField(ref _createJpg, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetField(ref _logText, value);
    }

    public ICommand BrowseIniCommand => _browseIniCommand;

    public ICommand BrowseOutputCommand => _browseOutputCommand;

    public ICommand ReverseSingleCommand => _reverseSingleCommand;

    public ICommand ReverseDrawIndexedCommand => _reverseDrawIndexedCommand;

    private bool CanReverse() => !IsBusy && File.Exists(IniPath);

    private void BrowseIni()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 3Dmigoto / XXMI / WWMI 模型 INI",
            Filter = "INI 文件 (*.ini)|*.ini|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IniPath = dialog.FileName;
        _customOutput = false;
        OutputPath = CreateDefaultOutputPath(drawIndexed: false);
        LogText = $"已选择：{IniPath}\r\n";
        StatusText = "准备就绪";
    }

    private void BrowseOutput()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择逆向输出目录",
            Multiselect = false,
        };
        if (!string.IsNullOrWhiteSpace(OutputPath)
            && Directory.Exists(OutputPath))
        {
            dialog.InitialDirectory = OutputPath;
        }

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        OutputPath = dialog.FolderName;
        _customOutput = true;
    }

    private async Task ReverseAsync(bool drawIndexed)
    {
        IsBusy = true;
        StatusText = "正在逆向";
        if (!_customOutput)
        {
            OutputPath = CreateDefaultOutputPath(drawIndexed);
        }

        AppendLog(drawIndexed ? "开始 DrawIndexed 分支逆向。" : "开始普通单模逆向。");
        AppendLog($"游戏格式：{SelectedGame}");
        AppendLog($"输出目录：{OutputPath}");

        try
        {
            IReadOnlyList<ExportedModel> models;
            if (string.Equals(SelectedGame, "WWMI", StringComparison.OrdinalIgnoreCase))
            {
                models = await WwmiModExporter.ExportAsync(IniPath, OutputPath);
            }
            else if (drawIndexed)
            {
                models = await new DrawIndexedModExporter(new ZzzGameProfile())
                    .ExportAsync(IniPath, OutputPath);
            }
            else
            {
                models = await new SingleModExporter(new ZzzGameProfile())
                    .ExportAsync(IniPath, OutputPath);
            }

            AppendLog($"模型完成：{models.Count} 套 IB/VB/FMT。");
            var texturePipeline = new TexturePipeline(
                new TexconvTextureConverter(LocateTexconv()));
            var textureResult = await texturePipeline.ProcessAsync(
                Path.GetDirectoryName(IniPath)!,
                OutputPath,
                new TextureOutputOptions(
                    PreserveDds: true,
                    CreatePng: CreatePng,
                    CreateJpg: CreateJpg));
            AppendLog(
                $"贴图完成：DDS {textureResult.DdsCopyCount}，"
                + $"PNG {textureResult.PngCount}，JPG {textureResult.JpgCount}。");
            StatusText = "完成";
            MessageBox.Show(
                $"逆向完成。\n\n模型：{models.Count} 套\n"
                + $"DDS：{textureResult.DdsCopyCount}\n"
                + $"PNG：{textureResult.PngCount}\n"
                + $"JPG：{textureResult.JpgCount}",
                "3Dmigoto Mod Reverse",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            StatusText = "失败";
            AppendLog($"失败：{exception.Message}");
            MessageBox.Show(
                exception.Message,
                "逆向失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string CreateDefaultOutputPath(bool drawIndexed)
    {
        if (string.IsNullOrWhiteSpace(IniPath))
        {
            return string.Empty;
        }

        var iniDirectory = Path.GetDirectoryName(IniPath)!;
        var parent = Path.GetDirectoryName(iniDirectory)!;
        var directoryName = Path.GetFileName(iniDirectory);
        var suffix = drawIndexed
            ? "-DrawIndexedBasedToggleModIniReverse"
            : "-SingleModIniReverse";
        return Path.Combine(parent, directoryName + suffix);
    }

    private static string LocateTexconv()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "tools",
            "DirectXTex",
            "texconv.exe");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "未找到随程序发布的 texconv.exe。",
                path);
        }

        return path;
    }

    private void AppendLog(string message) =>
        LogText += $"[{DateTime.Now:HH:mm:ss}] {message}\r\n";

    private void RaiseCommandStates()
    {
        _browseIniCommand.RaiseCanExecuteChanged();
        _browseOutputCommand.RaiseCanExecuteChanged();
        _reverseSingleCommand.RaiseCanExecuteChanged();
        _reverseDrawIndexedCommand.RaiseCanExecuteChanged();
    }

    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
