namespace LeafVox.Tts;

public class LeafVoxTtsException : Exception
{
    public LeafVoxTtsException(string message) : base(message) { }
    public LeafVoxTtsException(string message, Exception inner) : base(message, inner) { }
}
