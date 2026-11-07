using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LeafVox.Core;

namespace LeafVox.App.ViewModels;

/// <summary>One sentence in the continuous reader flow; carries the karaoke highlight brushes.</summary>
public sealed partial class SentenceVm : ObservableObject
{
    private static readonly IBrush AccentWash = new SolidColorBrush(Color.Parse("#26FF7A50"));
    private static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#FF7A50"));

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

    /// <summary>Row tint applied when this sentence is the one being spoken (accent wash only — no line color for text).</summary>
    public IBrush? CurrentBackground { get; private set; }

    /// <summary>2px annotation marker at the row's left edge; invisible (null) when not speaking.</summary>
    public IBrush? MarkerBrush { get; private set; }

    partial void OnIsCurrentChanged(bool value)
    {
        CurrentBackground = value ? AccentWash : null;
        MarkerBrush = value ? Accent : null;
        OnPropertyChanged(nameof(CurrentBackground));
        OnPropertyChanged(nameof(MarkerBrush));
    }
}
