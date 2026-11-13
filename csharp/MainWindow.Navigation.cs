using Avalonia.Input;
using AvaloniaEdit.Editing;

namespace TextPlus2;

public partial class MainWindow
{
    private bool TryArrowNavigation(KeyEventArgs e)
    {
        if (!Editor.TextArea.IsKeyboardFocusWithin || Editor.Document == null)
            return false;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            return false;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (!ctrl && !shift && !Editor.TextArea.Selection.IsEmpty)
        {
            if (e.Key == Key.Left)
            {
                PlaceCaret(Editor.SelectionStart, extend: false);
                e.Handled = true;
                return true;
            }

            if (e.Key == Key.Right)
            {
                PlaceCaret(Editor.SelectionStart + Editor.SelectionLength, extend: false);
                e.Handled = true;
                return true;
            }
        }

        if (!ctrl)
            return false;

        var text = Editor.Document.Text;
        var offset = Editor.CaretOffset;
        int? next = e.Key switch
        {
            Key.Left => CaretMoves.Word(text, offset, forward: false),
            Key.Right => CaretMoves.Word(text, offset, forward: true),
            Key.Up => CaretMoves.Paragraph(text, offset, forward: false),
            Key.Down => CaretMoves.Paragraph(text, offset, forward: true),
            _ => null,
        };
        if (next == null)
            return false;

        PlaceCaret(next.Value, shift);
        e.Handled = true;
        return true;
    }

    private void PlaceCaret(int offset, bool extend)
    {
        var document = Editor.Document;
        offset = Math.Clamp(offset, 0, document.TextLength);
        var area = Editor.TextArea;
        if (!extend)
        {
            area.ClearSelection();
            area.Caret.Offset = offset;
            area.Caret.BringCaretToView();
            return;
        }

        var anchor = area.Selection.IsEmpty
            ? area.Caret.Offset
            : area.Caret.Offset == Editor.SelectionStart
                ? Editor.SelectionStart + Editor.SelectionLength
                : Editor.SelectionStart;
        area.Caret.Offset = offset;
        area.Selection = Selection.Create(area, anchor, offset);
        area.Caret.BringCaretToView();
    }
}
