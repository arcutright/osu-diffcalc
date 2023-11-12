using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpPolyfills;

// source for net framework:
// https://referencesource.microsoft.com/#mscorlib/system/runtime/compilerservices/runtimehelpers.cs,b8d6172d5976a77a
// source for net core / modern .net:
// https://github.com/dotnet/runtime/tree/release/7.0/src/libraries/System.Private.CoreLib/src/System
// https://github.com/dotnet/runtime/blob/release/7.0/src/libraries/System.Private.CoreLib/src/System/MemoryExtensions.cs

public static partial class MissingExtensionMethods {

#if NETFRAMEWORK && !NET472_OR_GREATER

		/// <summary>
		/// Creates a System.Collections.Generic.HashSet`1 from an System.Collections.Generic.IEnumerable`1.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <param name="source">An System.Collections.Generic.IEnumerable`1 to create a System.Collections.Generic.HashSet`1 from</param>
		/// <returns>A System.Collections.Generic.HashSet`1 that contains values of type TSource selected from the input sequence.</returns>
		public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) {
			return new HashSet<TSource>(source);
		}

#endif

#if NETFRAMEWORK

		/// <summary>
		/// Returns a value indicating whether a specified character occurs within this string.
		/// </summary>
		/// <param name="text">The string to search in.</param>
		/// <param name="value">The character to seek.</param>
		/// <remarks>This method performs an ordinal (case-sensitive and culture-insensitive) comparison.</remarks>
		/// <returns>true if the value parameter occurs within this string; otherwise, false.</returns>
		public static bool Contains(this string text, char value)
			=> text.IndexOf(value) >= 0;

		/// <summary>
		/// Returns a value indicating whether a specified string occurs within this string, using the specified comparison rules.
		/// </summary>
		/// <param name="text">The string to search in.</param>
		/// <param name="value">The string to seek.</param>
		/// <param name="comparison">One of the enumeration values that specifies the rules to use in the comparison.</param>
		/// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
		public static bool Contains(this string text, string value, StringComparison comparison)
			=> text.IndexOf(value, comparison) >= 0;

		/// <summary>
		/// Determines whether the end of the <paramref name="text"/> string instance matches the specified character.
		/// </summary>
		/// <param name="text">The text to check.</param>
		/// <param name="ch">The character to compare to the character at the end of <paramref name="text"/>.</param>
		/// <returns>
		/// True if <paramref name="text"/> is non-empty and ends with <paramref name="ch"/>.
		/// False if <paramref name="text"/> is null, empty, or doesn't end with <paramref name="ch"/>.
		/// </returns>
		public static bool EndsWith(this string text, char ch)
			=> text.Length > 0 && text[text.Length - 1] == ch;

		/// <summary>
		/// Determines whether the start of the <paramref name="text"/> string instance matches the specified character.
		/// </summary>
		/// <param name="text">The text to check.</param>
		/// <param name="ch">The character to compare to the character at the start of <paramref name="text"/>.</param>
		/// <returns>
		/// True if <paramref name="text"/> is non-empty and starts with <paramref name="ch"/>.
		/// False if <paramref name="text"/> is null, empty, or doesn't start with <paramref name="ch"/>.
		/// </returns>
		public static bool StartsWith(this string text, char ch)
			=> text.Length > 0 && text[0] == ch;
			
		/// <summary>
		/// Returns a value indicating whether the specified <paramref name="value"/> occurs within the <paramref name="span"/>.
		/// <param name="span">The source span.</param>
		/// <param name="value">The value to seek within the source span.</param>
		/// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
		/// </summary>
		public static bool Contains(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
			=> span.IndexOf(value, comparisonType) >= 0;

		/// <inheritdoc cref="Contains(ReadOnlySpan{char}, ReadOnlySpan{char}, StringComparison)"/>
		public static bool Contains(this ReadOnlySpan<char> span, string value, StringComparison comparisonType)
			=> Contains(span, value.AsSpan(), comparisonType);

		/// <summary>
		/// Determines whether the beginning of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared using the specified <paramref name="comparisonType"/> option.
		/// </summary>
		/// <param name="span">The source span.</param>
		/// <param name="value">The sequence to compare to the beginning of the source span.</param>
		/// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType) {
			if (comparisonType == StringComparison.Ordinal)
				return span.StartsWith(value);
			else
				return value.Length <= span.Length
							 && span[..value.Length].CompareTo(value, StringComparison.OrdinalIgnoreCase) == 0;
		}

		/// <inheritdoc cref="StartsWith(ReadOnlySpan{char}, ReadOnlySpan{char}, StringComparison)"/>
		public static bool StartsWith(this ReadOnlySpan<char> span, string value, StringComparison comparisonType)
			=> StartsWith(span, value.AsSpan(), comparisonType);

		/// <summary>
		/// Determines whether the end of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared using the specified <paramref name="comparisonType"/> option.
		/// </summary>
		/// <param name="span">The source span.</param>
		/// <param name="value">The sequence to compare to the end of the source span.</param>
		/// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType) {
			if (comparisonType == StringComparison.Ordinal)
				return span.EndsWith(value);
			else
				return value.Length <= span.Length
							 && span[(span.Length - value.Length)..].CompareTo(value, StringComparison.OrdinalIgnoreCase) == 0;
		}

		/// <inheritdoc cref="EndsWith(ReadOnlySpan{char}, ReadOnlySpan{char}, StringComparison)"/>
		public static bool EndsWith(this ReadOnlySpan<char> span, string value, StringComparison comparisonType)
			=> EndsWith(span, value.AsSpan(), comparisonType);

#endif
}
