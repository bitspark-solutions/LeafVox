using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafVox.Tts;

namespace LeafVox.App.ViewModels;

/// <summary>One row in the voice manager: catalog entry + live install state.</summary>
public sealed partial class VoiceItemVm : ObservableObject
{
    [ObservableProperty]
    private string _stateLabel = "";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool _busy;

    private readonly Action<string>? _setActiveVoice;

    public VoiceItemVm(InstalledVoice voice, Action<string>? setActiveVoice = null)
    {
        Voice = voice;
        _setActiveVoice = setActiveVoice;
        Refresh(voice);
    }

    [RelayCommand]
    private void Use() => _setActiveVoice?.Invoke(Info.Id);

    public InstalledVoice Voice { get; private set; }
    public VoiceInfo Info => Voice.Voice;
    public string Title => Info.DisplayName;
    public string Language => Info.Language;
    public string TierLabel => Info.Tier == VoiceTier.Natural ? "natural" : "light";
    public string SizeLabel => $"{Info.ArchiveBytes / (1024 * 1024)} MB";
    public string LicenseLabel => Voice.LicenseNote ?? "license: see model card";
    public bool IsInstalled => Voice.State == InstallState.Installed;

    private bool CanInstall() => !Busy && !IsInstalled;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync(Action<string>? statusCallback = null)
    {
        Busy = true;
        StateLabel = "downloading…";
        Progress = 0;
        try
        {
            var downloader = new VoiceDownloader();
            var progress = new Progress<double>(p => Dispatcher.UIThread.Post(() => Progress = p * 100));
            var dir = await downloader.DownloadAsync(Info, progress);
            Voice = new InstalledVoice(Info, InstallState.Installed, dir, null);
            StateLabel = "installed ✓";
            statusCallback?.Invoke($"Installed {Info.DisplayName}.");
        }
        catch (Exception ex)
        {
            StateLabel = "failed: " + ex.Message;
            statusCallback?.Invoke(StateLabel);
        }
        finally
        {
            Busy = false;
            InstallCommand.NotifyCanExecuteChanged();
        }
    }

    public void Refresh(InstalledVoice fresh)
    {
        Voice = fresh;
        StateLabel = fresh.State switch
        {
            InstallState.Installed => "installed ✓",
            InstallState.Partial => "partial download — install again",
            _ => "not installed",
        };
        InstallCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsInstalled));
    }
}
