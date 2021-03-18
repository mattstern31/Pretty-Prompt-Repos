﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using static System.ConsoleModifiers;
using static System.ConsoleKey;

namespace PrettyPrompt.Panes
{
    public class CompletionPane : IKeyPressHandler
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

        async Task IKeyPressHandler.OnKeyDown(KeyPress key)
        {
            if (!IsOpen)
            {
                if(key.Pattern is (Control, Spacebar))
                {
                    Open(codePane.Caret);
                    key.Handled = true;
                    return;
                }
                key.Handled = false;
                return;
            }

            if (FilteredView is null || FilteredView.Count == 0)
            {
                key.Handled = false;
                return;
            }

            switch(key.Pattern)
            {
                case DownArrow:
                    var next = SelectedItem.Next;
                    if(next is not null)
                    {
                        SelectedItem = next;
                    }
                    key.Handled = true;
                    return;
                case UpArrow:
                    var prev = SelectedItem.Previous;
                    if(prev is not null)
                    {
                        SelectedItem = prev;
                    }
                    key.Handled = true;
                    return;
                case Spacebar:
                    codePane.Caret = InsertCompletion(codePane.Input, codePane.Caret, SelectedItem.Value, " ");
                    key.Handled = true;
                    return;
                case Enter:
                case RightArrow:
                case Tab:
                    codePane.Caret = InsertCompletion(codePane.Input, codePane.Caret, SelectedItem.Value);
                    key.Handled = true;
                    return;
                case (Control, Spacebar) when FilteredView.Count == 1:
                    codePane.Caret = InsertCompletion(codePane.Input, codePane.Caret, FilteredView.First.Value);
                    key.Handled = true;
                    return;
                case (Control, Spacebar):
                    key.Handled = true;
                    return;
                case LeftArrow:
                    Close();
                    key.Handled = false;
                    return;
                case Escape:
                    Close();
                    key.Handled = true;
                    return;
                default:
                    this.SelectedItem = FilteredView.First;
                    key.Handled = false;
                    return;
            }
        }

        async Task IKeyPressHandler.OnKeyUp(KeyPress key)
        {
            if (!char.IsControl(key.ConsoleKeyInfo.KeyChar)
                && ShouldAutomaticallyOpen(codePane.Input, codePane.Caret, key))
            {
                Close();
                Open(codePane.Caret - 1);
            }

            if (codePane.Caret < openedCaretIndex)
            {
                Close();
            }
            else if (IsOpen)
            {
                var textToComplete = codePane.Input.ToString(openedCaretIndex, codePane.Caret - openedCaretIndex);
                if (textToComplete == string.Empty || allCompletions.Count == 0)
                {
                    var completions = await this.complete.Invoke(codePane.Input.ToString(), codePane.Caret);
                    SetCompletions(completions);
                }
                else
                {
                    FilterCompletions(textToComplete);
                }
            }
        }

        private void SetCompletions(IReadOnlyCollection<CompletionItem> completions)
        {
            allCompletions = completions;
            if(completions.Any())
            {
                var completion = completions.First();
                var prefix = completion.ReplacementText.Substring(0, Math.Max(0, openedCaretIndex - completion.StartIndex));
                openedCaretIndex = completion.StartIndex;
                FilterCompletions(prefix);
            }
        }

        private void FilterCompletions(string filter)
        {
            FilteredView = new LinkedList<CompletionItem>();
            foreach (var completion in allCompletions)
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

            static bool Matches(CompletionItem completion, string filter) =>
                completion.ReplacementText.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool ShouldAutomaticallyOpen(StringBuilder input, int caret, KeyPress key)
        {
            if (key.ConsoleKeyInfo.KeyChar is '.' or '(') return true; // typical "intellisense behavior", opens for new methods and parameters

            if (input.Length == 1 && !char.IsWhiteSpace(input[0])) return true; // open on brand new typing in a prompt

            // open when we're starting a new "word" in the prompt.
            return input.Length > 1 && char.IsWhiteSpace(input[caret - 2]) && !char.IsWhiteSpace(input[caret - 1]);
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
