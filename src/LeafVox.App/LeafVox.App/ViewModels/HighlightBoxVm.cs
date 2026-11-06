namespace LeafVox.App.ViewModels;

/// <summary>A highlight rectangle in page-image pixel space (canvas units).</summary>
public sealed class HighlightBoxVm
{
    public HighlightBoxVm(double left, double top, double width, double height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public double Left { get; }
    public double Top { get; }
    public double Width { get; }
    public double Height { get; }
}
