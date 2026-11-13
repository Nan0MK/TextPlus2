using System.Text;

namespace TextPlus2;

enum LinkTargetKind
{
    Web,
    Local,
    Anchor,
    Unsupported,
}

readonly record struct LinkTarget(LinkTargetKind Kind, string Path, string? Fragment);

readonly record struct MarkdownLink(int Start, int End, string Destination);

readonly record struct MarkdownHeading(string Text, string? Id);

/// <summary>
/// Single-line Markdown links: inline, reference, autolink, bare http(s)/mailto, and wiki [[targets]].
/// Fenced blocks and inline code are left alone. The document text is not rewritten.
/// </summary>
sealed class MarkdownIndex
{
    private readonly List<MarkdownLink> _links;

    private MarkdownIndex(List<MarkdownLink> links) => _links = links;

    public static MarkdownIndex Build(string text)
    {
        var lines = Split(text);
        var masks = new bool[lines.Count][];
        var skip = new bool[lines.Count];
        var refs = new Dictionary<string, string>(StringComparer.Ordinal);
        var links = new List<MarkdownLink>();
        var fence = false;
        var fenceMark = "";

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Text;
            var mask = CodeMask(line);
            masks[i] = mask;
            if (ToggleFence(line, ref fence, ref fenceMark))
            {
                skip[i] = true;
                continue;
            }

            if (fence)
            {
                skip[i] = true;
                continue;
            }

            if (TryDefinition(line, mask, out var key, out var destination, out var destStart, out var destLength))
            {
                refs[key] = destination;
                skip[i] = true;
                if (destLength > 0 && destination.Length > 0)
                    links.Add(new MarkdownLink(lines[i].Offset + destStart, lines[i].Offset + destStart + destLength, destination));
            }
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (!skip[i])
                ScanLine(lines[i].Text, lines[i].Offset, masks[i], refs, links);
        }

        links.Sort(static (a, b) => a.Start.CompareTo(b.Start));
        return new MarkdownIndex(links);
    }

    public MarkdownLink? At(int offset)
    {
        var lo = 0;
        var hi = _links.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            var link = _links[mid];
            if (offset < link.Start)
                hi = mid - 1;
            else if (offset >= link.End)
                lo = mid + 1;
            else
                return link;
        }

        return null;
    }

    public void Visit(int start, int end, Action<MarkdownLink> visit)
    {
        var lo = 0;
        var hi = _links.Count - 1;
        var first = _links.Count;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            if (_links[mid].End > start)
            {
                first = mid;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        for (var i = first; i < _links.Count && _links[i].Start < end; i++)
            visit(_links[i]);
    }

    public static bool IsMarkdownExtension(string extension) =>
        extension is ".md" or ".markdown" or ".mdown" or ".mkd";

    public static LinkTarget Classify(string destination)
    {
        var dest = UnescapeMarkdown(destination).Trim();
        if (dest.Length == 0)
            return new LinkTarget(LinkTargetKind.Unsupported, "", null);
        if (dest[0] == '#')
            return new LinkTarget(LinkTargetKind.Anchor, "", PercentDecode(dest[1..]));

        if (SchemeOf(dest) is { } scheme)
        {
            if (scheme is "http" or "https" or "mailto")
                return new LinkTarget(LinkTargetKind.Web, dest, null);
            if (scheme == "file" && Uri.TryCreate(dest, UriKind.Absolute, out var fileUri))
            {
                var fragment = fileUri.Fragment.Length > 1 ? PercentDecode(fileUri.Fragment[1..]) : null;
                return new LinkTarget(LinkTargetKind.Local, fileUri.LocalPath, fragment);
            }

            return new LinkTarget(LinkTargetKind.Unsupported, dest, null);
        }

        string? anchor = null;
        var hash = dest.IndexOf('#');
        if (hash >= 0)
        {
            anchor = PercentDecode(dest[(hash + 1)..]);
            dest = dest[..hash];
        }

        var query = dest.IndexOf('?');
        if (query >= 0)
            dest = dest[..query];
        dest = PercentDecode(dest);
        if (dest.Length == 0)
            return new LinkTarget(LinkTargetKind.Anchor, "", anchor);
        return new LinkTarget(LinkTargetKind.Local, dest, anchor);
    }

    public static int? FindHeading(string text, string fragment)
    {
        if (string.IsNullOrEmpty(fragment))
            return null;

        foreach (var (offset, line) in Split(text))
        {
            if (HeadingOf(line) is { } heading && HeadingMatches(heading, fragment))
                return offset;
        }

        return null;
    }

    public static bool HeadingMatches(MarkdownHeading heading, string fragment)
    {
        if (heading.Id != null && string.Equals(heading.Id, fragment, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(heading.Text, fragment, StringComparison.OrdinalIgnoreCase))
            return true;
        var slug = Slug(fragment);
        return slug.Length > 0 && slug == Slug(heading.Text);
    }

    public static MarkdownHeading? HeadingOf(string line)
    {
        var i = 0;
        while (i < line.Length && line[i] == ' ')
            i++;
        if (i > 3)
            return null;

        var hashes = 0;
        while (i < line.Length && line[i] == '#' && hashes < 7)
        {
            hashes++;
            i++;
        }

        if (hashes is < 1 or > 6 || i >= line.Length || line[i] != ' ')
            return null;

        var text = line[(i + 1)..].Trim();
        while (text.EndsWith('#'))
            text = text[..^1].TrimEnd();

        string? id = null;
        var mark = text.LastIndexOf("{#", StringComparison.Ordinal);
        if (mark >= 0 && text.EndsWith('}') && mark + 2 < text.Length - 1)
        {
            var raw = text[(mark + 2)..^1];
            if (raw.Length > 0 && raw.All(static c => char.IsLetterOrDigit(c) || c is '-' or '_'))
            {
                id = raw;
                text = text[..mark].TrimEnd();
            }
        }

        return text.Length == 0 ? null : new MarkdownHeading(text, id);
    }

    public static string Slug(string heading)
    {
        var builder = new StringBuilder(heading.Length);
        var dash = false;
        foreach (var c in heading.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                dash = false;
            }
            else if ((char.IsWhiteSpace(c) || c is '-' or '_') && builder.Length > 0 && !dash)
            {
                builder.Append('-');
                dash = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
            builder.Length--;
        return builder.ToString();
    }

    private static void ScanLine(string line, int lineOffset, bool[] mask, Dictionary<string, string> refs, List<MarkdownLink> links)
    {
        var i = 0;
        while (i < line.Length)
        {
            if (mask[i])
            {
                i++;
                continue;
            }

            if (line[i] == '\\' && i + 1 < line.Length)
            {
                i += 2;
                continue;
            }

            if (line[i] == '<')
            {
                var close = line.IndexOf('>', i + 1);
                if (close > i + 1)
                {
                    var inner = line[(i + 1)..close];
                    if (Autolink(inner) is { } destination)
                    {
                        links.Add(new MarkdownLink(lineOffset + i, lineOffset + close + 1, destination));
                        i = close + 1;
                        continue;
                    }
                }
            }

            if (TryWiki(line, i, mask, out var wiki))
            {
                links.Add(wiki.Shift(lineOffset));
                i = wiki.End;
                continue;
            }

            var bang = line[i] == '!' && i + 1 < line.Length && line[i + 1] == '[' && !mask[i + 1];
            var bracket = bang ? i + 1 : i;
            if (line[bracket] == '[')
            {
                if (TryLink(line, i, bracket, mask, refs, out var link))
                {
                    links.Add(link.Shift(lineOffset));
                    i = link.End;
                    continue;
                }
            }

            if (BareUrl(line, i) is { } url)
            {
                links.Add(new MarkdownLink(lineOffset + i, lineOffset + i + url.Length, url));
                i += url.Length;
                continue;
            }

            i++;
        }
    }

    private static bool TryWiki(string line, int spanStart, bool[] mask, out MarkdownLink link)
    {
        link = default;
        var open = spanStart;
        if (line[open] == '!')
        {
            if (open + 1 >= line.Length || mask[open + 1])
                return false;
            open++;
        }

        if (open + 1 >= line.Length || line[open] != '[' || line[open + 1] != '[' || mask[open] || mask[open + 1])
            return false;

        var target = new StringBuilder();
        var aliased = false;
        var i = open + 2;
        while (i < line.Length)
        {
            if (mask[i])
                return false;
            if (line[i] == '\\' && i + 1 < line.Length)
            {
                if (!aliased)
                    target.Append(line[i + 1]);
                i += 2;
                continue;
            }

            if (line[i] == ']' && i + 1 < line.Length && line[i + 1] == ']' && !mask[i + 1])
            {
                var destination = target.ToString().Trim();
                if (destination.Length == 0)
                    return false;
                link = new MarkdownLink(spanStart, i + 2, destination);
                return true;
            }

            if (line[i] == '|' && !aliased)
            {
                aliased = true;
                i++;
                continue;
            }

            if (!aliased)
                target.Append(line[i]);
            i++;
        }

        return false;
    }

    private static bool TryLink(string line, int spanStart, int bracket, bool[] mask, Dictionary<string, string> refs, out MarkdownLink link)
    {
        link = default;
        if (!TryWrapped(line, bracket, '[', ']', mask, out _, out _, out var afterLabel, out var label))
            return false;

        if (afterLabel < line.Length && line[afterLabel] == '(')
        {
            if (!TryInlineDestination(line, afterLabel, out var destination, out var end) || destination.Length == 0)
                return false;
            link = new MarkdownLink(spanStart, end, destination);
            return true;
        }

        if (afterLabel < line.Length && line[afterLabel] == '[')
        {
            if (!TryWrapped(line, afterLabel, '[', ']', mask, out _, out _, out var afterRef, out var reference))
                return false;
            var key = RefKey(reference.Length == 0 ? label : reference);
            if (!refs.TryGetValue(key, out var destination))
                return false;
            link = new MarkdownLink(spanStart, afterRef, destination);
            return true;
        }

        var shortcut = RefKey(label);
        if (shortcut.Length == 0 || !refs.TryGetValue(shortcut, out var target))
            return false;
        link = new MarkdownLink(spanStart, afterLabel, target);
        return true;
    }

    private static bool TryInlineDestination(string line, int open, out string destination, out int end)
    {
        destination = "";
        end = open;
        var i = open + 1;
        while (i < line.Length && line[i] is ' ' or '\t')
            i++;
        if (i >= line.Length)
            return false;

        int tokenEnd;
        if (line[i] == '<')
        {
            var close = line.IndexOf('>', i + 1);
            if (close < 0)
                return false;
            destination = line[(i + 1)..close];
            tokenEnd = close + 1;
        }
        else
        {
            var start = i;
            var depth = 0;
            while (i < line.Length)
            {
                if (line[i] == '\\' && i + 1 < line.Length)
                {
                    i += 2;
                    continue;
                }

                if (line[i] == '(')
                {
                    depth++;
                    i++;
                    continue;
                }

                if (line[i] == ')' && depth == 0)
                    break;
                if (line[i] == ')' && depth > 0)
                {
                    depth--;
                    i++;
                    continue;
                }

                if (depth == 0 && line[i] is ' ' or '\t')
                    break;
                i++;
            }

            if (i == start)
                return false;
            destination = line[start..i];
            tokenEnd = i;
        }

        i = tokenEnd;
        while (i < line.Length && line[i] is ' ' or '\t')
            i++;
        if (i < line.Length && line[i] is '"' or '\'' or '(')
        {
            var close = line[i] == '(' ? ')' : line[i];
            var title = line.IndexOf(close, i + 1);
            if (title < 0)
                return false;
            i = title + 1;
            while (i < line.Length && line[i] is ' ' or '\t')
                i++;
        }

        if (i >= line.Length || line[i] != ')')
            return false;
        end = i + 1;
        return true;
    }

    private static bool TryDefinition(string line, bool[] mask, out string key, out string destination, out int destStart, out int destLength)
    {
        key = "";
        destination = "";
        destStart = 0;
        destLength = 0;
        var i = 0;
        while (i < line.Length && line[i] == ' ')
            i++;
        if (i > 3 || i >= line.Length || line[i] != '[' || mask[i])
            return false;
        if (!TryWrapped(line, i, '[', ']', mask, out _, out _, out var after, out var label))
            return false;
        if (after + 1 >= line.Length || line[after] != ':' )
            return false;
        i = after + 1;
        while (i < line.Length && line[i] is ' ' or '\t')
            i++;
        if (i >= line.Length)
            return false;

        if (line[i] == '<')
        {
            var close = line.IndexOf('>', i + 1);
            if (close < 0)
                return false;
            destStart = i;
            destLength = close + 1 - i;
            destination = line[(i + 1)..close];
        }
        else
        {
            var start = i;
            while (i < line.Length && line[i] is not (' ' or '\t'))
                i++;
            destStart = start;
            destLength = i - start;
            destination = line[start..i];
        }

        key = RefKey(label);
        return key.Length > 0 && destination.Length > 0;
    }

    private static bool TryWrapped(string line, int open, char openChar, char closeChar, bool[] mask, out int contentStart, out int contentEnd, out int after, out string content)
    {
        contentStart = contentEnd = after = 0;
        content = "";
        if (open >= line.Length || line[open] != openChar)
            return false;

        var depth = 1;
        var i = open + 1;
        var builder = new StringBuilder();
        while (i < line.Length)
        {
            if (mask[i])
                return false;
            if (line[i] == '\\' && i + 1 < line.Length)
            {
                builder.Append(line[i + 1]);
                i += 2;
                continue;
            }

            if (line[i] == openChar)
                depth++;
            else if (line[i] == closeChar)
            {
                depth--;
                if (depth == 0)
                {
                    contentStart = open + 1;
                    contentEnd = i;
                    after = i + 1;
                    content = builder.ToString();
                    return true;
                }
            }

            builder.Append(line[i]);
            i++;
        }

        return false;
    }

    private static string? Autolink(string inner)
    {
        if (inner.Length == 0 || inner.IndexOfAny([' ', '<', '>']) >= 0)
            return null;
        if (inner.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || inner.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || inner.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            return inner;

        var at = inner.IndexOf('@');
        if (at <= 0 || inner.IndexOf('.', at + 1) < 0)
            return null;
        return "mailto:" + inner;
    }

    private static string? BareUrl(string line, int index)
    {
        if (index > 0 && (char.IsLetterOrDigit(line[index - 1]) || line[index - 1] == '_'))
            return null;

        var rest = line.AsSpan(index);
        var mark = rest.StartsWith("https://") ? 8
            : rest.StartsWith("http://") ? 7
            : rest.StartsWith("mailto:") ? 7
            : 0;
        if (mark == 0 || rest.Length == mark)
            return null;

        var end = index + mark;
        while (end < line.Length && !char.IsWhiteSpace(line[end]) && line[end] is not ('<' or '>'))
            end++;
        while (end > index + mark && ".,;:!?)]\"'".Contains(line[end - 1]))
            end--;
        return end > index + mark ? line[index..end] : null;
    }

    private static bool ToggleFence(string line, ref bool fence, ref string fenceMark)
    {
        var i = 0;
        while (i < line.Length && line[i] == ' ')
            i++;
        if (i > 3)
            return false;
        var mark = line.AsSpan(i).StartsWith("```") ? "```"
            : line.AsSpan(i).StartsWith("~~~") ? "~~~"
            : "";
        if (mark.Length == 0)
            return false;
        if (!fence)
        {
            fence = true;
            fenceMark = mark;
            return true;
        }

        if (line.AsSpan(i).StartsWith(fenceMark))
        {
            fence = false;
            fenceMark = "";
            return true;
        }

        return false;
    }

    private static bool[] CodeMask(string line)
    {
        var mask = new bool[line.Length];
        var i = 0;
        while (i < line.Length)
        {
            if (line[i] != '`')
            {
                i++;
                continue;
            }

            var n = 0;
            while (i + n < line.Length && line[i + n] == '`')
                n++;
            var close = IndexOfTicks(line, i + n, n);
            if (close < 0)
            {
                i += n;
                continue;
            }

            for (var k = i; k < close + n && k < mask.Length; k++)
                mask[k] = true;
            i = close + n;
        }

        return mask;
    }

    private static int IndexOfTicks(string line, int from, int count)
    {
        for (var i = from; i + count <= line.Length; i++)
        {
            var match = true;
            for (var n = 0; n < count; n++)
            {
                if (line[i + n] != '`')
                {
                    match = false;
                    break;
                }
            }

            if (!match)
                continue;
            if (i + count < line.Length && line[i + count] == '`')
                continue;
            return i;
        }

        return -1;
    }

    private static string RefKey(string label)
    {
        var builder = new StringBuilder(label.Length);
        var space = false;
        foreach (var c in label.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                space = builder.Length > 0;
                continue;
            }

            if (space)
            {
                builder.Append(' ');
                space = false;
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static bool IsMarkdownPunctuation(char c) =>
        c is '!' or '"' or '#' or '$' or '%' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or '-' or '.' or '/'
            or ':' or ';' or '<' or '=' or '>' or '?' or '@' or '[' or '\\' or ']' or '^' or '_' or '`' or '{' or '|' or '}' or '~';

    private static string? SchemeOf(string text)
    {
        if (text.Length < 2 || !char.IsLetter(text[0]))
            return null;
        var i = 1;
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '+' or '.' or '-'))
            i++;
        // A single letter and a colon is a Windows drive, not a scheme.
        if (i == 1 || i >= text.Length || text[i] != ':')
            return null;
        return text[..i].ToLowerInvariant();
    }

    private static string UnescapeMarkdown(string text)
    {
        if (text.IndexOf('\\') < 0)
            return text;
        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length && IsMarkdownPunctuation(text[i + 1]))
            {
                builder.Append(text[i + 1]);
                i++;
            }
            else
            {
                builder.Append(text[i]);
            }
        }

        return builder.ToString();
    }

    private static string PercentDecode(string text)
    {
        if (text.IndexOf('%') < 0)
            return text;
        try
        {
            return Uri.UnescapeDataString(text);
        }
        catch (UriFormatException)
        {
            return text;
        }
    }

    private static List<(int Offset, string Text)> Split(string text)
    {
        var lines = new List<(int, string)>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n' && text[i] != '\r')
                continue;
            lines.Add((start, text[start..i]));
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                i++;
            start = i + 1;
        }

        lines.Add((start, text[start..]));
        return lines;
    }
}

static class MarkdownLinkShift
{
    public static MarkdownLink Shift(this MarkdownLink link, int offset) =>
        new(link.Start + offset, link.End + offset, link.Destination);
}
