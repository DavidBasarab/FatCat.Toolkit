#nullable enable
using System.Net;
using FatCat.Testing;
using FatCat.Testing.Comparers;
using FatCat.Testing.Objects;
using FatCat.Testing.Strings;

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
	: ComparerBase<WebResult<T>, WebResultClosedOverAssertions<T>>(result)
	where T : class
{
	private ObjectComparer<WebResult<T>> SubjectAsObject
	{
		get { return new ObjectComparer<WebResult<T>>(Subject); }
	}

	public WebResultClosedOverAssertions<T> Be(WebResult<T> expectedResult)
	{
		SubjectAsObject.BeEquivalentTo(expectedResult);

		return this;
	}

	public WebResultClosedOverAssertions<T> Be(T expectedValue)
	{
		SubjectAsObject.Not.BeNull();

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
		SubjectAsObject.Not.BeNull();

		var list = Subject.Data as List<T>;

		new ObjectComparer<List<T>>(list!).Not.BeNull();

		list!.Should().BeEmpty();

		return this;
	}

	public WebResultClosedOverAssertions<T> BeEquivalentTo(WebResult expectedResult)
	{
		new ObjectComparer<object>(Subject).BeEquivalentTo(expectedResult);

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
		return HaveOneOfStatusCode([HttpStatusCode.OK, HttpStatusCode.NoContent]);
	}

	public WebResultClosedOverAssertions<T> BeSuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsSuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public WebResultClosedOverAssertions<T> BeUnsuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsUnsuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public WebResultClosedOverAssertions<T> For(Action<T> action)
	{
		SubjectAsObject.Not.BeNull();

		action(Subject.Data!);

		return this;
	}

	public WebResultClosedOverAssertions<T> ForList(Action<List<T>> action)
	{
		SubjectAsObject.Not.BeNull();

		var list = Subject.Data as List<T>;

		action(list!);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveContent(string content)
	{
		SubjectAsObject.Not.BeNull();

		Subject.Content.Should().Be(content);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveContentEquivalentTo(T expectedContent)
	{
		SubjectAsObject.Not.BeNull("WebResult should never be null");

		HaveStatusCode(
			HttpStatusCode.OK,
			$"you cannot test for content from an unsuccessful status code: {Subject.StatusCode}"
		);

		Subject.Data.Should().BeEquivalentTo(expectedContent);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveContentTypeOf(string contentType)
	{
		SubjectAsObject.Not.BeNull();

		Subject.ContentType.Should().Be(contentType);

		return this;
	}

	public WebResultClosedOverAssertions<T> HaveNoContent()
	{
		return HaveStatusCode(HttpStatusCode.NoContent);
	}

	public WebResultClosedOverAssertions<T> HaveStatusCode(HttpStatusCode statusCode, string? because = null)
	{
		return HaveOneOfStatusCode([statusCode], because);
	}

	public WebResultClosedOverAssertions<T> WithMessage(string expectedMessage, string? because = null)
	{
		Subject.Content.Should().Match(expectedMessage, Options.IgnoreCase, because);

		return this;
	}

	private WebResultClosedOverAssertions<T> HaveOneOfStatusCode(
		HttpStatusCode[] acceptableStatusCodes,
		string? because = null
	)
	{
		SubjectAsObject.Not.BeNull();

		Subject.StatusCode.Should().BeOneOf(acceptableStatusCodes, because);

		return this;
	}
}
