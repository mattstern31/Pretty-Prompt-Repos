﻿using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ConsoleKey;
using static System.ConsoleModifiers;

namespace PrettyPrompt.Panes
{
    internal class CompletionPane : IKeyPressHandler
    {
        private readonly CodePane codePane;
        private readonly CompletionHandlerAsync complete;

        /// <summary>
        /// The index of the caret when the pane was opened
        /// </summary>
        private int openedCaretIndex;

        /// <summary>
        /// All completions available. Called once when the window is initially opened
        /// </summary>
        private IReadOnlyCollection<CompletionItem> allCompletions = Array.Empty<CompletionItem>();

        /// <summary>
        /// An "ordered view" over <see cref="allCompletions"/> that shows the list filtered by what the user has typed.
        /// </summary>
        public LinkedList<CompletionItem> FilteredView { get; set; } = new LinkedList<CompletionItem>();

        /// <summary>
        /// Currently selected item in the completion menu
        /// </summary>
        public LinkedListNode<CompletionItem> SelectedItem { get; set; }

        /// <summary>
        /// Whether or not the window is currently open / visible.
        /// </summary>
        public bool IsOpen { get; set; }

        public CompletionPane(CodePane codePane, CompletionHandlerAsync complete)
        {
            this.codePane = codePane;
            this.complete = complete;
        }

        private void Open(int caret)
        {
            this.IsOpen = true;
            this.openedCaretIndex = caret;
            this.allCompletions = Array.Empty<CompletionItem>();
        }

        private void Close()
        {
            this.IsOpen = false;
            this.openedCaretIndex = int.MinValue;
            this.SelectedItem = null;
            this.FilteredView = new LinkedList<CompletionItem>();
        }

        Task IKeyPressHandler.OnKeyDown(KeyPress key)
        {
            if (!IsOpen)
            {
                if (key.Pattern is (Control, Spacebar))
                {
                    Open(codePane.Caret);
                    key.Handled = true;
                    return Task.CompletedTask;
                }
                key.Handled = false;
                return Task.CompletedTask;
            }

            if (FilteredView is null || FilteredView.Count == 0)
            {
                key.Handled = false;
                return Task.CompletedTask;
            }

            switch (key.Pattern)
            {
                case DownArrow:
                    var next = SelectedItem.Next;
                    if (next is not null)
                    {
                        SelectedItem = next;
                    }
                    key.Handled = true;
                    break;
                case UpArrow:
                    var prev = SelectedItem.Previous;
                    if (prev is not null)
                    {
                        SelectedItem = prev;
                    }
                    key.Handled = true;
                    break;
                case Enter:
                case RightArrow:
                case Tab:
                    codePane.Caret = InsertCompletion(codePane.Input, codePane.Caret, SelectedItem.Value);
                    key.Handled = true;
                    break;
                case (Control, Spacebar) when FilteredView.Count == 1:
                    codePane.Caret = InsertCompletion(codePane.Input, codePane.Caret, FilteredView.First.Value);
                    key.Handled = true;
                    break;
                case (Control, Spacebar):
                    key.Handled = true;
                    break;
                case LeftArrow:
                    Close();
                    key.Handled = false;
                    break;
                case Escape:
                    Close();
                    key.Handled = true;
                    break;
                default:
                    this.SelectedItem = FilteredView.First;
                    key.Handled = false;
                    break;
            }

            return Task.CompletedTask;
        }

        async Task IKeyPressHandler.OnKeyUp(KeyPress key)
        {
            if (!char.IsControl(key.ConsoleKeyInfo.KeyChar) && ShouldAutomaticallyOpen(codePane.Input, codePane.Caret) is int offset and >= 0)
            {
                Close();
                Open(codePane.Caret - offset);
            }

            if (codePane.Caret < openedCaretIndex)
            {
                Close();
            }
            else if (IsOpen)
            {
                if (allCompletions.Count == 0)
                {
                    var completions = await this.complete.Invoke(codePane.Input.ToString(), codePane.Caret);
                    if(completions.Any())
                    {
                        SetCompletions(completions, codePane.Input);
                    }
                    else
                    {
                        Close();
                    }
                }
                else
                {
                    FilterCompletions(codePane.Input);
                    if (HasTypedPastCompletion())
                    {
                        Close();
                    }
                }
            }
        }

        private bool HasTypedPastCompletion() =>
            SelectedItem?.Value is not null && SelectedItem.Value.ReplacementText.Length < (codePane.Caret - openedCaretIndex);

        private void SetCompletions(IReadOnlyCollection<CompletionItem> completions, StringBuilder input)
        {
            allCompletions = completions;
            if (completions.Any())
            {
                var completion = completions.First();
                openedCaretIndex = completion.StartIndex;
                FilterCompletions(input);
            }
        }

        private void FilterCompletions(StringBuilder input)
        {
            FilteredView = new LinkedList<CompletionItem>();
            foreach (var completion in allCompletions)
            {
                if (!Matches(completion, input)) continue;

                var node = FilteredView.AddLast(completion);
                if (completion.ReplacementText == SelectedItem?.Value.ReplacementText)
                {
                    SelectedItem = node;
                }
            }
            if (SelectedItem is null || !Matches(SelectedItem.Value, input))
            {
                SelectedItem = FilteredView.First;
            }

            bool Matches(CompletionItem completion, StringBuilder input) =>
                completion.ReplacementText.StartsWith(
                    input.ToString(completion.StartIndex, codePane.Caret - completion.StartIndex).Trim(),
                    StringComparison.CurrentCultureIgnoreCase
                );
        }

        private static int ShouldAutomaticallyOpen(StringBuilder input, int caret)
        {
            if (caret > 0 && input[caret - 1] is '.' or '(') return 0; // typical "intellisense behavior", opens for new methods and parameters

            if (caret == 1 && !char.IsWhiteSpace(input[0]) // 1 word character typed in brand new prompt
                && (input.Length == 1 || !char.IsLetterOrDigit(input[1]))) // if there's more than one character on the prompt, but we're typing a new word at the beginning (e.g. "a| bar")
            {
                return 1;
            }

            // open when we're starting a new "word" in the prompt.
            return caret - 2 >= 0
                && char.IsWhiteSpace(input[caret - 2])
                && char.IsLetter(input[caret - 1])
                ? 1
                : -1;
        }

        private int InsertCompletion(StringBuilder input, int caret, CompletionItem completion, string suffix = "")
        {
            input.Remove(completion.StartIndex, caret - completion.StartIndex);
            input.Insert(completion.StartIndex, completion.ReplacementText + suffix);
            Close();
            return completion.StartIndex + completion.ReplacementText.Length + suffix.Length;
        }
    }
}
