// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using PolyType;
using PolyType.ReflectionProvider;

namespace Nerdbank.MessagePack.AspNetCoreMvcFormatter;

/// <summary>
/// An input formatter for ASP.NET Core MVC that deserializes MessagePack content into strongly-typed objects.
/// </summary>
/// <remarks>
/// This formatter supports the "application/x-msgpack" media type and uses the provided <see cref="ITypeShapeProvider"/>
/// to resolve type shapes for deserialization with the specified <see cref="MessagePackSerializer"/>.
/// </remarks>
public class MessagePackInputFormatter : InputFormatter, IInputFormatterExceptionPolicy
{
	/// <summary>
	/// The content type that this formatter supports for deserialization.
	/// </summary>
	internal const string ContentType = "application/x-msgpack";

	private readonly ITypeShapeProvider typeShapeProvider;
	private readonly MessagePackSerializer serializer;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackInputFormatter"/> class.
	/// </summary>
	/// <param name="typeShapeProvider"><inheritdoc cref="MessagePackInputFormatter(ITypeShapeProvider, MessagePackSerializer)" path="/param[@name='typeShapeProvider']"/></param>
	/// <remarks>
	/// The <see cref="MessagePackSerializer"/> this constructor initializes includes converters commonly useful to ASP.NET Core MVC scenarios,
	/// including <see cref="OptionalConverters.WithObjectConverter(MessagePackSerializer)"/>.
	/// </remarks>
	public MessagePackInputFormatter(ITypeShapeProvider typeShapeProvider)
		: this(typeShapeProvider, new MessagePackSerializer().WithObjectConverter())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackInputFormatter"/> class with the specified MessagePack serializer.
	/// </summary>
	/// <param name="typeShapeProvider">
	/// The type shape provider used to resolve type shapes for deserialization.
	/// This may be <see cref="ReflectionTypeShapeProvider.Default"/> but
	/// is recommended to be a source generated type shape provider for better startup performance.
	/// </param>
	/// <param name="serializer">The serializer to use.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> is <see langword="null"/>.</exception>
	public MessagePackInputFormatter(ITypeShapeProvider typeShapeProvider, MessagePackSerializer serializer)
	{
		this.typeShapeProvider = typeShapeProvider;
		this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		this.SupportedMediaTypes.Add(ContentType);
	}

	/// <inheritdoc />
	InputFormatterExceptionPolicy IInputFormatterExceptionPolicy.ExceptionPolicy => InputFormatterExceptionPolicy.MalformedInputExceptions;

	/// <summary>
	/// Reads and deserializes the request body from MessagePack format into the target model type.
	/// </summary>
	/// <param name="context">The input formatter context containing the HTTP request and model type information.</param>
	/// <returns>
	/// A task that represents the asynchronous read operation. The task result contains an <see cref="InputFormatterResult"/>
	/// indicating success and the deserialized model object.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
	/// <remarks>
	/// This method creates a <see cref="PipeReader"/> from the request body stream, resolves the type shape for the target model type,
	/// and uses the configured MessagePack serializer to deserialize the content asynchronously.
	/// </remarks>
	public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
	{
		if (context is null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		HttpRequest request = context.HttpContext.Request;
		ITypeShape shape = this.typeShapeProvider.GetTypeShapeOrThrow(context.ModelType);

		var reader = PipeReader.Create(request.Body);
		object? model;
		try
		{
			model = await this.serializer.DeserializeObjectAsync(reader, shape, context.HttpContext.RequestAborted);
		}
		catch (MessagePackSerializationException exception)
		{
			Exception modelStateException = new InputFormatterException(exception.Message, exception);
			context.ModelState.TryAddModelError(string.Empty, modelStateException, context.Metadata);
			return InputFormatterResult.Failure();
		}
		finally
		{
			await reader.CompleteAsync();
		}

		return InputFormatterResult.Success(model);
	}
}
