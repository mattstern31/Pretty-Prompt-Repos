using Xunit;
using System.Threading.Tasks;
using static System.ConsoleKey;
using static System.ConsoleModifiers;
using static System.Environment;
using static PrettyPrompt.AnsiEscapeCodes;

namespace PrettyPrompt.Tests
{
    public class PromptTests
    {
        [Fact]
        public async Task ReadLine_TypeSimpleString_GetSimpleString()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"Hello World{Enter}");

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal("Hello World", result.Text);
        }

        [Fact]
        public async Task ReadLine_WhitespacePrompt_ReturnsWhitespace()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"  {Enter}");

            // add completion handler, as it has caused problem when completing all whitespace prompts
            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal("  ", result.Text);
        }

        [Fact]
        public async Task ReadLine_Abort_NoResult()
        {
            var console = ConsoleStub.NewConsole();
            console.Input($"Hello World{Control}c");

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLine("> ");

            Assert.False(result.Success);
            Assert.Empty(result.Text);
        }

        [Fact]
        public async Task ReadLine_WordWrap()
        {
            // window width of 5, with a 2 char prompt.
            var console = ConsoleStub.NewConsole(width: 5);
            console.Input($"111222333{Enter}");

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLine("> ");

            Assert.True(result.Success);
            Assert.Equal("111222333", result.Text);

            var finalOutput = console.GetFinalOutput();

            Assert.Equal(
                expected: MoveCursorToPosition(1, 1) + ClearToEndOfScreen +
                          "> 111" +
                          "  222" + 
                          "  333" + 
                          MoveCursorToPosition(row: 4, column: 3),
                actual: finalOutput
            );
        }

        [Fact]
        public async Task ReadLine_HorizontalNavigationKeys()
        {
            var console = ConsoleStub.NewConsole();
            console.Input(
                $"pretty{Backspace}{Backspace}{Home}{LeftArrow}{RightArrow}{RightArrow}{Delete}omp{RightArrow}!{RightArrow}{Enter}"
            );

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLine("> ");

            Assert.Equal("prompt!", result.Text);
        }

        [Fact]
        public async Task ReadLine_VerticalNavigationKeys()
        {
            var console = ConsoleStub.NewConsole();
            console.Input(
                $"pretty{Shift}{Enter}",
                $"unit-tested{Shift}{Enter}",
                $"prompt",
                $"{UpArrow}{RightArrow}{RightArrow}{RightArrow}{RightArrow}{RightArrow}?",
                $"{UpArrow} well",
                $"{DownArrow}{DownArrow}!{Enter}"
            );

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLine("> ");

            Assert.Equal($"pretty well{NewLine}unit-tested?{NewLine}prompt!", result.Text);
        }
    }
}

