namespace LeafVox.Core;

/// <summary>
/// Splits page text into speakable sentences. Abbreviation-aware, decimal-safe,
/// un-hyphenates words broken across visual lines.
/// </summary>
public static class SentenceBuilder
{
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "mr","mrs","ms","dr","prof","st","vs","etc","i.e","e.g","approx","inc","ltd","co","corp",
        "jr","sr","dept","univ","fig","eq","pp","vol","no","nos","ph","d","mt","sg","tbl","est",
        "jan","feb","mar","apr","jun","jul","aug","sep","oct","nov","dec","mon","tue","wed","thu",
        "fri","sat","sun","ave","blvd","rd","mt","sq","fl","mi","ga","ca","ny","tx","wa","dc",
        "a.m","p.m","am","pm","pm","edt","eds","trans","bib","repr","sec","par","cl","art","act",
    };

    /// <summary>Joins visual lines of a paragraph region into flowing text, fixing end-of-line hyphenation.</summary>
    public static string JoinLines(IReadOnlyList<string> lines)
    {
        var text = new System.Text.StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            var cur = lines[i];
            if (text.Length > 0)
            {
                bool hyphenated = cur.Length > 0 && char.IsLower(cur[0]);
                if (text[^1] == '-' && hyphenated && LooksLikeHyphenation(text, cur))
                {
                    text.Length--; // drop the hyphen, glue the word
                }
                else if (text[^1] != ' ')
                {
                    text.Append(' ');
                }
            }
            text.Append(cur);
        }
        return text.ToString();
    }

    private static bool LooksLikeHyphenation(System.Text.StringBuilder prevLine, string nextLine)
    {
        // "read-" + "ing" → join. But "well-" + "being" is also fine to join for speech;
        // only refuse when the hyphen is attached to a single letter (like "a-") or numeric.
        int start = prevLine.Length - 2;
        while (start >= 0 && (char.IsLetterOrDigit(prevLine[start]) || prevLine[start] == '-')) start--;
        var lastToken = prevLine.ToString(start + 1, prevLine.Length - 1 - start);
        if (lastToken.Length <= 1) return false;
        if (lastToken.Any(char.IsDigit)) return false;
        return nextLine.Length > 0 && char.IsLetter(nextLine[0]);
    }

    /// <summary>Splits flowing text into sentences (returns at least one item for non-empty input).</summary>
    public static IReadOnlyList<string> Split(string text)
    {
        var outList = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return outList;

        var buf = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            buf.Append(text[i]);
            char c = text[i];
            if (c != '.' && c != '!' && c != '?' && c != '…') continue;

            // Collect the token that ends here.
            int t = buf.Length - 2;
            if (t < 0) continue;
            int tokenStart = t;
            while (tokenStart >= 0 && !char.IsWhiteSpace(buf[tokenStart])) tokenStart--;
            var token = buf.ToString(tokenStart + 1, Math.Max(0, t - tokenStart)).TrimEnd('.', '!', '?', '…');
            if (EndsSentence(text, i, token))
            {
                // consume trailing closers and whitespace
                int j = i + 1;
                while (j < text.Length && (text[j] == '"' || text[j] == '\'' || text[j] == ')')) j++;
                buf.Append(text, i + 1, j - (i + 1));
                var sentence = buf.ToString().Trim();
                if (sentence.Length > 0) outList.Add(sentence);
                buf.Clear();
                i = j - 1;
                if (i + 1 < text.Length && text[i + 1] == ' ') i++;
            }
        }

        var tail = buf.ToString().Trim();
        if (tail.Length > 0) outList.Add(tail);

        // Merge absurdly short fragments into their predecessor.
        var merged = new List<string>();
        foreach (var s in outList)
        {
            if (merged.Count > 0 && s.Length < 3) merged[^1] += " " + s;
            else merged.Add(s);
        }
        return merged;
    }

    private static bool EndsSentence(string text, int dotIndex, string token)
    {
        int next = dotIndex + 1;
        while (next < text.Length && (text[next] == '"' || text[next] == '\'' || text[next] == ')')) next++;
        if (next >= text.Length) return true; // end of page text

        char after = text[next];
        bool spaceBeforeAfter = text[next - 1] == ' ';

        // decimal / version / file-like: 3.5, v1.2, www.x.com
        if (char.IsDigit(text[next]) && dotIndex > 0 && char.IsDigit(text[dotIndex - 1]) && !spaceBeforeAfter) return false;
        if (token.Length == 0) return false;
        if (token.Contains('/') || token.Contains("www.") || token.EndsWith(".com", StringComparison.OrdinalIgnoreCase)) return false;

        // initials: "J. Smith"
        if (token.Length == 1 && char.IsUpper(token[0]) && after != ' ') return false;

        // abbreviations never end a sentence ("Dr.", "p.m.", "e.g.")
        var bare = token.Replace(".", "").ToLowerInvariant();
        if (Abbreviations.Contains(bare) || Abbreviations.Contains(token.ToLowerInvariant())) return false;

        // sentence boundary needs: whitespace then capital/quote/digit-start
        if (!char.IsWhiteSpace(after)) return false;
        char start = NextWordStart(text, next);
        if (char.IsLower(start))
        {
            // lower-case continuation only allowed if terminator was ! ? …
            return text[dotIndex] != '.';
        }
        return true;
    }

    private static char NextWordStart(string text, int from)
    {
        for (int k = from; k < text.Length; k++)
            if (!char.IsWhiteSpace(text[k])) return text[k];
        return '\0';
    }
}
