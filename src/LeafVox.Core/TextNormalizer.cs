using System.Text;

namespace LeafVox.Core;

/// <summary>Cleans extracted PDF line text: whitespace runs, ligatures, control junk.</summary>
public static class TextNormalizer
{
    private static readonly Dictionary<char, string> Map = new()
    {
        ['\u00AD'] = "",   // soft hyphen: delete
        ['\u2028'] = " ", ['\u2029'] = " ", ['\n'] = " ", ['\r'] = " ", ['\t'] = " ",
        ['\uFB00'] = "ff", ['\uFB01'] = "fi", ['\uFB02'] = "fl",
        ['\uFB03'] = "ffi", ['\uFB04'] = "ffl", ['\uFB05'] = "st", ['\uFB06'] = "st",
        ['\u2013'] = "-", ['\u2014'] = "-", ['\u2010'] = "-", ['\u2011'] = "-",
        ['\u2018'] = "'", ['\u2019'] = "'", ['\u201A'] = "'", ['\u201B'] = "'",
        ['\u201C'] = "\"", ['\u201D'] = "\"", ['\u201E'] = "\"", ['\u201F'] = "\"",
        ['\u2026'] = "…",
    };

    public static string Normalize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var sb = new StringBuilder(raw.Length);
        char? prev = null;
        foreach (var ch in raw)
        {
            string piece = Map.TryGetValue(ch, out var mapped) ? mapped : ch.ToString();
            foreach (var c in piece)
            {
                if (c == ' ' && prev == ' ') continue;
                if (char.IsControl(c)) continue;
                sb.Append(c);
                prev = c;
            }
        }
        return sb.ToString().Trim();
    }
}
