﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using System.Threading.Tasks;
using Xunit;
using static System.ConsoleKey;
using static System.ConsoleModifiers;

namespace PrettyPrompt.Tests
{
    public class SelectionTests
    {
        [Fact]
        public async Task ReadLine_HorizontalSelectWordAndType_ReplacesWord()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput(
                $"The quick brown fox jumped over the lazy dog{Control | Shift}{LeftArrow}giraffe",
                $"{Control}{LeftArrow}{Control}{LeftArrow}{Control | Shift}{RightArrow}enigmatic {Enter}");
            var prompt = new Prompt(console: console);

            var result = await prompt.ReadLineAsync("> ");

            Assert.True(result.IsSuccess);
            Assert.Equal("The quick brown fox jumped over the enigmatic giraffe", result.Text);
        }

        [Fact]
        public async Task ReadLine_SelectAllAndType_ReplacesEverything()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput($"The quick brown fox jumped over the lazy dog{Control}{A}bonk{Enter}");
            var prompt = new Prompt(console: console);

            var result = await prompt.ReadLineAsync("> ");

            Assert.True(result.IsSuccess);
            Assert.Equal("bonk", result.Text);
        }

        [Fact]
        public async Task ReadLine_HorizontalSelectTextWithArrowKeys_SelectsText()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput(
                $"I am really happy!",
                $"{Control}{LeftArrow}{Control}{LeftArrow}",
                $"{Control | Shift}{RightArrow}so ",
                $"{Shift}{LeftArrow}{Shift}{LeftArrow}{Shift}{LeftArrow}",
                $"{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}",
                $"up{Enter}"
                );
            var prompt = new Prompt(console: console);

            var result = await prompt.ReadLineAsync("> ");

            Assert.True(result.IsSuccess);
            Assert.Equal("I am soup!", result.Text);
        }

        [Fact]
        public async Task ReadLine_VerticalSelectTextWithArrowKeys_SelectsText()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput(
                $"There once was a REPL from Spain{Shift}{Enter}",
                $"Whose text select was a bit of a pain{Shift}{Enter}",
                $"But through unit testing{Shift}{Enter}",
                $"Which was quite interesting{Shift}{Enter}",
                $"The implementation was proved right as rain!",
                $"{UpArrow}{UpArrow}",
                $"{Control}{LeftArrow}{Control}{LeftArrow}{LeftArrow}",
                $"{Shift}{UpArrow}{Shift}{UpArrow}{Shift}{UpArrow}",
                $"{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{RightArrow}{Delete}",
                $"{Shift}{RightArrow}{Shift}{RightArrow}{Shift}{Home}{Shift}{End}{Shift}{DownArrow}{Shift}{DownArrow}",
                $"{Control | Shift}{End}{Control | Shift}{LeftArrow}{Shift}{LeftArrow}{Delete}{Enter}"
                );
            var prompt = new Prompt(console: console);

            var result = await prompt.ReadLineAsync("> ");

            Assert.True(result.IsSuccess);
            Assert.Equal("There once was rain!", result.Text);
        }
    }
}
