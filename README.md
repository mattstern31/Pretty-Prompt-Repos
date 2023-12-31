# PrettyPrompt

[![Nuget](https://img.shields.io/nuget/v/PrettyPrompt.svg?style=flat&color=005ca4)](https://www.nuget.org/packages/PrettyPrompt/)
[![Code Coverage](https://codecov.io/gh/waf/PrettyPrompt/branch/main/graph/badge.svg)](https://app.codecov.io/gh/waf/PrettyPrompt)
[![Build Status](https://github.com/waf/PrettyPrompt/workflows/main%20build/badge.svg)](https://github.com/waf/PrettyPrompt/actions/workflows/main.yml)

A cross-platform command line prompt that provides syntax highlighting, autocompletion, history and more! It's `Console.ReadLine()` on steroids.

<p align="center">
  <img src="https://raw.githubusercontent.com/waf/PrettyPrompt/main/images/screenshot.png" alt="PrettyPrompt screenshot" style="max-width:100%;">
</p>

## Features

- User Experience (UX) features:
  - Syntax highlighting support via ANSI escape sequences. Supports both the terminal color palette and full RGB colors.
  - Autocompletion menu, with extended documentation tooltips and overload menus.
  - Multi-line input with word-wrapping
  - Word-wrapping
  - History navigation, optionally persistent across sessions, with history filtering (similar to PSReadLine's `HistorySearchBackward`).
  - Unsurprising keybindings: <kbd>Home</kbd>, <kbd>End</kbd>, <kbd>Ctrl-L</kbd> to clear screen, <kbd>Ctrl-C</kbd> to cancel current line, <kbd>Ctrl+Space</kbd> to open autocomplete menu, and more.
  - Cross platform copy/paste: <kbd>Ctrl-Shift-C</kbd> for copy, <kbd>Ctrl-V</kbd> or <kbd>Shift-Insert</kbd> for pasting.
  - Optionally detects incomplete lines and converts <kbd>Enter</kbd> to a "soft newline" (<kbd>Shift-Enter</kbd>).
  - Optionally autoformats input text as it's typed.
  - Works "in-line" on the command line; it doesn't take over the entire terminal window.
- Developer Experience (DX) features:
  - Many customization hooks available for configuring PrettyPrompt for your application (see `IPromptCallbacks`).
  - Provides a `CancellationToken` for each prompt result, so the end-user of your application can cancel long running tasks via <kbd>Ctrl-C</kbd>.
  - Fast rendering—PrettyPrompt only renders the diff of what changed, so the screen doesn't flicker as text is redrawn.

## Installation

PrettyPrompt can be [installed from nuget](https://www.nuget.org/packages/PrettyPrompt/) by running the following command:

```
dotnet add package PrettyPrompt
```

## Usage

A simple read-eval-print-loop looks like this:

```csharp

var prompt = new Prompt();

while (true)
{
    var response = await prompt.ReadLineAsync("> ");
    if (response.IsSuccess) // false if user cancels, i.e. ctrl-c
    {
        if (response.Text == "exit") break;

        Console.WriteLine("You wrote " + response.Text);
    }
}
```

The `Prompt` constructor takes optional configuration options for enabling syntax highlighting, autocompletion, and soft-newline configuration.
For a more complete example, see the project in the [`examples`](https://github.com/waf/PrettyPrompt/tree/main/examples/PrettyPrompt.Examples.FruitPrompt) directory.
If you have the [`dotnet example`](https://github.com/patriksvensson/dotnet-example) global tool installed, run the following command in the repository root:

```
dotnet example FruitPrompt
```

## Building from source

This application targets modern .NET (i.e. not .NET Framework), and can be built with either Visual Studio or the normal `dotnet build` command line tool.
