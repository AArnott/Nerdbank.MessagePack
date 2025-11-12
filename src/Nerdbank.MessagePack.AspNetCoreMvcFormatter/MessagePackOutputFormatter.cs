// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Mvc.Formatters;
using PolyType;
using PolyType.ReflectionProvider;

namespace Nerdbank.MessagePack.AspNetCoreMvcFormatter;

/// <summary>
/// An output formatter for ASP.NET Core MVC that serializes strongly-typed objects into MessagePack content.
/// </summary>
/// <remarks>
/// This formatter supports the "application/x-msgpack" media type and uses the provided <see cref="ITypeShapeProvider"/>
/// to resolve type shapes for serialization with the specified <see cref="MessagePackSerializer"/>.
/// </remarks>
public class MessagePackOutputFormatter : OutputFormatter
{
	private const string ContentType = MessagePackInputFormatter.ContentType;

	private static readonly StreamPipeWriterOptions PipeWriterOptions = new(leaveOpen: true);

	private readonly ITypeShapeProvider typeShapeProvider;
	private readonly MessagePackSerializer serializer;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackOutputFormatter"/> class.
	/// </summary>
	/// <param name="typeShapeProvider"><inheritdoc cref="MessagePackOutputFormatter(ITypeShapeProvider, MessagePackSerializer)" path="/param[@name='typeShapeProvider']"/></param>
	/// <remarks>
	/// The <see cref="MessagePackSerializer"/> this constructor initializes includes converters commonly useful to ASP.NET Core MVC scenarios,
	/// including <see cref="OptionalConverters.WithObjectConverter(MessagePackSerializer)"/>.
	/// </remarks>
	public MessagePackOutputFormatter(ITypeShapeProvider typeShapeProvider)
		: this(typeShapeProvider, new MessagePackSerializer().WithObjectConverter())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackOutputFormatter"/> class with the specified MessagePack serializer.
	/// </summary>
	/// <param name="typeShapeProvider">
	/// The type shape provider used to resolve type shapes for serialization.
	/// This may be <see cref="ReflectionTypeShapeProvider.Default"/> but
	/// is recommended to be a source generated type shape provider for better startup performance.
	/// </param>
	/// <param name="serializer">The serializer to use.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> is <see langword="null"/>.</exception>
	public MessagePackOutputFormatter(ITypeShapeProvider typeShapeProvider, MessagePackSerializer serializer)
	{
		this.typeShapeProvider = typeShapeProvider ?? throw new ArgumentNullException(nameof(typeShapeProvider));
		this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		this.SupportedMediaTypes.Add(ContentType);
	}

	/// <inheritdoc/>
	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
	{
		if (context is null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		PipeWriter? writer;

		if (context.Object is null)
		{
#if NETSTANDARD2_0
			context.HttpContext.Response.Body.WriteByte(MessagePackCode.Nil);
			return;
#else
			writer = context.HttpContext.Response.BodyWriter;
			if (writer is null)
			{
				context.HttpContext.Response.Body.WriteByte(MessagePackCode.Nil);
				return;
			}

			Span<byte> span = writer.GetSpan(1);
			span[0] = MessagePackCode.Nil;
			writer.Advance(1);
			await writer.FlushAsync();
			return;
#endif
		}

		Type objectType = context.ObjectType is null || context.ObjectType == typeof(object) ? context.Object.GetType() : context.ObjectType;
		ITypeShape shape = this.typeShapeProvider.GetTypeShapeOrThrow(objectType);
		bool writerOwned = false;

#if NETSTANDARD2_0
		writer = PipeWriter.Create(context.HttpContext.Response.Body, PipeWriterOptions);
		writerOwned = true;
#else
		writer = context.HttpContext.Response.BodyWriter;
		if (writer is null)
		{
			writer = PipeWriter.Create(context.HttpContext.Response.Body, PipeWriterOptions);
			writerOwned = true;
		}
#endif

		await this.serializer.SerializeObjectAsync(writer, context.Object, shape, context.HttpContext.RequestAborted);

		if (writerOwned)
		{
			await writer.CompleteAsync();
		}
		else
		{
			await writer.FlushAsync();
		}
	}
}
