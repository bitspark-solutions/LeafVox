using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafVox.Tts;

namespace LeafVox.App.ViewModels;

/// <summary>Browse + download the free voice catalog.</summary>
public sealed partial class VoiceManagerViewModel : ObservableObject
{
    private readonly VoiceRepository _repository;
    private readonly Action<string>? _setActiveVoice;

    public ObservableCollection<VoiceItemVm> Voices { get; } = new();

    [ObservableProperty]
    private string _status = "All voices are free and run 100% offline.";

    public VoiceManagerViewModel(VoiceRepository repository, Action<string>? setActiveVoice = null)
    {
        _repository = repository;
        _setActiveVoice = setActiveVoice;
        foreach (var v in _repository.List())
            Voices.Add(new VoiceItemVm(v, _setActiveVoice));
    }

    [RelayCommand]
    private void Refresh()
    {
        _repository.Refresh();
        foreach (var vm in Voices)
        {
            var fresh = _repository.Get(vm.Info.Id);
            if (fresh is not null) vm.Refresh(fresh);
        }
    }
}
