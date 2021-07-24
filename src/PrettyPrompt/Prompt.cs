﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using PrettyPrompt.Cancellation;
using PrettyPrompt.Consoles;
using PrettyPrompt.Highlighting;
using PrettyPrompt.History;
using PrettyPrompt.Panes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PrettyPrompt
{
    /// <inheritdoc cref="IPrompt" />
    public sealed class Prompt : IPrompt
    {
        private readonly IConsole console;
        private readonly HistoryLog history;
        private readonly CancellationManager cancellationManager;

        private readonly CompletionCallbackAsync completionCallback;
        private readonly ForceSoftEnterCallbackAsync detectSoftEnterCallback;
        private readonly Dictionary<object, KeyPressCallbackAsync> keyPressCallbacks;
        private readonly SyntaxHighlighter highlighter;

        /// <summary>
        /// Instantiates a prompt object. This object can be re-used for multiple lines of input.
        /// </summary>
        /// <param name="persistentHistoryFilepath">The filepath of where to store history entries. If null, persistent history is disabled.</param>
        /// <param name="callbacks">A collection of callbacks for modifying and intercepting the prompt's behavior</param>
        /// <param name="console">The implementation of the console to use. This is mainly for ease of unit testing</param>
        public Prompt(
            string persistentHistoryFilepath = null,
            PromptCallbacks callbacks = null,
            IConsole console = null)
        {
            this.console = console ?? new SystemConsole();
            this.console.InitVirtualTerminalProcessing();

            this.history = new HistoryLog(persistentHistoryFilepath);
            this.cancellationManager = new CancellationManager(this.console);

            callbacks ??= new PromptCallbacks();
            this.completionCallback = callbacks.CompletionCallback;
            this.detectSoftEnterCallback = callbacks.ForceSoftEnterCallback;
            this.keyPressCallbacks = callbacks.KeyPressCallbacks;

            var highlightCallback = callbacks.HighlightCallback;
            this.highlighter = new SyntaxHighlighter(highlightCallback, HasUserOptedOutFromColor);
        }

        /// <inheritdoc cref="IPrompt.ReadLineAsync(string)" />
        public async Task<PromptResult> ReadLineAsync(string prompt)
        {
            var renderer = new Renderer(console, prompt, HasUserOptedOutFromColor);
            renderer.RenderPrompt();

            // code pane contains the code the user is typing. It does not include the prompt (i.e. "> ")
            var codePane = new CodePane(topCoordinate: console.CursorTop, detectSoftEnterCallback);
            codePane.MeasureConsole(console, prompt);

            // completion pane is the pop-up window that shows potential autocompletions.
            var completionPane = new CompletionPane(codePane, completionCallback);

            history.Track(codePane);
            cancellationManager.CaptureControlC();

            foreach(var key in KeyPress.ReadForever(console))
            {
                // grab the code area width every key press, so we rerender appropriately when the console is resized.
                codePane.MeasureConsole(console, prompt);

                await InterpretKeyPress(key, codePane, completionPane).ConfigureAwait(false);

                // typing / word-wrapping may have scrolled the console, giving us more room.
                codePane.MeasureConsole(console, prompt);

                // render the typed input, with syntax highlighting
                var inputText = codePane.Document.GetText();
                var highlights = await highlighter.HighlightAsync(inputText).ConfigureAwait(false);
                await renderer.RenderOutput(codePane, completionPane, highlights, key).ConfigureAwait(false);

                // process any user-defined keyboard shortcuts
                if (keyPressCallbacks.TryGetValue(key.Pattern, out var callback))
                {
                    await callback.Invoke(inputText, codePane.Document.Caret).ConfigureAwait(false);
                }

                if (codePane.Result is not null)
                {
                    _ = history.SavePersistentHistoryAsync(inputText);
                    cancellationManager.AllowControlCToCancelResult(codePane.Result);
                    return codePane.Result;
                }
            }

            Debug.Assert(false, "Should never reach here due to infinite " + nameof(KeyPress.ReadForever));
            return null;
        }

        private async Task InterpretKeyPress(KeyPress key, CodePane codePane, CompletionPane completionPane)
        {
            foreach (var panes in new IKeyPressHandler[] { completionPane, codePane, history })
                await panes.OnKeyDown(key).ConfigureAwait(false);

            codePane.WordWrap();

            foreach (var panes in new IKeyPressHandler[] { completionPane, codePane, history })
                await panes.OnKeyUp(key).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IPrompt.HasUserOptedOutFromColor" />
        public bool HasUserOptedOutFromColor { get; } = Environment.GetEnvironmentVariable("NO_COLOR") is not null;
    }

    /// <summary>
    /// The main entry point of the prompt functionality.
    /// This class should be instantiated once with the desired configuration; then <see cref="ReadLineAsync"/> 
    /// can be called once for each line of input.
    /// </summary>
    /// <remarks>
    /// We don't actually use this interface internally, but it's likely that
    /// users will want to mock the prompt as it's IO-related.
    /// </remarks>
    public interface IPrompt
    {
        /// <summary>
        /// Prompts the user for input and returns the result.
        /// </summary>
        /// <param name="prompt">The prompt string to draw (e.g. "> ")</param>
        /// <returns>The input that the user submitted</returns>
        Task<PromptResult> ReadLineAsync(string prompt);

        /// <summary>
        /// <code>true</code> if the user opted out of color, via an environment variable as specified by https://no-color.org/.
        /// PrettyPrompt will automatically disable colors in this case. You can read this property to control other colors in
        /// your application.
        /// </summary>
        bool HasUserOptedOutFromColor { get; }
    }

    /// <summary>
    /// Represents the user's input from the prompt.
    /// If the user successfully submitted text, Success will be true and Text will be present.
    /// If the user cancelled (via ctrl-c), Success will be false and Text will be an empty string.
    /// </summary>
    public record PromptResult(bool IsSuccess, string Text, bool IsHardEnter)
    {
        internal CancellationTokenSource CancellationTokenSource { get; set; }
        public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? CancellationToken.None;
    }
}
