using Avalonia.Media.Imaging;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LeafVox.App.ViewModels;

/// <summary>
/// One page in the continuous page viewer: display size in pixels, the rendered bitmap
/// (lazily produced by <see cref="Services.PageRenderer"/>) and the highlight overlay
/// boxes for the line currently being spoken on this page.
/// </summary>
public sealed partial class PageVm : ObservableObject
{
    public PageVm(int pageNumber, double widthPx, double heightPx)
    {
        PageNumber = pageNumber;
        DisplayWidth = widthPx;
        DisplayHeight = heightPx;
    }

    /// <summary>1-based page number in the document.</summary>
    public int PageNumber { get; }

    /// <summary>Displayed width in device pixels (PDF points × <c>PxPerPt</c>).</summary>
    public double DisplayWidth { get; }

    /// <summary>Displayed height in device pixels (PDF points × <c>PxPerPt</c>).</summary>
    public double DisplayHeight { get; }

    /// <summary>Pixel-space rectangles for this page's karaoke overlay.</summary>
    public ObservableCollection<HighlightBoxVm> HighlightBoxes { get; } = new();

    /// <summary>Rendered page bitmap, null until first realized near the viewport.</summary>
    [ObservableProperty]
    private Bitmap? _image;

    /// <summary>True once the bitmap has been rendered (render requests dedupe on this).</summary>
    [ObservableProperty]
    private bool _loaded;

    public string PageLabel => $"{PageNumber}";
}
