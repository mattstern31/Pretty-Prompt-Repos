﻿using System.Collections.Generic;
using System.Threading.Tasks;
using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using PrettyPrompt.Documents;
using PrettyPrompt.Highlighting;

namespace PrettyPrompt.Tests;

internal delegate Task<TextSpan> SpanToReplaceByCompletionCallbackAsync(string text, int caret);
internal delegate Task<IReadOnlyList<CompletionItem>> CompletionCallbackAsync(string text, int caret, TextSpan spanToBeReplaced);
internal delegate Task<bool> OpenCompletionWindowCallbackAsync(string text, int caret);
internal delegate Task<IReadOnlyCollection<FormatSpan>> HighlightCallbackAsync(string text);
internal delegate Task<bool> ForceSoftEnterCallbackAsync(string text, int caret, KeyPressPattern keyPress);

internal class TestPromptCallbacks : PromptCallbacks
{
    public SpanToReplaceByCompletionCallbackAsync? SpanToReplaceByCompletionCallback { get; set; }
    public CompletionCallbackAsync? CompletionCallback { get; set; }
    public OpenCompletionWindowCallbackAsync? OpenCompletionWindowCallback { get; set; }
    public HighlightCallbackAsync? HighlightCallback { get; set; }
    public ForceSoftEnterCallbackAsync? InterpretKeyPressAsInputSubmitCallback { get; set; }

    public TestPromptCallbacks(Dictionary<object, KeyPressCallbackAsync>? keyPressCallbacks = null)
    {
        if (keyPressCallbacks != null)
        {
            foreach (var (key, value) in keyPressCallbacks)
            {
                base.keyPressCallbacks.Add(key, value);
            }
        }
    }

    protected override Task<TextSpan> GetSpanToReplaceByCompletionkAsync(string text, int caret)
    {
        return
            SpanToReplaceByCompletionCallback is null ?
            base.GetSpanToReplaceByCompletionkAsync(text, caret) :
            SpanToReplaceByCompletionCallback(text, caret);
    }

    public override Task<IReadOnlyList<CompletionItem>> GetCompletionItemsAsync(string text, int caret, TextSpan spanToBeReplaced)
    {
        return
            CompletionCallback is null ?
            base.GetCompletionItemsAsync(text, caret, spanToBeReplaced) :
            CompletionCallback(text, caret, spanToBeReplaced);
    }

    public override Task<bool> ShouldOpenCompletionWindowAsync(string text, int caret)
    {
        return
            OpenCompletionWindowCallback is null ?
            base.ShouldOpenCompletionWindowAsync(text, caret) :
            OpenCompletionWindowCallback(text, caret);
    }

    public override Task<IReadOnlyCollection<FormatSpan>> HighlightCallbackAsync(string text)
    {
        return
            HighlightCallback is null ?
            base.HighlightCallbackAsync(text) :
            HighlightCallback(text);
    }

    public override Task<bool> InterpretKeyPressAsInputSubmit(string text, int caret, KeyPressPattern keyPress)
    {
        return
            InterpretKeyPressAsInputSubmitCallback is null ?
            base.InterpretKeyPressAsInputSubmit(text, caret, keyPress) :
            InterpretKeyPressAsInputSubmitCallback(text, caret, keyPress);
    }
}