#nullable enable
using System.Net;
using FatCat.Testing;
using FatCat.Testing.Comparers;
using FatCat.Testing.Objects;
using FatCat.Testing.Strings;
using FatCat.Toolkit.Web;

namespace FatCat.Toolkit.Testing;

public static class FatCatWebResponseAssertionsExtensions
{
	public static FatWebResponseAssertions Should(this Task<FatWebResponse> task)
	{
		var result = task.Result;

		return new FatWebResponseAssertions(result);
	}

	public static FatWebResponseAssertions Should(this FatWebResponse response)
	{
		return new FatWebResponseAssertions(response);
	}
}

public class FatWebResponseAssertions(FatWebResponse result) : ComparerBase<FatWebResponse, FatWebResponseAssertions>(result)
{
	private ObjectComparer<FatWebResponse> SubjectAsObject
	{
		get { return new ObjectComparer<FatWebResponse>(Subject); }
	}

	public FatWebResponseAssertions Be(FatWebResponse expectedResult)
	{
		SubjectAsObject.BeEquivalentTo(expectedResult);

		return this;
	}

	public FatWebResponseAssertions Be<T>(T expectedValue)
	{
		SubjectAsObject.Not.BeNull();

		new ObjectComparer<object>(Subject.To<T>()!).BeEquivalentTo(expectedValue!);

		return this;
	}

	public FatWebResponseAssertions BeBadRequest()
	{
		return HaveStatusCode(HttpStatusCode.BadRequest);
	}

	public FatWebResponseAssertions BeConflict()
	{
		return HaveStatusCode(HttpStatusCode.Conflict);
	}

	public FatWebResponseAssertions BeEmptyListOf<T>()
	{
		SubjectAsObject.Not.BeNull();

		var list = Subject.To<List<T>>();

		list.Should().BeEmpty();

		return this;
	}

	public FatWebResponseAssertions BeEquivalentTo(FatWebResponse expectedResult)
	{
		SubjectAsObject.BeEquivalentTo(expectedResult);

		return this;
	}

	public FatWebResponseAssertions BeEquivalentTo<T>(T expectedValue)
	{
		new ObjectComparer<object>(Subject.To<T>()!).BeEquivalentTo(expectedValue!);

		return this;
	}

	public FatWebResponseAssertions BeNotAcceptable()
	{
		return HaveStatusCode(HttpStatusCode.NotAcceptable);
	}

	public FatWebResponseAssertions BeNotFound()
	{
		return HaveStatusCode(HttpStatusCode.NotFound);
	}

	public FatWebResponseAssertions BeOk()
	{
		return HaveOneOfStatusCode([HttpStatusCode.OK, HttpStatusCode.NoContent]);
	}

	public FatWebResponseAssertions BeSuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsSuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseAssertions BeUnauthorized()
	{
		return HaveStatusCode(HttpStatusCode.Unauthorized);
	}

	public FatWebResponseAssertions BeUnsuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsUnsuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseAssertions For<T>(Action<T> action)
	{
		SubjectAsObject.Not.BeNull();

		action(Subject.To<T>()!);

		return this;
	}

	public FatWebResponseAssertions ForList<T>(Action<List<T>> action)
	{
		SubjectAsObject.Not.BeNull();

		action(Subject.To<List<T>>()!);

		return this;
	}

	public FatWebResponseAssertions HaveContent(string content)
	{
		SubjectAsObject.Not.BeNull();

		Subject.Content.Should().Be(content);

		return this;
	}

	public FatWebResponseAssertions HaveContentEquivalentTo<TContentType>(TContentType expectedContent)
	{
		SubjectAsObject.Not.BeNull("FatWebResponse should never be null");

		HaveStatusCode(
			HttpStatusCode.OK,
			$"you cannot test for content from an unsuccessful status code: {Subject.StatusCode}"
		);

		new ObjectComparer<object>(Subject.To<TContentType>()!).BeEquivalentTo(expectedContent!);

		return this;
	}

	public FatWebResponseAssertions HaveContentTypeOf(string contentType)
	{
		SubjectAsObject.Not.BeNull();

		Subject.ContentType.Should().Be(contentType);

		return this;
	}

	public FatWebResponseAssertions HaveNoContent()
	{
		return HaveStatusCode(HttpStatusCode.NoContent);
	}

	public FatWebResponseAssertions HaveStatusCode(HttpStatusCode statusCode, string? because = null)
	{
		return HaveOneOfStatusCode([statusCode], because);
	}

	public FatWebResponseAssertions WithMessage(string expectedMessage, string? because = null)
	{
		Subject.Content.Should().Match(expectedMessage, Options.IgnoreCase, because);

		return this;
	}

	private FatWebResponseAssertions HaveOneOfStatusCode(HttpStatusCode[] acceptableStatusCodes, string? because = null)
	{
		SubjectAsObject.Not.BeNull();

		Subject.StatusCode.Should().BeOneOf(acceptableStatusCodes, because);

		return this;
	}
}
