namespace LeafVox.Core;

/// <summary>The file could not be parsed as a PDF at all.</summary>
public sealed class PdfCorruptException : LeafVoxException
{
    public PdfCorruptException(string path, Exception inner)
        : base($"'{Path.GetFileName(path)}' could not be read as a valid PDF.", inner) { }
}
