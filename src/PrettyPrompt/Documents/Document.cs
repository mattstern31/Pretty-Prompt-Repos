﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using PrettyPrompt.Panes;
using PrettyPrompt.TextSelection;

namespace PrettyPrompt.Documents;

/// <summary>
/// A Document represents the input text being typed into the prompt.
/// It contains the text being typed, the caret/cursor positions, text selection, and word wrapping.
/// </summary>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
internal class Document : IEquatable<Document>
{
    private readonly StringBuilderWithCaret stringBuilder;
    private readonly UndoRedoHistory undoRedoHistory;

    /// <summary>
    /// Cached ToStringed current stringBuilder content. This is optimization for GetText calls so they don't repeatedly ToString underlying stringBuilder.
    /// </summary>
    private string currentText;

    /// <summary>
    /// The one-dimensional index of the text caret in the document text
    /// </summary>
    public int Caret
    {
        get => stringBuilder.Caret;
        set => stringBuilder.Caret = value;
    }

    public Document() : this(string.Empty, 0) { }
    public Document(string text, int caret)
    {
        this.stringBuilder = new StringBuilderWithCaret(text, caret);
        this.undoRedoHistory = new UndoRedoHistory(text, caret);
        this.currentText = text;
        stringBuilder.TextChanged += () => currentText = stringBuilder.ToString();
    }

    public void InsertAtCaret(CodePane codePane, char character)
    {
        using (BeginChanges(codePane))
        {
            if (codePane.GetSelectionSpan().TryGet(out var selectionValue))
            {
                codePane.Selection = null;
                stringBuilder.Remove(selectionValue);
            }
            stringBuilder.Insert(Caret, character);
        }
    }

    public void DeleteSelectedText(CodePane codePane)
    {
        using (BeginChanges(codePane))
        {
            if (codePane.GetSelectionSpan().TryGet(out var selectionValue))
            {
                codePane.Selection = null;
                stringBuilder.Remove(selectionValue);
            }
        }
    }

    public void InsertAtCaret(CodePane codePane, string text)
    {
        using (BeginChanges(codePane))
        {
            if (codePane.GetSelectionSpan().TryGet(out var selectionValue))
            {
                codePane.Selection = null;
                stringBuilder.Remove(selectionValue);
            }
            this.stringBuilder.Insert(Caret, text);
        }
    }

    /// <summary>
    /// Consume an async enumerable while live updating the Document at each iteration.
    /// The streaming input is entered as a single "block" of undo/redo history.
    /// </summary>
    public async IAsyncEnumerable<string> InsertAtCaretAsync(CodePane codePane, IAsyncEnumerable<string> streamingText)
    {
        using (BeginChanges(codePane, ChangeContextType.UndoRedoHistory))
        {
            if (codePane.GetSelectionSpan().TryGet(out var selectionValue))
            {
                codePane.Selection = null;
                stringBuilder.Remove(selectionValue);
            }
            await foreach (var token in streamingText.ConfigureAwait(false))
            {
                this.stringBuilder.Insert(Caret, token);
                yield return token;
            }
        }
    }

    public void Indent(CodePane codePane, TextSpan span, int direction)
    {
        using (BeginChanges(codePane))
        {
            var lines = currentText.Split('\n');
            int startLine = -1;
            int endLine = -1;
            int charCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                charCount += lines[i].Length + 1;
                if (startLine == -1 && span.Start < charCount) startLine = i;
                if (span.End <= charCount)
                {
                    endLine = i;
                    break;
                }
            }

            SelectionSpan s;
            stringBuilder.Clear();
            if (direction > 0)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i >= startLine && i <= endLine)
                    {
                        stringBuilder.Insert(stringBuilder.Caret, codePane.TabSpaces);
                        stringBuilder.Insert(stringBuilder.Caret, lines[i]);
                    }
                    else
                    {
                        stringBuilder.Insert(stringBuilder.Caret, lines[i]);
                    }
                    if (i < lines.Length - 1) stringBuilder.Insert(stringBuilder.Caret, '\n');
                }

                s = codePane.Selection!.Value;
                lines = stringBuilder.ToString().Split('\n');
                if (s.End.Column > 0)
                {
                    Debug.Assert(s.End.Row == endLine);
                    codePane.Selection = s.WithEnd(s.End.WithColumn(Math.Min(lines[endLine].Length, s.End.Column + codePane.TabSpaces.Length)));
                }
            }
            else
            {
                int removedCharsOnStartLine = 0;
                int removedCharsOnEndLine = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (i >= startLine && i <= endLine)
                    {
                        int whitespaces = 0;
                        for (int k = 0; k < line.Length; k++)
                        {
                            if (char.IsWhiteSpace(line[k])) ++whitespaces;
                            else break;
                        }

                        int charsToRemove = Math.Min(codePane.TabSpaces.Length, whitespaces);
                        stringBuilder.Insert(stringBuilder.Caret, line.AsSpan(charsToRemove));

                        if (i == startLine) removedCharsOnStartLine = charsToRemove;
                        if (i == endLine) removedCharsOnEndLine = charsToRemove;
                    }
                    else
                    {
                        stringBuilder.Insert(stringBuilder.Caret, line);
                    }
                    if (i < lines.Length - 1) stringBuilder.Insert(stringBuilder.Caret, '\n');
                }

                s = codePane.Selection!.Value;
                lines = stringBuilder.ToString().Split('\n');
                codePane.Selection = s
                    .WithStart(s.Start.WithColumn(Math.Max(0, s.Start.Column - removedCharsOnStartLine)))
                    .WithEnd(s.End.WithColumn(Math.Max(0, s.End.Column - removedCharsOnEndLine)));
            }

            s = codePane.Selection!.Value;
            Caret =
                s.Direction == SelectionDirection.FromLeftToRight ?
                s.End.ToCaret(lines) :
                s.Start.ToCaret(lines);
        }
    }

    public void Remove(CodePane codePane, TextSpan span) => Remove(codePane, span.Start, span.Length);

    public void Remove(CodePane codePane, int startIndex, int length)
    {
        if (startIndex >= stringBuilder.Length || startIndex < 0) return;

        using (BeginChanges(codePane))
        {
            stringBuilder.Remove(startIndex, length);
        }
    }

    public void Clear(CodePane codePane)
    {
        using (BeginChanges(codePane))
        {
            stringBuilder.Clear();
        }
    }

    public void SetContents(CodePane codePane, string contents, int caret)
    {
        using (BeginChanges(codePane))
        {
            stringBuilder.SetContents(contents, caret);
        }
    }

    public WordWrappedText WrapEditableCharacters(int width)
        => WordWrapping.WrapEditableCharacters(stringBuilder, Caret, width);

    public void MoveToWordBoundary(int direction) =>
        Caret = CalculateWordBoundaryIndexNearCaret(direction);

    public int CalculateWordBoundaryIndexNearCaret(int direction)
    {
        if (direction == 0) throw new ArgumentOutOfRangeException(nameof(direction), "cannot be 0");
        direction = Math.Sign(direction);

        if (direction > 0)
        {
            for (var i = Caret; i < stringBuilder.Length - 1; i++)
            {
                if (IsWordBoundary(i, i + 1))
                    return i + 1;
            }
            return stringBuilder.Length;
        }
        else
        {
            for (var i = Math.Min(Caret, stringBuilder.Length) - 1; i > 0; i--)
            {
                if (IsWordBoundary(i - 1, i))
                    return i;
            }
            return 0;
        }

        bool IsWordBoundary(int index1, int index2)
        {
            if (index2 >= stringBuilder.Length) return false;

            var c1 = stringBuilder[index1];
            var c2 = stringBuilder[index2];

            var isWhitespace1 = char.IsWhiteSpace(c1);
            var isWhitespace2 = char.IsWhiteSpace(c2);
            if (isWhitespace1 && !isWhitespace2) return true;
            if (isWhitespace1 || isWhitespace2) return false;

            return char.IsLetterOrDigit(c1) != char.IsLetterOrDigit(c2);
        }
    }

    public void MoveToLineBoundary(int direction)
    {
        Debug.Assert(direction is -1 or 1);

        Caret = CalculateLineBoundaryIndexNearCaret(direction, smartHome: true);
    }

    public int CalculateLineBoundaryIndexNearCaret(int direction, bool smartHome)
        => CalculateLineBoundaryIndexNearCaret(Caret, direction, smartHome);

    private int CalculateLineBoundaryIndexNearCaret(int caret, int direction, bool smartHome)
    {
        if (stringBuilder.Length == 0) return caret;

        if (direction > 0)
        {
            for (var i = caret; i < stringBuilder.Length; i++)
            {
                if (stringBuilder[i] == '\n') return i;
            }
            return stringBuilder.Length;
        }
        else
        {
            if (caret == 0 && !smartHome) return 0;

            int lineStart = 0;
            var beforeCaretIndex = (caret - 1).Clamp(0, Length - 1);
            for (int i = beforeCaretIndex; i >= 0; i--)
            {
                if (stringBuilder[i] == '\n')
                {
                    lineStart = Math.Min(i + 1, Length);
                    break;
                }
            }

            if (!smartHome) return lineStart;

            //smart Home implementation (repeating Home presses switch between 'non-white-space start of line' and 'start of line')
            int lineStartNonWhiteSpace = lineStart;
            for (int i = lineStart; i < Length; i++)
            {
                var c = stringBuilder[i];
                if (c == '\n')
                {
                    return lineStart;
                }
                if (!char.IsWhiteSpace(c))
                {
                    lineStartNonWhiteSpace = i;
                    break;
                }
            }

            return lineStartNonWhiteSpace == beforeCaretIndex + 1 ? lineStart : lineStartNonWhiteSpace;
        }
    }

    public void Undo(out SelectionSpan? selection)
    {
        var record = undoRedoHistory.Undo();
        selection = record.Selection;
        stringBuilder.SetContents(record.Text);
        Caret = record.Caret;
    }

    public void Redo(out SelectionSpan? selection)
    {
        var record = undoRedoHistory.Redo();
        selection = record.Selection;
        stringBuilder.SetContents(record.Text);
        Caret = record.Caret;
    }

    public void ClearUndoRedoHistory() => undoRedoHistory.Clear();

    public event Action? Changed
    {
        add => stringBuilder.Changed += value;
        remove => stringBuilder.Changed -= value;
    }

    /*
     * The following methods are forwarding along the StringBuilder APIs.
     */
    public char this[int index] => currentText[index];
    public int Length => currentText.Length;
    public string GetText() => currentText;
    public ReadOnlySpan<char> GetText(TextSpan span) => currentText.AsSpan(span);
    public override bool Equals(object? obj) => Equals(obj as Document);
    public bool Equals(Document? other) => other != null && other.currentText.Equals(currentText);
    public override int GetHashCode() => currentText.GetHashCode();
    private string GetDebuggerDisplay() => currentText.Insert(this.Caret, "|");

    /// <summary>
    /// Accumulates changed events and invokes only one on dispose.
    /// Also takes care of history tracking (before/after).
    /// </summary>
    private ChangeContext BeginChanges(CodePane codePane, ChangeContextType changeContextType = ChangeContextType.All) =>
        new(codePane, this, changeContextType);

    private readonly struct ChangeContext : IDisposable
    {
        private readonly ChangeContextType changeContextType;
        private readonly CodePane codePane;
        private readonly Document document;

        public ChangeContext(CodePane codePane, Document document, ChangeContextType changeContextType)
        {
            Debug.Assert(document.stringBuilder.ToString() == document.currentText);

            this.changeContextType = changeContextType;
            this.codePane = codePane;
            this.document = document;

            if(changeContextType.HasFlag(ChangeContextType.TextUpdate))
            {
                document.stringBuilder.SuspendChangedEvents();
            }
            if (changeContextType.HasFlag(ChangeContextType.UndoRedoHistory))
            {
                document.undoRedoHistory.Track(document.stringBuilder, document.Caret, codePane.Selection);
            }
        }

        public void Dispose()
        {
            if (changeContextType.HasFlag(ChangeContextType.TextUpdate))
            {
                document.stringBuilder.ResumeChangedEvents();
            }
            if (changeContextType.HasFlag(ChangeContextType.UndoRedoHistory))
            {
                document.undoRedoHistory.Track(document.stringBuilder, document.Caret, codePane.Selection);
            }

            Debug.Assert(document.stringBuilder.ToString() == document.currentText);
        }
    }

    [Flags]
    public enum ChangeContextType
    {
        TextUpdate = 1,
        UndoRedoHistory = 2,
        All = TextUpdate | UndoRedoHistory
    }
}