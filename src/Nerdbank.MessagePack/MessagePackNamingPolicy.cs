// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Nerdbank.MessagePack;

/// <summary>
/// Defines a transformation for property names from .NET to msgpack.
/// </summary>
public abstract class MessagePackNamingPolicy
{
	/// <summary>
	/// Gets a naming policy that converts a .NET PascalCase property to camelCase for msgpack.
	/// </summary>
	public static MessagePackNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

	/// <summary>
	/// Gets a naming policy that converts a .NET camelCase property to PascalCase for msgpack.
	/// </summary>
	public static MessagePackNamingPolicy PascalCase { get; } = new PascalCaseNamingPolicy();

	/// <summary>
	/// Transforms a property name as defined in .NET to a property name as it should be serialized to MessagePack.
	/// </summary>
	/// <param name="name">The .NET property name.</param>
	/// <returns>The msgpack property name.</returns>
	public abstract string ConvertName(string name);

	private static string ConvertName(string name, bool toCamelCase)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}

#if NET
		return string.Create(name.Length, (name, toCamelCase), static (span, state) =>
		{
			bool firstWord = true;
			int i = 0;
			int outputPosition = 0;
			while (i < state.name.Length)
			{
				if (!char.IsLetterOrDigit(state.name[i]))
				{
					span[outputPosition++] = state.name[i++];
					firstWord = true;
					continue;
				}

				int wordLength = 1;
				while (i + wordLength < state.name.Length && char.IsLower(state.name[i + wordLength]))
				{
					wordLength++;
				}

				if (wordLength == 1)
				{
					while (i + wordLength < state.name.Length && char.IsUpper(state.name[i + wordLength]) && (i + wordLength + 1 >= state.name.Length || !char.IsLower(state.name[i + wordLength + 1])))
					{
						wordLength++;
					}
				}

				ReadOnlySpan<char> word = state.name.AsSpan(i, wordLength);
				if (firstWord)
				{
					if (state.toCamelCase)
					{
						for (int j = 0; j < word.Length; j++)
						{
							span[outputPosition++] = char.ToLowerInvariant(word[j]);
						}
					}
					else
					{
						span[outputPosition++] = char.ToUpperInvariant(word[0]);
						word.Slice(1).CopyTo(span.Slice(outputPosition));
						outputPosition += word.Length - 1;
					}

					firstWord = false;
				}
				else
				{
					span[outputPosition++] = char.ToUpperInvariant(word[0]);
					word.Slice(1).CopyTo(span.Slice(outputPosition));
					outputPosition += word.Length - 1;
				}

				i += wordLength;
			}

			span.Slice(outputPosition).Clear();
		});
#else
		StringBuilder sb = new StringBuilder(name.Length);
		bool firstWord = true;
		int i = 0;
		while (i < name.Length)
		{
			if (!char.IsLetterOrDigit(name[i]))
			{
				sb.Append(name[i++]);
				firstWord = true;
				continue;
			}

			int wordLength = 1;
			while (i + wordLength < name.Length && char.IsLower(name[i + wordLength]))
			{
				wordLength++;
			}

			if (wordLength == 1)
			{
				while (i + wordLength < name.Length && char.IsUpper(name[i + wordLength]) && (i + wordLength + 1 >= name.Length || !char.IsLower(name[i + wordLength + 1])))
				{
					wordLength++;
				}
			}

			string word = name.Substring(i, wordLength);
			if (firstWord)
			{
				if (toCamelCase)
				{
					sb.Append(word.ToLowerInvariant());
				}
				else
				{
					sb.Append(char.ToUpperInvariant(word[0]));
					sb.Append(word.Substring(1));
				}

				firstWord = false;
			}
			else
			{
				sb.Append(char.ToUpperInvariant(word[0]));
				sb.Append(word.Substring(1));
			}

			i += wordLength;
		}

		return sb.ToString();
#endif
	}

	private class CamelCaseNamingPolicy : MessagePackNamingPolicy
	{
		public override string ConvertName(string name) => ConvertName(name, toCamelCase: true);
	}

	private class PascalCaseNamingPolicy : MessagePackNamingPolicy
	{
		public override string ConvertName(string name) => ConvertName(name, toCamelCase: false);
	}
}
