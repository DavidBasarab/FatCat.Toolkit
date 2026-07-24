using System.Net;
using FatCat.Testing;
using FatCat.Testing.Comparers;
using FatCat.Testing.Objects;
using FatCat.Testing.Strings;
using FatCat.Toolkit.Web;

namespace FatCat.Toolkit.Testing;

public static class FatWebResponseClosedOverAssertions
{
	public static FatWebResponseClosedOverAssertions<T> Should<T>(this Task<FatWebResponse<T>> task)
		where T : class
	{
		var result = task.Result;

		return new FatWebResponseClosedOverAssertions<T>(result);
	}

	public static FatWebResponseClosedOverAssertions<T> Should<T>(this FatWebResponse<T> response)
		where T : class
	{
		return new FatWebResponseClosedOverAssertions<T>(response);
	}
}

public class FatWebResponseClosedOverAssertions<T>(FatWebResponse<T> result)
	: ComparerBase<FatWebResponse<T>, FatWebResponseClosedOverAssertions<T>>(result)
	where T : class
{
	private ObjectComparer<FatWebResponse<T>> SubjectAsObject
	{
		get { return new ObjectComparer<FatWebResponse<T>>(Subject); }
	}

	public FatWebResponseClosedOverAssertions<T> Be(FatWebResponse<T> expectedResult)
	{
		SubjectAsObject.BeEquivalentTo(expectedResult);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> Be(T expectedValue)
	{
		SubjectAsObject.Not.BeNull();

		Subject.Data.Should().BeEquivalentTo(expectedValue);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> BeBadRequest()
	{
		return HaveStatusCode(HttpStatusCode.BadRequest);
	}

	public FatWebResponseClosedOverAssertions<T> BeConflict()
	{
		return HaveStatusCode(HttpStatusCode.Conflict);
	}

	public FatWebResponseClosedOverAssertions<T> BeEmptyListOf()
	{
		SubjectAsObject.Not.BeNull();

		var list = Subject.Data as List<T>;

		new ObjectComparer<List<T>>(list!).Not.BeNull();

		list.Should().BeEmpty();

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> BeEquivalentTo(FatWebResponse expectedResult)
	{
		new ObjectComparer<object>(Subject).BeEquivalentTo(expectedResult);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> BeEquivalentTo(T expectedValue)
	{
		Subject.Data.Should().BeEquivalentTo(expectedValue);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> BeNotAcceptable()
	{
		return HaveStatusCode(HttpStatusCode.NotAcceptable);
	}

	public FatWebResponseClosedOverAssertions<T> BeNotFound()
	{
		return HaveStatusCode(HttpStatusCode.NotFound);
	}

	public FatWebResponseClosedOverAssertions<T> BeOk()
	{
		return HaveOneOfStatusCode([HttpStatusCode.OK, HttpStatusCode.NoContent]);
	}

	public FatWebResponseClosedOverAssertions<T> BeSuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsSuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> BeUnsuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsUnsuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> For(Action<T> action)
	{
		SubjectAsObject.Not.BeNull();

		action(Subject.Data!);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> ForList(Action<List<T>> action)
	{
		SubjectAsObject.Not.BeNull();

		var list = Subject.Data as List<T>;

		action(list!);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> HaveContent(string content)
	{
		SubjectAsObject.Not.BeNull();

		Subject.Content.Should().Be(content);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> HaveContentEquivalentTo(T expectedContent)
	{
		SubjectAsObject.Not.BeNull("FatWebResponse should never be null");

		HaveStatusCode(
			HttpStatusCode.OK,
			$"you cannot test for content from an unsuccessful status code: {Subject.StatusCode}"
		);

		Subject.Data.Should().BeEquivalentTo(expectedContent);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> HaveContentTypeOf(string contentType)
	{
		SubjectAsObject.Not.BeNull();

		Subject.ContentType.Should().Be(contentType);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> HaveNoContent()
	{
		return HaveStatusCode(HttpStatusCode.NoContent);
	}

	public FatWebResponseClosedOverAssertions<T> HaveStatusCode(HttpStatusCode statusCode, string because = null)
	{
		return HaveOneOfStatusCode([statusCode], because);
	}

	public FatWebResponseClosedOverAssertions<T> WithMessage(string expectedMessage, string because = null)
	{
		Subject.Content.Should().Match(expectedMessage, Options.IgnoreCase, because);

		return this;
	}

	private FatWebResponseClosedOverAssertions<T> HaveOneOfStatusCode(
		HttpStatusCode[] acceptableStatusCodes,
		string because = null
	)
	{
		SubjectAsObject.Not.BeNull();

		Subject.StatusCode.Should().BeOneOf(acceptableStatusCodes, because);

		return this;
	}
}
