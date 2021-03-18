﻿using System.Threading.Tasks;
using Xunit;
using static System.ConsoleKey;
using static System.ConsoleModifiers;
using static System.Environment;

namespace PrettyPrompt.Tests
{
    public class CompletionTests
    {
        [Fact]
        public async Task ReadLine_SingleCompletion()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"Aa{Enter}{Enter}");

            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal("Aardvark", result.Text);
        }

        [Fact]
        public async Task ReadLine_MultipleCompletion()
        {
            var console = ConsoleStub.NewConsole();
            // complete 3 animals. For the third animal, start completing Alligator, but then backspace and complete as Alpaca instead.
            console.Input($"Aa{Enter} Z{Tab} Alli{Backspace}{Backspace}{DownArrow}{DownArrow}{RightArrow}{Enter}");

            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal("Aardvark Zebra Alpaca", result.Text);
        }

        [Fact]
        public async Task ReadLine_MultilineCompletion()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"Aa{Enter}{Shift}{Enter}Z{Control}{Spacebar}{Enter}{Enter}");

            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal($"Aardvark{NewLine}Zebra", result.Text);
        }

        [Fact]
        public async Task ReadLine_CompletionMenu_AutoOpens()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"A{Enter}{Shift}{Enter}Z{Enter}{Enter}");

            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal($"Aardvark{NewLine}Zebra", result.Text);
        }

        [Fact]
        public async Task ReadLine_EmptyPrompt_AutoOpens()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"{Control}{Spacebar}{Enter}{Enter}");

            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal($"Aardvark", result.Text);
        }

        [Fact]
        public async Task ReadLine_CompletionWithNoMatches_DoesNotAutoComplete()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"A{Enter} Q{Enter}"); // first {Enter} selects an autocompletion, second {Enter} submits because there are no completions.

            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal($"Aardvark Q", result.Text);
        }
    }
}
