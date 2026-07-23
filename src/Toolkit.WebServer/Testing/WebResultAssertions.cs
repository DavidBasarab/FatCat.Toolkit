#nullable enable
using System.Net;
using FatCat.Testing;
using FatCat.Testing.Comparers;
using FatCat.Testing.Objects;
using FatCat.Testing.Strings;

namespace FatCat.Toolkit.WebServer.Testing;

public static class WebResultAssertionsExtensions
{
	public static WebResultAssertions Should(this Task<WebResult> task)
	{
		var result = task.Result;

		return new WebResultAssertions(result);
	}

	public static WebResultAssertions Should(this WebResult webResult)
	{
		return new WebResultAssertions(webResult);
	}
}

public class WebResultAssertions(WebResult result) : ComparerBase<WebResult, WebResultAssertions>(result)
{
	private ObjectComparer<WebResult> SubjectAsObject
	{
		get { return new ObjectComparer<WebResult>(Subject); }
	}

	public WebResultAssertions Be(WebResult expectedResult)
	{
		SubjectAsObject.BeEquivalentTo(expectedResult);

		return this;
	}

	public WebResultAssertions Be<T>(T expectedValue)
	{
		SubjectAsObject.Not.BeNull();

		new ObjectComparer<object>(Subject.To<T>()!).BeEquivalentTo(expectedValue!);

		return this;
	}

	public WebResultAssertions BeBadRequest()
	{
		return HaveStatusCode(HttpStatusCode.BadRequest);
	}

	public WebResultAssertions BeBadRequest(string fieldName, string messageId)
	{
		var expectedResult = WebResult.BadRequest(fieldName, messageId);

		return HaveStatusCode(HttpStatusCode.BadRequest).Be(expectedResult);
	}

	public WebResultAssertions BeBadRequest(string messageId)
	{
		var expectedResult = WebResult.BadRequest(messageId);

		return HaveStatusCode(HttpStatusCode.BadRequest).Be(expectedResult);
	}

	public WebResultAssertions BeConflict()
	{
		return HaveStatusCode(HttpStatusCode.Conflict);
	}

	public WebResultAssertions BeEmptyListOf<T>()
	{
		SubjectAsObject.Not.BeNull();

		var list = Subject.To<List<T>>();

		list.Should().BeEmpty();

		return this;
	}

	public WebResultAssertions BeEquivalentTo(WebResult expectedResult)
	{
		SubjectAsObject.BeEquivalentTo(expectedResult);

		return this;
	}

	public WebResultAssertions BeEquivalentTo<T>(T expectedValue)
	{
		new ObjectComparer<object>(Subject.To<T>()!).BeEquivalentTo(expectedValue!);

		return this;
	}

	public WebResultAssertions BeNotAcceptable()
	{
		return HaveStatusCode(HttpStatusCode.NotAcceptable);
	}

	public WebResultAssertions BeNotFound()
	{
		return HaveStatusCode(HttpStatusCode.NotFound);
	}

	public WebResultAssertions BeOk()
	{
		return HaveOneOfStatusCode([HttpStatusCode.OK, HttpStatusCode.NoContent]);
	}

	public WebResultAssertions BeSuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsSuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public WebResultAssertions BeUnauthorized()
	{
		return HaveStatusCode(HttpStatusCode.Unauthorized);
	}

	public WebResultAssertions BeUnsuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsUnsuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public WebResultAssertions For<T>(Action<T> action)
	{
		SubjectAsObject.Not.BeNull();

		action(Subject.To<T>()!);

		return this;
	}

	public WebResultAssertions ForList<T>(Action<List<T>> action)
	{
		SubjectAsObject.Not.BeNull();

		action(Subject.To<List<T>>()!);

		return this;
	}

	public WebResultAssertions HaveContent(string content)
	{
		SubjectAsObject.Not.BeNull();

		Subject.Content.Should().Be(content);

		return this;
	}

	public WebResultAssertions HaveContentEquivalentTo<TContentType>(TContentType expectedContent)
	{
		SubjectAsObject.Not.BeNull("WebResult should never be null");

		HaveStatusCode(
			HttpStatusCode.OK,
			$"you cannot test for content from an unsuccessful status code: {Subject.StatusCode}"
		);

		new ObjectComparer<object>(Subject.To<TContentType>()!).BeEquivalentTo(expectedContent!);

		return this;
	}

	public WebResultAssertions HaveContentTypeOf(string contentType)
	{
		SubjectAsObject.Not.BeNull();

		Subject.ContentType.Should().Be(contentType);

		return this;
	}

	public WebResultAssertions HaveNoContent()
	{
		return HaveStatusCode(HttpStatusCode.NoContent);
	}

	public WebResultAssertions HaveStatusCode(HttpStatusCode statusCode, string? because = null)
	{
		return HaveOneOfStatusCode([statusCode], because);
	}

	public WebResultAssertions WithMessage(string expectedMessage, string? because = null)
	{
		Subject.Content.Should().Match(expectedMessage, Options.IgnoreCase, because);

		return this;
	}

	private WebResultAssertions HaveOneOfStatusCode(HttpStatusCode[] acceptableStatusCodes, string? because = null)
	{
		SubjectAsObject.Not.BeNull();

		Subject.StatusCode.Should().BeOneOf(acceptableStatusCodes, because);

		return this;
	}
}
