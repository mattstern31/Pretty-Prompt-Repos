﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PrettyPrompt.Documents;
using PrettyPrompt.Highlighting;

namespace PrettyPrompt;

public static class Extensions
{
    internal static string EnvironmentNewlines(this string text) =>
        Environment.NewLine == "\n"
            ? text
            : text.Replace("\n", Environment.NewLine);

    internal static bool TryGet<T>(this T? nullableValue, out T value)
        where T : struct
    {
        if (nullableValue.HasValue)
        {
            value = nullableValue.GetValueOrDefault();
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public static ConsoleKeyInfo ToKeyInfo(this ConsoleKey consoleKey, char character, ConsoleModifiers modifiersPressed)
       => consoleKey.ToKeyInfo(
           character,
           shift: modifiersPressed.HasFlag(ConsoleModifiers.Shift),
           alt: modifiersPressed.HasFlag(ConsoleModifiers.Alt),
           control: modifiersPressed.HasFlag(ConsoleModifiers.Control));

    public static ConsoleKeyInfo ToKeyInfo(this ConsoleKey consoleKey, char character, bool shift = false, bool alt = false, bool control = false)
       => new(character, consoleKey, shift, alt, control);

    internal static int Clamp(this int value, int min, int max)
    {
        Debug.Assert(min <= max);
        return value < min ? min : (value > max ? max : value);
    }

    internal static void SetContents(this StringBuilder sb, string contents)
    {
        sb.Clear();
        sb.Append(contents);
    }

    public static ReadOnlySpan<char> AsSpan(this string text, TextSpan span)
        => text.AsSpan(span.Start, span.Length);

    internal static void TransformBackground(this Cell cell, AnsiColor? background)
    {
        if (cell.Formatting.Background is null)
        {
            cell.Formatting = cell.Formatting with { Background = background };
        }
    }

    internal static void TransformBackground(this Row row, AnsiColor? background, int startIndex = 0)
    {
        Debug.Assert(startIndex >= 0);
        Debug.Assert(startIndex <= row.Length);
        for (int i = startIndex; i < row.Length; i++)
        {
            row[i].TransformBackground(background);
        }
    }
}