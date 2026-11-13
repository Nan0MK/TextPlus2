using System.Buffers;
using System.Text;

namespace TextPlus2;

/// <summary>
/// Caret stops for Ctrl+arrow. Right lands on a word end, left on a word start.
/// CamelCase and digits are their own stops. Underscores are separators, like spaces.
/// </summary>
static class CaretMoves
{
    public static int Word(string text, int offset, bool forward)
    {
        if (text.Length == 0)
            return 0;
        offset = Math.Clamp(offset, 0, text.Length);
        return forward ? Forward(text, offset) : Backward(text, offset);
    }

    public static int Paragraph(string text, int offset, bool forward)
    {
        offset = Math.Clamp(offset, 0, text.Length);
        var lines = Split(text);
        if (lines.Count == 0)
            return 0;

        var index = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Offset <= offset)
                index = i;
            else
                break;
        }

        if (forward)
        {
            if (!Blank(lines[index].Text))
            {
                while (index + 1 < lines.Count && !Blank(lines[index + 1].Text))
                    index++;
                if (index + 1 >= lines.Count)
                    return text.Length;
                index++;
            }

            while (index < lines.Count && Blank(lines[index].Text))
                index++;
            return index >= lines.Count ? text.Length : lines[index].Offset;
        }

        if (Blank(lines[index].Text))
        {
            while (index > 0 && Blank(lines[index].Text))
                index--;
            if (Blank(lines[index].Text))
                return 0;
            return lines[ParagraphStart(lines, index)].Offset;
        }

        var start = ParagraphStart(lines, index);
        if (offset != lines[start].Offset)
            return lines[start].Offset;

        var prev = start - 1;
        while (prev > 0 && Blank(lines[prev].Text))
            prev--;
        if (prev < 0 || Blank(lines[prev].Text))
            return 0;
        return lines[ParagraphStart(lines, prev)].Offset;
    }

    private static int Forward(string text, int offset)
    {
        if (offset >= text.Length || !TryRune(text, offset, out var rune, out _))
            return text.Length;

        var kind = ClassOf(rune);
        if (kind is Class.Space or Class.Underscore)
        {
            offset = SkipForward(text, offset, Class.Space, Class.Underscore);
            if (offset >= text.Length || !TryRune(text, offset, out rune, out _))
                return text.Length;
            kind = ClassOf(rune);
        }

        return kind == Class.Word ? SubwordEnd(text, offset) : SkipForward(text, offset, Class.Other);
    }

    private static int Backward(string text, int offset)
    {
        if (offset <= 0 || !TryPrev(text, offset, out var rune, out _))
            return 0;

        var kind = ClassOf(rune);
        if (kind is Class.Space or Class.Underscore)
        {
            offset = SkipBackward(text, offset, Class.Space, Class.Underscore);
            if (offset <= 0 || !TryPrev(text, offset, out rune, out _))
                return 0;
            kind = ClassOf(rune);
        }

        return kind == Class.Word ? SubwordStart(text, offset) : SkipBackward(text, offset, Class.Other);
    }

    private static int SubwordEnd(string text, int offset)
    {
        if (!TryRune(text, offset, out var first, out var next))
            return text.Length;

        var kind = CaseOf(first);
        if (kind == Case.Digit)
            return SkipCase(text, offset, Case.Digit);
        if (kind == Case.OtherLetter)
            return SkipCase(text, offset, Case.OtherLetter);
        if (kind == Case.Lower)
            return SkipCase(text, offset, Case.Lower);

        var afterFirst = next;
        var end = next;
        while (TryRune(text, end, out var rune, out var after) && CaseOf(rune) == Case.Upper)
            end = after;

        if (end > afterFirst && TryRune(text, end, out var following, out _) && CaseOf(following) == Case.Lower)
            return PrevIndex(text, end);

        if (end != afterFirst)
            return end;

        var lower = end;
        while (TryRune(text, lower, out var rune, out var after) && CaseOf(rune) == Case.Lower)
            lower = after;
        return lower;
    }

    private static int SubwordStart(string text, int offset)
    {
        if (!TryPrev(text, offset, out var last, out var start))
            return 0;

        var kind = CaseOf(last);
        if (kind == Case.Digit)
        {
            while (TryPrev(text, start, out var rune, out var prev) && CaseOf(rune) == Case.Digit)
                start = prev;
            return start;
        }

        if (kind is Case.Lower or Case.OtherLetter)
        {
            while (TryPrev(text, start, out var rune, out var prev))
            {
                var previous = CaseOf(rune);
                if (previous == kind)
                {
                    start = prev;
                    continue;
                }

                if (previous == Case.Upper && kind == Case.Lower)
                    return prev;
                break;
            }

            return start;
        }

        if (TryRune(text, offset, out var after, out _) && CaseOf(after) == Case.Lower)
            return start;

        while (TryPrev(text, start, out var rune, out var prev) && CaseOf(rune) == Case.Upper)
            start = prev;
        return start;
    }

    private static int SkipCase(string text, int offset, Case kind)
    {
        while (TryRune(text, offset, out var rune, out var next) && CaseOf(rune) == kind)
            offset = next;
        return offset;
    }

    private static int SkipForward(string text, int offset, Class a, Class b = Class.Other)
    {
        while (TryRune(text, offset, out var rune, out var next) && Matches(ClassOf(rune), a, b))
            offset = next;
        return offset;
    }

    private static int SkipBackward(string text, int offset, Class a, Class b = Class.Other)
    {
        while (TryPrev(text, offset, out var rune, out var start) && Matches(ClassOf(rune), a, b))
            offset = start;
        return offset;
    }

    private static bool Matches(Class kind, Class a, Class b) => kind == a || (b != Class.Other && kind == b) || (a == Class.Other && kind == Class.Other);

    private static int PrevIndex(string text, int offset) =>
        TryPrev(text, offset, out _, out var start) ? start : 0;

    private static int ParagraphStart(List<(int Offset, string Text)> lines, int index)
    {
        while (index > 0 && !Blank(lines[index].Text) && !Blank(lines[index - 1].Text))
            index--;
        return index;
    }

    private static bool Blank(string text) => string.IsNullOrWhiteSpace(text);

    private static List<(int Offset, string Text)> Split(string text)
    {
        var lines = new List<(int, string)>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\r' && text[i] != '\n')
                continue;
            lines.Add((start, text[start..i]));
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                i++;
            start = i + 1;
        }

        lines.Add((start, text[start..]));
        return lines;
    }

    private static bool TryRune(string text, int index, out Rune rune, out int next)
    {
        if ((uint)index >= (uint)text.Length)
        {
            rune = default;
            next = index;
            return false;
        }

        var status = Rune.DecodeFromUtf16(text.AsSpan(index), out rune, out var consumed);
        if (status != OperationStatus.Done || consumed <= 0)
        {
            rune = new Rune(text[index]);
            next = index + 1;
            return true;
        }

        next = index + consumed;
        return true;
    }

    private static bool TryPrev(string text, int index, out Rune rune, out int start)
    {
        if (index <= 0)
        {
            rune = default;
            start = 0;
            return false;
        }

        start = index - 1;
        if (char.IsLowSurrogate(text[start]) && start > 0 && char.IsHighSurrogate(text[start - 1]))
            start--;
        return TryRune(text, start, out rune, out _);
    }

    private enum Class
    {
        Word,
        Space,
        Underscore,
        Other,
    }

    private enum Case
    {
        Upper,
        Lower,
        Digit,
        OtherLetter,
    }

    private static Class ClassOf(Rune rune)
    {
        if (rune.Value == '_')
            return Class.Underscore;
        if (Rune.IsWhiteSpace(rune))
            return Class.Space;
        if (Rune.IsLetterOrDigit(rune))
            return Class.Word;
        return Class.Other;
    }

    private static Case CaseOf(Rune rune)
    {
        if (Rune.IsDigit(rune))
            return Case.Digit;
        if (Rune.IsUpper(rune))
            return Case.Upper;
        if (Rune.IsLower(rune))
            return Case.Lower;
        return Case.OtherLetter;
    }
}
