// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Defines a transformation for property names from .NET to msgpack.
/// </summary>
public abstract class MessagePackNamingPolicy
{
	/// <summary>
	/// Gets a naming policy that converts identifiers to camelCase
	/// for serialization purposes.
	/// </summary>
	public static MessagePackNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

	/// <summary>
	/// Gets a naming policy that converts identifiers to PascalCase
	/// for serialization purposes.
	/// </summary>
	public static MessagePackNamingPolicy PascalCase { get; } = new PascalCaseNamingPolicy();

	/// <summary>
	/// Gets a naming policy that converts identifiers to lower-case kebab-case (e.g., "example-name")
	/// for serialization purposes.
	/// </summary>
	public static MessagePackNamingPolicy KebabLowerCase { get; } = new KebabLowerCaseNamingPolicy();

	/// <summary>
	/// Gets a naming policy that converts identifiers to upper-case kebab-case (e.g., "EXAMPLE-NAME")
	/// for serialization purposes.
	/// </summary>
	public static MessagePackNamingPolicy KebabUpperCase { get; } = new KebabUpperCaseNamingPolicy();

	/// <summary>
	/// Gets a naming policy that converts property names to snake_case (with all lowercase letters)
	/// for serialization purposes.
	/// </summary>
	public static MessagePackNamingPolicy SnakeLowerCase { get; } = new SnakeLowerCaseNamingPolicy();

	/// <summary>
	/// Gets a naming policy that converts property names to SNAKE_CASE (with all uppercase letters)
	/// for serialization purposes.
	/// </summary>
	public static MessagePackNamingPolicy SnakeUpperCase { get; } = new SnakeUpperCaseNamingPolicy();

	/// <summary>
	/// Transforms a property name as defined in .NET to a property name as it should be serialized to MessagePack.
	/// </summary>
	/// <param name="name">The .NET property name.</param>
	/// <returns>The msgpack property name.</returns>
	public abstract string ConvertName(string name);

	/// <summary>
	/// A useful base class for identifier transformations based on tokenized words.
	/// </summary>
	/// <remarks>
	/// This class case largely from <see href="https://github.com/dotnet/runtime/blob/b2974279efd059efaa17f359ed4b266b1c705721/src/libraries/System.Text.Json/Common/JsonSeparatorNamingPolicy.cs#L11">this System.Text.Json code</see>.
	/// </remarks>
	private abstract class BuiltInPolicy : MessagePackNamingPolicy
	{
		/// <summary>
		/// The maximum number of bytes that should be allocated on the stack.
		/// </summary>
		private const int StackallocByteThreshold = 256;

		/// <summary>
		/// The maximum number of characters that should be allocated on the stack.
		/// </summary>
		private const int StackallocCharThreshold = StackallocByteThreshold / 2;

		private readonly bool lowercase;
		private readonly bool capitalizeFirstLetterOfSubsequentWords;
		private readonly bool capitalizeFirstLetterOfFirstWord;
		private readonly char? separator;

		internal BuiltInPolicy(bool lowercase, bool capitalizeFirstLetterOfSubsequentWords = false, bool capitalizeFirstLetterOfFirstWord = false, char? separator = null)
		{
			Debug.Assert(separator is null || char.IsPunctuation(separator.Value), "Separator is expected to be punctuation.");

			this.lowercase = lowercase;
			this.separator = separator;
			this.capitalizeFirstLetterOfFirstWord = capitalizeFirstLetterOfFirstWord;
			this.capitalizeFirstLetterOfSubsequentWords = capitalizeFirstLetterOfSubsequentWords;
		}

		private enum SeparatorState
		{
			NotStarted,
			UppercaseLetter,
			LowercaseLetterOrDigit,
			SpaceSeparator,
		}

		public sealed override string ConvertName(string name)
		{
			Requires.NotNull(name);

			return ConvertNameCore(this.separator, this.lowercase, this.capitalizeFirstLetterOfSubsequentWords, this.capitalizeFirstLetterOfFirstWord, name.AsSpan());
		}

		private static string ConvertNameCore(char? separator, bool lowercase, bool capitalizeFirstLetterOfSubsequentWords, bool capitalizeFirstLetterOfFirstWord, ReadOnlySpan<char> chars)
		{
			char[]? rentedBuffer = null;

			// While we can't predict the expansion factor of the resultant string,
			// start with a buffer that is at least 20% larger than the input.
			int initialBufferLength = (int)(1.2 * chars.Length);
			Span<char> destination = initialBufferLength <= StackallocCharThreshold
				? stackalloc char[StackallocCharThreshold]
				: (rentedBuffer = ArrayPool<char>.Shared.Rent(initialBufferLength));

			SeparatorState state = SeparatorState.NotStarted;
			int charsWritten = 0;
			bool nextCharacterStartsFirstWord = true;
			bool nextCharacterStartsSubsequentWord = false;

			for (int i = 0; i < chars.Length; i++)
			{
				// NB this implementation does not handle surrogate pair letters
				// cf. https://github.com/dotnet/runtime/issues/90352
				char current = chars[i];
				UnicodeCategory category = char.GetUnicodeCategory(current);

				switch (category)
				{
					case UnicodeCategory.UppercaseLetter:

						switch (state)
						{
							case SeparatorState.NotStarted:
								break;

							case SeparatorState.LowercaseLetterOrDigit:
							case SeparatorState.SpaceSeparator:
								// An uppercase letter following a sequence of lowercase letters or spaces
								// denotes the start of a new grouping: emit a separator character.
								if (separator.HasValue)
								{
									WriteChar(separator.Value, ref destination);
								}

								nextCharacterStartsSubsequentWord = true;
								break;

							case SeparatorState.UppercaseLetter:
								// We are reading through a sequence of two or more uppercase letters.
								// Uppercase letters are grouped together with the exception of the
								// final letter, assuming it is followed by lowercase letters.
								// For example, the value 'XMLReader' should render as 'xml_reader',
								// however 'SHA512Hash' should render as 'sha512-hash'.
								if (i + 1 < chars.Length && char.IsLower(chars[i + 1]))
								{
									if (separator.HasValue)
									{
										WriteChar(separator.Value, ref destination);
									}

									nextCharacterStartsSubsequentWord = true;
								}

								break;

							default:
								Debug.Fail($"Unexpected state {state}");
								break;
						}

						if (lowercase && !((nextCharacterStartsSubsequentWord && capitalizeFirstLetterOfSubsequentWords) || (nextCharacterStartsFirstWord && capitalizeFirstLetterOfFirstWord)))
						{
							current = char.ToLowerInvariant(current);
						}

						WriteChar(current, ref destination);
						nextCharacterStartsFirstWord = false;
						nextCharacterStartsSubsequentWord = false;
						state = SeparatorState.UppercaseLetter;
						break;

					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.DecimalDigitNumber:

						if (state is SeparatorState.SpaceSeparator)
						{
							if (separator.HasValue)
							{
								// Normalize preceding spaces to one separator.
								WriteChar(separator.Value, ref destination);
							}

							nextCharacterStartsSubsequentWord = true;
						}

						bool shouldBeUpperCase = !lowercase ||
							((nextCharacterStartsSubsequentWord && capitalizeFirstLetterOfSubsequentWords) ||
							(nextCharacterStartsFirstWord && capitalizeFirstLetterOfFirstWord));
						if (shouldBeUpperCase && category is UnicodeCategory.LowercaseLetter)
						{
							current = char.ToUpperInvariant(current);
						}

						WriteChar(current, ref destination);
						nextCharacterStartsFirstWord = false;
						nextCharacterStartsSubsequentWord = false;
						state = SeparatorState.LowercaseLetterOrDigit;
						break;

					case UnicodeCategory.SpaceSeparator:
						// Space characters are trimmed from the start and end of the input string
						// but are normalized to separator characters if between letters.
						if (state != SeparatorState.NotStarted)
						{
							state = SeparatorState.SpaceSeparator;
						}

						break;

					default:
						// Non-alphanumeric characters (including the separator character and surrogates)
						// are written as-is to the output and reset the separator state.
						// E.g. 'ABC???def' maps to 'abc???def' in snake_case.
						WriteChar(current, ref destination);
						state = SeparatorState.NotStarted;
						break;
				}
			}

			string result = destination.Slice(0, charsWritten).ToString();

			if (rentedBuffer is not null)
			{
				destination.Slice(0, charsWritten).Clear();
				ArrayPool<char>.Shared.Return(rentedBuffer);
			}

			return result;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void WriteChar(char value, ref Span<char> destination)
			{
				if (charsWritten == destination.Length)
				{
					ExpandBuffer(ref destination);
				}

				destination[charsWritten++] = value;
			}

			void ExpandBuffer(ref Span<char> destination)
			{
				int newSize = checked(destination.Length * 2);
				char[] newBuffer = ArrayPool<char>.Shared.Rent(newSize);
				destination.CopyTo(newBuffer);

				if (rentedBuffer is not null)
				{
					destination.Slice(0, charsWritten).Clear();
					ArrayPool<char>.Shared.Return(rentedBuffer);
				}

				rentedBuffer = newBuffer;
				destination = rentedBuffer;
			}
		}
	}

	private class CamelCaseNamingPolicy() : BuiltInPolicy(lowercase: true, capitalizeFirstLetterOfSubsequentWords: true);

	private class PascalCaseNamingPolicy() : BuiltInPolicy(lowercase: true, capitalizeFirstLetterOfFirstWord: true, capitalizeFirstLetterOfSubsequentWords: true);

	private class KebabLowerCaseNamingPolicy() : BuiltInPolicy(lowercase: true, separator: '-');

	private class KebabUpperCaseNamingPolicy() : BuiltInPolicy(lowercase: false, separator: '-');

	private class SnakeLowerCaseNamingPolicy() : BuiltInPolicy(lowercase: true, separator: '_');

	private class SnakeUpperCaseNamingPolicy() : BuiltInPolicy(lowercase: false, separator: '_');
}
