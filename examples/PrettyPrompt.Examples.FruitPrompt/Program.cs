﻿using PrettyPrompt.Completion;
using PrettyPrompt.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrettyPrompt
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome! Try typing some fruit names.");
            Console.WriteLine();

            var prompt = new Prompt(completionHandler: FindCompletions, highlightHandler: Highlight, persistentHistoryFilepath: "./history-file");
            while (true)
            {
                var response = await prompt.ReadLineAsync("> ").ConfigureAwait(false);
                if (response.Success)
                {
                    if (response.Text == "exit") break;
                    // optionally, use response.CancellationToken so the user can
                    // cancel long-running processing of their response via ctrl-c
                    Console.WriteLine("You wrote " + response.Text);
                }
            }
        }

        // demo data
        private static (string name, string description, AnsiColor highlight)[] Fruits = new[]
        {
            ( "apple", "the round fruit of a tree of the rose family, which typically has thin red or green skin and crisp flesh. Many varieties have been developed as dessert or cooking fruit or for making cider.", AnsiColor.BrightRed ),
            ( "apricot", "a juicy, soft fruit, resembling a small peach, of an orange-yellow color.", AnsiColor.Yellow ),
            ( "avocado", "a pear-shaped fruit with a rough leathery skin, smooth oily edible flesh, and a large stone.", AnsiColor.Green ),
            ( "banana", "a long curved fruit which grows in clusters and has soft pulpy flesh and yellow skin when ripe.", AnsiColor.BrightYellow ),
            ( "cantaloupe", "a small round melon of a variety with orange flesh and ribbed skin.", AnsiColor.Green ),
            ( "grapefruit", "a large round yellow citrus fruit with an acid juicy pulp.", AnsiColor.RGB(224, 112, 124) ),
            ( "grape", "a berry, typically green (classified as white), purple, red, or black, growing in clusters on a grapevine, eaten as fruit, and used in making wine.", AnsiColor.Blue ),
            ( "mango", "a fleshy, oval, yellowish-red tropical fruit that is eaten ripe or used green for pickles or chutneys.", AnsiColor.Yellow ),
            ( "melon", "the large round fruit of a plant of the gourd family, with sweet pulpy flesh and many seeds.", AnsiColor.Green ),
            ( "orange", "a round juicy citrus fruit with a tough bright reddish-yellow rind.", AnsiColor.RGB(255, 165, 0) ),
            ( "pear", "a yellowish- or brownish-green edible fruit that is typically narrow at the stalk and wider toward the base, with sweet, slightly gritty flesh.", AnsiColor.Green ),
            ( "peach", "a round stone fruit with juicy yellow flesh and downy pinkish-yellow skin.", AnsiColor.RGB(255, 229, 180) ),
            ( "pineapple", "a large juicy tropical fruit consisting of aromatic edible yellow flesh surrounded by a tough segmented skin and topped with a tuft of stiff leaves.", AnsiColor.BrightYellow ),
            ( "strawberry", "a sweet soft red fruit with a seed-studded surface.", AnsiColor.BrightRed ),
        };

        // demo completion algorithm callback
        private static Task<IReadOnlyList<CompletionItem>> FindCompletions(string typedInput, int caret)
        {
            var textUntilCaret = typedInput.Substring(0, caret);
            var previousWordStart = textUntilCaret.LastIndexOfAny(new[] { ' ', '\n', '.', '(', ')' });
            var typedWord = previousWordStart == -1
                ? textUntilCaret.ToLower()
                : textUntilCaret.Substring(previousWordStart + 1).ToLower();
            return Task.FromResult<IReadOnlyList<CompletionItem>>(
                Fruits
                .Where(fruit => fruit.name.StartsWith(typedWord))
                .Select(fruit => new CompletionItem
                {
                    StartIndex = previousWordStart + 1,
                    ReplacementText = fruit.name,
                    ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult(fruit.description))
                })
                .ToArray()
            );
        }

        // demo syntax highlighting callback
        private static Task<IReadOnlyCollection<FormatSpan>> Highlight(string text)
        {
            var spans = new List<FormatSpan>();

            for (int i = 0; i < text.Length; i++)
            {
                foreach (var fruit in Fruits)
                {
                    if (text.Length >= i + fruit.name.Length && text.Substring(i, fruit.name.Length).ToLower() == fruit.name)
                    {
                        spans.Add(new FormatSpan(i, fruit.name.Length, new ConsoleFormat(foreground: fruit.highlight)));
                        i += fruit.name.Length;
                        break;
                    }
                }
            }
            return Task.FromResult<IReadOnlyCollection<FormatSpan>>(spans);
        }
    }
}
