﻿using System.Threading.Tasks;
using Xunit;
using static System.ConsoleKey;

namespace PrettyPrompt.Tests
{
    public class HistoryTests
    {
        [Fact]
        public async Task ReadLine_WithHistory_DoesNothing()
        {
            var console = ConsoleStub.NewConsole();
            var prompt = new Prompt(console: console);

            console.StubInput($"{UpArrow}{UpArrow}{DownArrow}{DownArrow}yo world{Enter}");
            var result = await prompt.ReadLineAsync("> ");

            // no exceptions, even though we cycled through history when there was no history to cycle through
            Assert.Equal("yo world", result.Text);
        }

        [Fact]
        public async Task ReadLine_WithHistory_CyclesThroughHistory()
        {
            var console = ConsoleStub.NewConsole();
            var prompt = new Prompt(console: console);

            console.StubInput($"Hello World{Enter}");
            await prompt.ReadLineAsync("> ");

            console.StubInput($"Howdy World{Enter}");
            await prompt.ReadLineAsync("> ");

            console.StubInput($"How ya' doin world{Enter}");
            await prompt.ReadLineAsync("> ");

            console.StubInput($"{UpArrow}{UpArrow}{UpArrow}{DownArrow}{Enter}");
            var result = await prompt.ReadLineAsync("> ");

            Assert.Equal("Howdy World", result.Text);
        }

        [Fact]
        public async Task ReadLine_UnsubmittedText_IsNotLostWhenChangingHistory()
        {
            var console = ConsoleStub.NewConsole();
            var prompt = new Prompt(console: console);

            console.StubInput($"Hello World{Enter}");
            await prompt.ReadLineAsync("> ");

            console.StubInput($"this prompt is my persistent storage{UpArrow}{DownArrow}{Enter}");
            var result = await prompt.ReadLineAsync("> ");

            Assert.Equal("this prompt is my persistent storage", result.Text);
        }

        [Fact]
        public async Task ReadLine_TypingOnHistory_ResetsHistory()
        {
            var console = ConsoleStub.NewConsole();
            var prompt = new Prompt(console: console);

            console.StubInput($"And a one{Enter}");
            await prompt.ReadLineAsync("> ");

            console.StubInput($"And a two{Enter}");
            await prompt.ReadLineAsync("> ");

            console.StubInput($"And a one, two, three...{UpArrow}{Backspace}{Backspace}{Backspace}three{UpArrow}{UpArrow}{DownArrow}{DownArrow}{Enter}");
            var result = await prompt.ReadLineAsync("> ");

            Assert.Equal("And a three", result.Text);
        }
    }
}
