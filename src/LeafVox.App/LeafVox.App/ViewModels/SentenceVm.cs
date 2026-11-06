using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LeafVox.Core;

namespace LeafVox.App.ViewModels;

/// <summary>One sentence in the continuous reader flow; carries the karaoke highlight brush.</summary>
public sealed partial class SentenceVm : ObservableObject
{
    [ObservableProperty]
    private bool _isCurrent;

    public SentenceVm(Sentence sentence)
    {
        Index = sentence.Index;
        Text = sentence.Text;
        PageNumber = sentence.PageNumber;
    }

    public int Index { get; }
    public string Text { get; }
    public int PageNumber { get; }

    public string PageLabel => $"p.{PageNumber}";

    /// <summary>Background brush applied when this sentence is the one being spoken.</summary>
    public IBrush? CurrentBackground { get; private set; }

    partial void OnIsCurrentChanged(bool value)
    {
        CurrentBackground = value
            ? new SolidColorBrush(Color.Parse("#2F6FE0"))
            : null;
    }
}
