// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes <see cref="Exception"/> objects.
/// </summary>
[GenerateShapeFor<string>]
internal partial class ExceptionConverter : MessagePackConverter<Exception>
{
	private static readonly MessagePackString Message = new(nameof(Exception.Message));

	/// <inheritdoc/>
	public override Exception? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		MessagePackConverter<string> stringConverter = context.GetConverter<string, ExceptionConverter>();

		int propertyCount = reader.ReadMapHeader();
		string? message = null;
		for (int i = 0; i < propertyCount; i++)
		{
			if (Message.TryRead(ref reader))
			{
				message = stringConverter.Read(ref reader, context);
			}
			else
			{
				// skip over unrecognized property names and their values.
				reader.Skip(context);
				reader.Skip(context);
			}
		}

		return new Exception(message);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Exception? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		MessagePackConverter<string> stringConverter = context.GetConverter<string, ExceptionConverter>();

		writer.WriteMapHeader(1);
		writer.Write(Message);
		stringConverter.Write(ref writer, value.Message, context);
	}
}
