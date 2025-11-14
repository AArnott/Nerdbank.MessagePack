// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.AspNetCoreMvcFormatter;
using PolyType;
using Xunit;

public partial class AspNetCoreMvcFormatterTest
{
	private const string MsgPackContentType = "application/x-msgpack";

	private readonly MessagePackSerializer serializer = new();
	private readonly MessagePackOutputFormatter formatter = new(Witness.GeneratedTypeShapeProvider);
	private readonly MessagePackInputFormatter deformatter = new(Witness.GeneratedTypeShapeProvider);

	[Fact]
	public async Task MessagePackFormatter()
	{
		var person = new User
		{
			UserId = 1,
			FullName = "John Denver",
			Age = 35,
			Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
		};

		byte[] messagePackBinary = this.serializer.Serialize(person, PolyType.SourceGenerator.TypeShapeProvider_Nerdbank_MessagePack_AspNetCoreMvcFormatter_Tests.Default.User, TestContext.Current.CancellationToken);

		// OutputFormatter
		OutputFormatterWriteContext outputFormatterContext = GetOutputFormatterContext(person, typeof(User), MsgPackContentType);

		Assert.True(this.formatter.CanWriteResult(outputFormatterContext));

		await this.formatter.WriteAsync(outputFormatterContext);

		Stream body = outputFormatterContext.HttpContext.Response.Body;

		Assert.NotNull(body);
		body.Position = 0;

		using (var ms = new MemoryStream())
		{
			await body.CopyToAsync(ms, TestContext.Current.CancellationToken);
			Assert.Equal(messagePackBinary, ms.ToArray());
		}

		// InputFormatter
		var httpContext = new DefaultHttpContext();
		httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
		httpContext.Request.Body = new NonSeekableReadStream(messagePackBinary);
		httpContext.Request.ContentType = MsgPackContentType;

		InputFormatterContext inputFormatterContext = this.CreateInputFormatterContext(typeof(User), httpContext);

		Assert.True(this.deformatter.CanRead(inputFormatterContext));

		InputFormatterResult result = await this.deformatter.ReadAsync(inputFormatterContext);

		Assert.False(result.HasError);

		User userModel = Assert.IsType<User>(result.Model);
		Assert.Equal(userModel, person, StructuralEqualityComparer.GetDefault(PolyType.SourceGenerator.TypeShapeProvider_Nerdbank_MessagePack_AspNetCoreMvcFormatter_Tests.Default.User));
	}

	[Fact]
	public void MessagePackFormatterCanNotRead()
	{
		var person = new User();

		// OutputFormatter
		OutputFormatterWriteContext outputFormatterContext = GetOutputFormatterContext(person, typeof(User), "application/json");

		Assert.False(this.formatter.CanWriteResult(outputFormatterContext));

		// InputFormatter
		var httpContext = new DefaultHttpContext();
		httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
		httpContext.Request.Body = new NonSeekableReadStream("{}"u8.ToArray());
		httpContext.Request.ContentType = "application/json";

		InputFormatterContext inputFormatterContext = this.CreateInputFormatterContext(typeof(User), httpContext);

		Assert.False(this.deformatter.CanRead(inputFormatterContext));
	}

	[Fact]
	public void MessagePackOutputFormatterSupportsXMsgPack()
	{
		Assert.Equal(MsgPackContentType, this.formatter.SupportedMediaTypes.Single());
	}

	[Fact]
	public void MessagePackInputFormatterSupportsXMsgPack()
	{
		Assert.Equal(MsgPackContentType, this.deformatter.SupportedMediaTypes.Single());
	}

	/// <summary>
	/// <see href="https://github.com/aspnet/Mvc/blob/master/test/Microsoft.AspNetCore.Mvc.Formatters.Json.Test/JsonOutputFormatterTests.cs#L453">JsonOutputFormatterTests.cs#L453</see>.
	/// </summary>
	private static OutputFormatterWriteContext GetOutputFormatterContext(
		object outputValue,
		Type outputType,
		string contentType = "application/xml; charset=utf-8",
		MemoryStream? responseStream = null)
	{
		var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);

		ActionContext actionContext = GetActionContext(mediaTypeHeaderValue, responseStream);
		return new OutputFormatterWriteContext(
			actionContext.HttpContext,
			new TestHttpResponseStreamWriterFactory().CreateWriter,
			outputType,
			outputValue)
		{
			ContentType = new StringSegment(contentType),
		};
	}

	/// <summary>
	/// <see href="https://github.com/aspnet/Mvc/blob/master/test/Microsoft.AspNetCore.Mvc.Formatters.Json.Test/JsonOutputFormatterTests.cs#L472">JsonOutputFormatterTests.cs#L472</see>.
	/// </summary>
	private static ActionContext GetActionContext(
		MediaTypeHeaderValue contentType,
		MemoryStream? responseStream = null)
	{
		var request = new Mock<HttpRequest>();
		var headers = new HeaderDictionary();
		request.Setup(r => r.ContentType).Returns(contentType.ToString());
		request.SetupGet(r => r.Headers).Returns(headers);
		headers[HeaderNames.AcceptCharset] = contentType.Charset.ToString();
		var response = new Mock<HttpResponse>();
		response.SetupGet(f => f.Body).Returns(responseStream ?? new MemoryStream());
		var httpContext = new Mock<HttpContext>();
		httpContext.SetupGet(c => c.Request).Returns(request.Object);
		httpContext.SetupGet(c => c.Response).Returns(response.Object);
		return new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
	}

	/// <summary>
	/// <see href="https://github.com/aspnet/Mvc/blob/master/test/Microsoft.AspNetCore.Mvc.Formatters.Json.Test/JsonInputFormatterTest.cs#L717">JsonInputFormatterTest.cs#L717</see>.
	/// </summary>
	private InputFormatterContext CreateInputFormatterContext(
		Type modelType,
		HttpContext httpContext,
		string? modelName = null,
		bool treatEmptyInputAsDefaultValue = false)
	{
		var provider = new EmptyModelMetadataProvider();
		ModelMetadata metadata = provider.GetMetadataForType(modelType);

		return new InputFormatterContext(
			httpContext,
			modelName: modelName ?? string.Empty,
			modelState: new ModelStateDictionary(),
			metadata: metadata,
			readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader,
			treatEmptyInputAsDefaultValue: treatEmptyInputAsDefaultValue);
	}

	/// <summary>
	/// <see href="https://github.com/aspnet/Mvc/blob/master/test/Microsoft.AspNetCore.Mvc.Formatters.Json.Test/JsonInputFormatterTest.cs#L791">JsonInputFormatterTest.cs#L791</see>.
	/// </summary>
	private class TestResponseFeature : HttpResponseFeature
	{
		public override void OnCompleted(Func<object, Task> callback, object state)
		{
			// do not do anything
		}
	}

	/// <summary>
	/// <see href="https://github.com/aspnet/Mvc/blob/master/test/Microsoft.AspNetCore.Mvc.Core.TestCommon/TestHttpResponseStreamWriterFactory.cs">TestHttpResponseStreamWriterFactory.cs</see>.
	/// </summary>
	private class TestHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
	{
		private const int DefaultBufferSize = 16 * 1024;

		public TextWriter CreateWriter(Stream stream, Encoding encoding)
		{
			return new HttpResponseStreamWriter(stream, encoding, DefaultBufferSize);
		}
	}

	/// <summary>
	/// <see href="https://github.com/aspnet/Mvc/blob/master/test/Microsoft.AspNetCore.Mvc.Core.TestCommon/TestHttpRequestStreamReaderFactory.cs">TestHttpRequestStreamReaderFactory.cs</see>.
	/// </summary>
	private class TestHttpRequestStreamReaderFactory : IHttpRequestStreamReaderFactory
	{
		public TextReader CreateReader(Stream stream, Encoding encoding)
		{
			return new HttpRequestStreamReader(stream, encoding);
		}
	}

	/// <summary>
	/// <see href="https://github.com/aspnet/Mvc/blob/master/test/Microsoft.AspNetCore.Mvc.Core.TestCommon/NonSeekableReadableStream.cs">NonSeekableReadableStream.cs</see>.
	/// </summary>
	private class NonSeekableReadStream : Stream
	{
		private readonly Stream inner;

		public NonSeekableReadStream(byte[] data)
			: this(new MemoryStream(data))
		{
		}

		public NonSeekableReadStream(Stream inner) => this.inner = inner;

		public override bool CanRead => this.inner.CanRead;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public override void Flush() => throw new NotImplementedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override int Read(byte[] buffer, int offset, int count) => this.inner.Read(buffer, offset, count);

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => this.inner.ReadAsync(buffer, offset, count, cancellationToken);

		public override void Close() => this.inner.Close();

		protected override void Dispose(bool disposing) => this.inner.Dispose();
	}

	[GenerateShapeFor<User>]
	private partial class Witness;
}
