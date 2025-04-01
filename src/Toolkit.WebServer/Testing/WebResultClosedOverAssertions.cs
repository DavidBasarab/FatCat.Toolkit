#nullable enable
using System.Net;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Primitives;

namespace FatCat.Toolkit.WebServer.Testing;

public static class WebResultClosedOverAssertions
{
	public static WebResultClosedOverAssertions<T> Should<T>(this Task<WebResult<T>> task)
		where T : class
	{
		var result = task.Result;

		return new WebResultClosedOverAssertions<T>(result);
	}

	public static WebResultClosedOverAssertions<T> Should<T>(this WebResult<T> webResult)
		where T : class
	{
		return new WebResultClosedOverAssertions<T>(webResult);
	}
}

public class WebResultClosedOverAssertions<T>(WebResult<T> result)
	: ReferenceTypeAssertions<WebResult<T>, WebResultClosedOverAssertions<T>>(result)
	where T : class
{
	protected override string Identifier
	{
		get => "Web Results assertions";
	}

	public WebResultClosedOverAssertions<T> Be(WebResult<T> expectedResult)
	{
		new ObjectAssertions(Subject).BeEquivalentTo(expectedResult);

		return this;
	}

	public WebResultClosedOverAssertions<T> Be(T expectedValue)
	{
		Subject.Should().NotBeNull();

		Subject.Data.Should().BeEquivalentTo(expectedValue);

		return this;
	}

	public WebResultClosedOverAssertions<T> BeBadRequest()
	{
		return HaveStatusCode(HttpStatusCode.BadRequest);
	}

	public WebResultClosedOverAssertions<T> BeBadRequest(string fieldName, string messageId)
	{
		var expectedResult = new WebResult<T>(WebResult.BadRequest(fieldName, messageId));

		return HaveStatusCode(HttpStatusCode.BadRequest).Be(expectedResult);
	}

	public WebResultClosedOverAssertions<T> BeBadRequest(string messageId)
	{
		var expectedResult = new WebResult<T>(WebResult.BadRequest(messageId));

		return HaveStatusCode(HttpStatusCode.BadRequest).Be(expectedResult);
	}

	public WebResultClosedOverAssertions<T> BeConflict()
	{
		return HaveStatusCode(HttpStatusCode.Conflict);
	}

	public WebResultClosedOverAssertions<T> BeEmptyListOf()
	{
		Subject.Should().NotBeNull();

		var list = Subject.Data as List<T>;

		list.Should().NotBeNull();

		list.Should().BeEmpty();

		return this;
	}

	public WebResultClosedOverAssertions<T> BeEquivalentTo(WebResult expectedResult)
	{
		new ObjectAssertions(Subject).BeEquivalentTo(expectedResult);

		return this;
	}

	public WebResultClosedOverAssertions<T> BeEquivalentTo(T expectedValue)
	{
		Subject.Data.Should().BeEquivalentTo(expectedValue);

		return this;
	}

	public WebResultClosedOverAssertions<T> BeNotAcceptable()
	{
		return HaveStatusCode(HttpStatusCode.NotAcceptable);
	}

	public WebResultClosedOverAssertions<T> BeNotFound()
	{
		return HaveStatusCode(HttpStatusCode.NotFound);
	}

	public WebResultClosedOverAssertions<T> BeOk()
	{
		return HaveOneOfStatusCode(new[] { HttpStatusCode.OK, HttpStatusCode.NoContent });
	}

	public WebResultClosedOverAssertions<T> BeSuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsSuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public WebResultClosedOverAssertions<T> BeUnsuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsUnsuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public WebResultClosedOverAssertions<T> For(Action<T> action)
	{
		Subject.Should().NotBeNull();

		action(Subject.Data!);

		return this;
	}

	public WebResultClosedOverAssertions<T> ForList(Action<List<T>> action)
	{
		Subject.Should().NotBeNull();

		var list = Subject.Data as List<T>;

		action(list!);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveContent(string content)
	{
		Subject.Should().NotBeNull();

		Subject.Content.Should().Be(content);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveContentEquivalentTo(T expectedContent)
	{
		return HaveContentEquivalentTo(expectedContent, config => config);
	}

	public WebResultClosedOverAssertions<T> HaveContentEquivalentTo(
		T expectedContent,
		Func<EquivalencyAssertionOptions<T>, EquivalencyAssertionOptions<T>> config
	)
	{
		Subject.Should().NotBeNull("WebResult should never be null");

		Subject
			.Should()
			.HaveStatusCode(
				HttpStatusCode.OK,
				"you cannot test for content from an unsuccessful status code: {0}",
				Subject.StatusCode
			);

		var actualContent = Subject.Data;

		actualContent.Should().BeEquivalentTo(expectedContent);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveContentTypeOf(string contentType)
	{
		Subject.Should().NotBeNull();

		Subject.ContentType.Should().Be(contentType);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveNoContent()
	{
		return HaveStatusCode(HttpStatusCode.NoContent);
	}

	public WebResultClosedOverAssertions<T> HaveStatusCode(
		HttpStatusCode statusCode,
		string? because = null,
		params object[] becauseArgs
	)
	{
		return HaveOneOfStatusCode(new[] { statusCode }, because, becauseArgs);
	}

	public WebResultClosedOverAssertions<T> WithMessage(
		string expectedMessage,
		string? because = null,
		params object[] becauseArgs
	)
	{
		Subject.Content.Should().MatchEquivalentOf(expectedMessage, because, becauseArgs);

		return this;
	}

	private WebResultClosedOverAssertions<T> HaveOneOfStatusCode(
		HttpStatusCode[] acceptableStatusCodes,
		string? because = null,
		params object[] becauseArgs
	)
	{
		Subject.Should().NotBeNull();

		Subject.StatusCode.Should().BeOneOf(acceptableStatusCodes, because, becauseArgs);

		return this;
	}
}
