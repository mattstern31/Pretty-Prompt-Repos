﻿#region License Header
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PrettyPrompt.Rendering;

namespace PrettyPrompt
{
    internal static class Extensions
    {
        public static string EnvironmentNewlines(this string text) =>
            Environment.NewLine == "\n"
                ? text
                : text.Replace("\n", Environment.NewLine);

        public static IEnumerable<string> EnumerateTextElements(this string text)
        {
            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                yield return enumerator.GetTextElement();
            }
        }

        public static IEnumerable<string> SplitIntoSubstrings(this string str, int maxChunkSize)
        {
            var stringWidth = UnicodeWidth.GetWidth(str);
            if (stringWidth <= maxChunkSize)
            {
                yield return str;
                yield break;
            }

            var buffer = new StringBuilder();

            int width = 0;
            foreach (var c in str)
            {
                var cWidth = UnicodeWidth.GetWidth(c);
                if (width + cWidth > maxChunkSize)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                    width = 0;
                }
                width += cWidth;
                buffer.Append(c);
            }

            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
            }
        }

        /// <summary>
        /// Like <see cref="System.Linq.Enumerable.Zip{TFirst, TSecond}(IEnumerable{TFirst}, IEnumerable{TSecond})"/>,
        /// but the length of the zipped sequence is equal to the longer enumerable (with default(T) elements for the shorter enumerable).
        /// </summary>
        public static IEnumerable<(int, T1, T2)> ZipLongest<T1, T2>(this IEnumerable<T1> left, IEnumerable<T2> right)
        {
            var leftEnumerator = left.GetEnumerator();
            var rightEnumerator = right.GetEnumerator();

            bool hasLeft = leftEnumerator.MoveNext();
            bool hasRight = rightEnumerator.MoveNext();

            int i = 0;
            while (hasLeft || hasRight)
            {
                if (hasLeft && hasRight)
                {
                    yield return (i, leftEnumerator.Current, rightEnumerator.Current);
                }
                else if (hasLeft)
                {
                    yield return (i, leftEnumerator.Current, default);
                }
                else if (hasRight)
                {
                    yield return (i, default, rightEnumerator.Current);
                }

                hasLeft = leftEnumerator.MoveNext();
                hasRight = rightEnumerator.MoveNext();
                i++;
            }
        }

        public static bool TryGet<T>(this T? nullableValue, out T value)
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
    }
}
