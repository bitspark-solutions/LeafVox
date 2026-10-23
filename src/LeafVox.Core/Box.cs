namespace LeafVox.Core;

/// <summary>A rectangle in PDF points (origin bottom-left, Y grows upward).</summary>
public sealed record Box(double Left, double Bottom, double Width, double Height)
{
    public double Top => Bottom + Height;
    public double Right => Left + Width;

    public bool ContainsY(double y) => y >= Bottom - 0.5 && y <= Top + 0.5;
    public bool ContainsX(double x) => x >= Left - 0.5 && x <= Right + 0.5;
}
