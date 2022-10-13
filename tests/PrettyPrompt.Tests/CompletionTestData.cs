﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrettyPrompt.Completion;
using PrettyPrompt.Documents;
using PrettyPrompt.Highlighting;

namespace PrettyPrompt.Tests;

public class CompletionTestData
{
    private readonly IReadOnlyCollection<string> completions;

    public CompletionTestData()
        : this(null)
    { }

    public CompletionTestData(params string[]? completions)
    {
        this.completions = completions ?? new[] { "Aardvark", "Albatross", "Alligator", "Alpaca", "Ant", "Anteater", "Baboon", "Cat", "Dog", "Elephant", "Fox", "Zebra" };
    }

    public Task<IReadOnlyList<CompletionItem>> CompletionHandlerAsync(string typedInput, int caret, TextSpan spanToBeReplaced)
    {
        var typedWord = typedInput.AsSpan(spanToBeReplaced.Start, spanToBeReplaced.Length).ToString();
        return Task.FromResult<IReadOnlyList<CompletionItem>>(
            completions
                .Select((c, i) => new CompletionItem(
                    replacementText: c,
                    displayText: i % 2 == 0 ? c : null, // display text is optional, ReplacementText should be used when this is null.
                    getExtendedDescription: _ => Task.FromResult<FormattedString>("a vivid description of " + c)
                ))
                .ToArray()
        );
    }

    public Task<(IReadOnlyList<OverloadItem>, int ArgumentIndex)> OverloadHandlerAsync(string text, int caret)
    {
        if(text == "ant(")
        {
            return Task.FromResult<(IReadOnlyList<OverloadItem>, int)>((
                new List<OverloadItem>
                {
                    new OverloadItem("red", "a red ant", "ant", new[]
                    {
                        new OverloadItem.Parameter("head", ""),
                        new OverloadItem.Parameter("thorax", "the middle part of the ant"),
                    }),
                    new OverloadItem("black", "a black ant", "ant", new[]
                    {
                        new OverloadItem.Parameter("head", ""),
                        new OverloadItem.Parameter("thorax", "the middle part of the ant"),
                    }),
                },
                0
            ));
        }

        return Task.FromResult<(IReadOnlyList<OverloadItem>, int)>((
            new List<OverloadItem>(),
            0
        ));
    }
}