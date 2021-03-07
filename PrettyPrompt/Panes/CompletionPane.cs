﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ConsoleModifiers;
using static System.ConsoleKey;
using static PrettyPrompt.AnsiEscapeCodes;

namespace PrettyPrompt
{
    public class CompletionPane : IKeyPressHandler
    {
        public static readonly LinkedList<Completion> NeedsCompletions = new LinkedList<Completion>();
        private readonly CodePane codePane;
        private readonly CompletionHandlerAsync complete;

        public CompletionPane(CodePane codePane, CompletionHandlerAsync complete)
        {
            this.codePane = codePane;
            this.complete = complete;
        }

        /// <summary>
        /// All completions available. Called once when the window is initially opened
        /// </summary>
        public IReadOnlyCollection<Completion> AllCompletions { get; set; } = NeedsCompletions;

        /// <summary>
        /// A "view" over <see cref="AllCompletions"/> that shows the list filtered by what the user has typed.
        /// </summary>
        public LinkedList<Completion> FilteredView { get; set; } = new LinkedList<Completion>();
        public LinkedListNode<Completion> SelectedItem { get; set; }
        public bool IsOpen { get; set; }
        public int OpenedIndex { get; set; }

        public void SetCompletions(IReadOnlyCollection<Completion> completions)
        {
            AllCompletions = completions;
            if(completions.Any())
            {
                var completion = completions.First();
                var prefix = completion.ReplacementText.Substring(0, OpenedIndex - completion.StartIndex);
                OpenedIndex = completion.StartIndex;
                FilterCompletions(prefix);
            }
        }

        public void FilterCompletions(string filter)
        {
            FilteredView = new LinkedList<Completion>();
            foreach (var completion in AllCompletions)
            {
                if (!Matches(completion, filter)) continue;

                var node = FilteredView.AddLast(completion);
                if (completion.ReplacementText == SelectedItem?.Value.ReplacementText)
                {
                    SelectedItem = node;
                }
            }
            if (SelectedItem is null || !Matches(SelectedItem.Value, filter))
            {
                SelectedItem = FilteredView.First;
            }

            static bool Matches(Completion completion, string filter) =>
                completion.ReplacementText.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task<bool> OnKeyDown(KeyPress key)
        {
            if (!IsOpen)
            {
                if(key.Pattern is (Control, Spacebar))
                {
                    Open(codePane.Caret);
                    return false;
                }
                return true;
            }

            if (FilteredView is null || FilteredView.Count == 0 || AllCompletions == NeedsCompletions) return true;

            switch(key.Pattern)
            {
                case DownArrow:
                    var next = SelectedItem.Next;
                    if(next is not null)
                    {
                        SelectedItem = next;
                    }
                    return false;
                case UpArrow:
                    var prev = SelectedItem.Previous;
                    if(prev is not null)
                    {
                        SelectedItem = prev;
                    }
                    return false;
                case Enter:
                case RightArrow:
                case Tab:
                    var completion = SelectedItem.Value;
                    codePane.Caret = InsertCompletion(codePane.Input, codePane.Caret, completion);
                    return false;
                case (Control, Spacebar) when FilteredView.Count == 1:
                    codePane.Caret = InsertCompletion(codePane.Input, codePane.Caret, FilteredView.First.Value);
                    return false;
                case (Control, Spacebar):
                    return false;
                case Escape:
                    Close();
                    return false;
                default:
                    this.SelectedItem = FilteredView.First;
                    return true;
            }
        }

        private int InsertCompletion(StringBuilder input, int caret, Completion completion)
        {
            input.Remove(completion.StartIndex, caret - completion.StartIndex);
            input.Insert(completion.StartIndex, completion.ReplacementText);
            Close();
            return completion.StartIndex + completion.ReplacementText.Length;
        }

        internal void Open(int caret)
        {
            IsOpen = true;
            this.OpenedIndex = caret;
            AllCompletions = NeedsCompletions;
        }

        internal void Close()
        {
            this.IsOpen = false;
            this.OpenedIndex = int.MinValue;
            this.SelectedItem = null;
            this.FilteredView = new LinkedList<Completion>();
        }

        public async Task<bool> OnKeyUp(KeyPress character)
        {
            if (!char.IsControl(character.ConsoleKeyInfo.KeyChar)
                && (character.ConsoleKeyInfo.KeyChar is '.' or '(' || codePane.Input.Length <= 1 || char.IsWhiteSpace(codePane.Input[codePane.Caret - 2])))
            {
                Close();
                Open(codePane.Caret - 1);
            }

            if (codePane.Caret < OpenedIndex)
            {
                Close();
            }
            if(IsOpen)
            {
                var textToComplete = codePane.Input.ToString(OpenedIndex, codePane.Caret - OpenedIndex);
                if(textToComplete == string.Empty || AllCompletions == NeedsCompletions)
                {
                    var completions = await this.complete.Invoke(codePane.Input.ToString(), codePane.Caret);
                    SetCompletions(completions);
                }
                else
                {
                    FilterCompletions(textToComplete);
                }
            }
            return true;
        }

        public string RenderCompletionMenu(int codeAreaStartColumn, int cursorRow, int cursorColumn)
        {
            //  _  <-- cursor location
            //  ┌──────────────┐
            //  │ completion 1 │
            //  │ completion 2 │
            //  └──────────────┘

            if(!this.IsOpen || codePane.Caret < this.OpenedIndex)
                return string.Empty;

            if (this.FilteredView.Count == 0)
                return string.Empty;

            int wordWidth = this.FilteredView.Max(w => w.ReplacementText.Length);
            int boxWidth = wordWidth + 2 + 2; // two border characters, plus two spaces for padding
            int boxHeight = this.FilteredView.Count + 2; // two border characters

            int boxStart =
                boxWidth > codePane.CodeAreaWidth ? codeAreaStartColumn // not enough room to show to completion box. We'll position all the way to the left, and truncate the box.
                : cursorColumn + boxWidth >= codePane.CodeAreaWidth ? codePane.CodeAreaWidth - boxWidth // not enough room to show to completion box offset to the current cursor. We'll position it stuck to the right.
                : cursorColumn; // enough room, we'll show the completion box offset at the cursor location.

            return Blue
                + MoveCursorToPosition(cursorRow + 1, boxStart)
                + "┌" + TruncateToWindow(new string('─', wordWidth + 2), 2) + "┐" + MoveCursorDown(1) + MoveCursorToColumn(boxStart)
                + string.Concat(this.FilteredView.Select((c,i) =>
                    "│" + (this.SelectedItem?.Value == c ? "|" : " ") + ResetFormatting + TruncateToWindow(c.ReplacementText.PadRight(wordWidth), 4) + Blue + " │" + MoveCursorDown(1) + MoveCursorToColumn(boxStart)
                  ))
                + "└" + TruncateToWindow(new string('─', wordWidth + 2), 2) + "┘" + MoveCursorUp(boxHeight) + MoveCursorToColumn(boxStart)
                + ResetFormatting;

            string TruncateToWindow(string line, int offset) =>
                line.Substring(0, Math.Min(line.Length, codePane.CodeAreaWidth - boxStart - offset));
        }

    }
}
