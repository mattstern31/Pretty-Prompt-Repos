using System.Threading.Tasks;
using Xunit;
using static PrettyPrompt.Consoles.AnsiEscapeCodes;
using static System.ConsoleKey;
using static System.ConsoleModifiers;
using static System.Environment;

namespace PrettyPrompt.Tests
{
    public class PromptTests
    {
        [Fact]
        public async Task ReadLine_TypeSimpleString_GetSimpleString()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput($"Hello World{Enter}");

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLineAsync("> ");

            Assert.True(result.Success);
            Assert.Equal("Hello World", result.Text);
        }

        [Fact]
        public async Task ReadLine_WhitespacePrompt_ReturnsWhitespace()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput($"  {Enter}");

            // add completion handler, as it has caused problem when completing all whitespace prompts
            var prompt = new Prompt(CompletionTestData.CompletionHandlerAsync, console: console);

            var result = await prompt.ReadLineAsync("> ");

            Assert.True(result.Success);
            Assert.Equal("  ", result.Text);
        }

        [Fact]
        public async Task ReadLine_Abort_NoResult()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput($"Hello World{Control}c");

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLineAsync("> ");

            Assert.False(result.Success);
            Assert.Empty(result.Text);
        }

        [Fact]
        public async Task ReadLine_WordWrap()
        {
            // window width of 5, with a 2 char prompt.
            var console = ConsoleStub.NewConsole(width: 5);
            console.StubInput($"111222333{Control}{L}{Enter}");

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLineAsync("> ");

            Assert.True(result.Success);
            Assert.Equal("111222333", result.Text);

            var finalOutput = console.GetFinalOutput();

            Assert.Equal(
                expected: "111\n" + MoveCursorLeft(2) +
                          "222\n" + MoveCursorLeft(2) +
                          "333\n" + MoveCursorLeft(2),
                actual: finalOutput
            );
        }

        [Fact]
        public async Task ReadLine_HorizontalNavigationKeys()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput(
                $"pretty{Backspace}{Backspace}{Home}{LeftArrow}{RightArrow}{RightArrow}{Delete}omp{RightArrow}!{RightArrow}{Enter}"
            );

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLineAsync("> ");

            Assert.Equal("prompt!", result.Text);
        }

        [Fact]
        public async Task ReadLine_VerticalNavigationKeys()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput(
                $"pretty{Shift}{Enter}",
                $"unit-tested{Shift}{Enter}",
                $"prompt",
                $"{UpArrow}{RightArrow}{RightArrow}{RightArrow}{RightArrow}{RightArrow}?",
                $"{UpArrow} well",
                $"{DownArrow}{DownArrow}!{Enter}"
            );

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLineAsync("> ");

            Assert.Equal($"pretty well{NewLine}unit-tested?{NewLine}prompt!", result.Text);
        }

        [Fact]
        public async Task ReadLine_NextWordPrevWordKeys()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput(
                $"aaaa bbbb 5555{Shift}{Enter}",
                $"dddd x5x5 foo.bar{Shift}{Enter}",
                $"{UpArrow}{Control}{RightArrow}{Control}{RightArrow}{Control}{RightArrow}lum",
                $"{Enter}"
            );

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLineAsync("> ");

            Assert.Equal($"aaaa bbbb 5555{NewLine}dddd x5x5 foo.lumbar{NewLine}", result.Text);
        }

        [Fact]
        public async Task ReadLine_DeleteWordPrevWordKeys()
        {
            var console = ConsoleStub.NewConsole();
            console.StubInput(
                $"aaaa bbbb cccc{Shift}{Enter}",
                $"dddd eeee ffff{Shift}{Enter}",
                $"{UpArrow}{Control}{Delete}{Control}{Backspace}",
                $"{Enter}"
            );

            var prompt = new Prompt(console: console);
            var result = await prompt.ReadLineAsync("> ");

            Assert.Equal($"aaaa bbbb eeee ffff{NewLine}", result.Text);
        }
    }
}

