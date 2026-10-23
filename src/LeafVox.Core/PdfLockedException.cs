namespace LeafVox.Core;

/// <summary>The PDF is encrypted and a password is required (or the password was wrong).</summary>
public sealed class PdfLockedException : LeafVoxException
{
    public PdfLockedException(string path)
        : base($"'{Path.GetFileName(path)}' is password-protected or could not be opened with the supplied password.") { }
}
