namespace LeafVox.Core;

/// <summary>Base exception for user-facing LeafVox failures.</summary>
public class LeafVoxException : Exception
{
    public LeafVoxException(string message) : base(message) { }
    public LeafVoxException(string message, Exception inner) : base(message, inner) { }
}
