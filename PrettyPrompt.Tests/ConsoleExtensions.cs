﻿using System;
using PrettyPrompt.Consoles;
using NSubstitute;
using NSubstitute.Core;
using static System.ConsoleModifiers;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace PrettyPrompt.Tests
{
    public static class ConsoleExtensions
    {
        private static readonly Regex FormatStringSplit = new Regex(@"({\d+}|.)");

        public static IReadOnlyList<string> AllOutput(this IConsole consoleStub) =>
            consoleStub.ReceivedCalls()
                .Where(call => call.GetMethodInfo().Name == nameof(Console.Write))
                .Select(call => (string)call.GetArguments().Single())
                .ToArray();

        /// <summary>
        /// Stub Console.ReadKey to return a series of keystrokes (<see cref="ConsoleKeyInfo" />).
        /// Keystrokes are specified as a <see cref="System.FormattableString"/> with any special keys,
        /// like modifiers or navigation keys, represented as FormattableString arguments (of type
        /// <see cref="ConsoleModifiers"/> or <see cref="ConsoleKey"/>).
        /// </summary>
        /// <example>$"{Control}LHello{Enter}" is turned into Ctrl-L, H, e, l, l, o, Enter key</example>
        public static ConfiguredCall Input(this IConsole consoleStub, params FormattableString[] inputs)
        {
            List<ConsoleKeyInfo> keys = inputs
                .SelectMany(line => MapToConsoleKeyPresses(line))
                .ToList();

            consoleStub
                .KeyAvailable
                .Returns(true);

            return consoleStub
                .ReadKey(intercept: true)
                .Returns(keys.First(), keys.Skip(1).ToArray());
        }

        private static List<ConsoleKeyInfo> MapToConsoleKeyPresses(FormattableString input)
        {
            ConsoleModifiers modifiersPressed = 0;
            // split the formattable strings into a mix of format placeholders (e.g. {0}, {1}) and literal characters.
            // For the format placeholders, we can get the arguments as their original objects (ConsoleModifiers or ConsoleKey).
            return FormatStringSplit
                .Matches(input.Format)
                .Aggregate(
                    seed: new List<ConsoleKeyInfo>(),
                    func: (list, key) =>
                    {
                        if (key.Value.StartsWith('{') && key.Value.EndsWith('}'))
                        {
                            var formatArgument = input.GetArgument(int.Parse(key.Value.Trim('{', '}')));
                            modifiersPressed = AppendFormatStringArgument(list, key, modifiersPressed, formatArgument);
                        }
                        else
                        {
                            modifiersPressed = AppendLiteralKey(list, key.Value.Single(), modifiersPressed);
                        }

                        return list;
                    }
                );
        }

        private static ConsoleModifiers AppendLiteralKey(List<ConsoleKeyInfo> list, char keyChar, ConsoleModifiers modifiersPressed)
        {
            list.Add(ToConsoleKeyInfo(modifiersPressed, (ConsoleKey)char.ToUpper(keyChar), keyChar));
            return 0;
        }

        private static ConsoleModifiers AppendFormatStringArgument(List<ConsoleKeyInfo> list, Match key, ConsoleModifiers modifiersPressed, object formatArgument)
        {
            switch (formatArgument)
            {
                case ConsoleModifiers modifier:
                    return modifiersPressed | modifier;
                case ConsoleKey consoleKey:
                    var parsed = char.TryParse(key.Value, out char character);
                    list.Add(ToConsoleKeyInfo(modifiersPressed, consoleKey, parsed ? character : '\0'));
                    return 0;
                default: throw new ArgumentException("Unknown value: " + formatArgument, nameof(formatArgument));
            }
        }

        private static ConsoleKeyInfo ToConsoleKeyInfo(ConsoleModifiers modifiersPressed, ConsoleKey consoleKey, char character) =>
            new ConsoleKeyInfo(
                character, consoleKey,
                modifiersPressed.HasFlag(Shift), modifiersPressed.HasFlag(Alt), modifiersPressed.HasFlag(Control)
            );
    }
}

