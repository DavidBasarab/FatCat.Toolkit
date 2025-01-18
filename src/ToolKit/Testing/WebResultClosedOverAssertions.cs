using System.Net;
using FatCat.Toolkit.Web;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

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
	: ReferenceTypeAssertions<FatWebResponse<T>, FatWebResponseClosedOverAssertions<T>>(
		result,
		AssertionChain.GetOrCreate()
	)
	where T : class
{
	protected override string Identifier
	{
		get => "Web Results assertions";
	}

	public FatWebResponseClosedOverAssertions<T> Be(FatWebResponse<T> expectedResult)
	{
		new ObjectAssertions(Subject, CurrentAssertionChain).BeEquivalentTo(expectedResult);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> Be(T expectedValue)
	{
		Subject.Should().NotBeNull();

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
		Subject.Should().NotBeNull();

		var list = Subject.Data as List<T>;

		list.Should().NotBeNull();

		list.Should().BeEmpty();

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> BeEquivalentTo(FatWebResponse expectedResult)
	{
		new ObjectAssertions(Subject, CurrentAssertionChain).BeEquivalentTo(expectedResult);

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
		return HaveOneOfStatusCode(new[] { HttpStatusCode.OK, HttpStatusCode.NoContent });
	}

	public FatWebResponseClosedOverAssertions<T> BeSuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsSuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> BeUnsuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsUnsuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> For(Action<T> action)
	{
		Subject.Should().NotBeNull();

		action(Subject.Data!);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> ForList(Action<List<T>> action)
	{
		Subject.Should().NotBeNull();

		var list = Subject.Data as List<T>;

		action(list!);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> HaveContent(string content)
	{
		Subject.Should().NotBeNull();

		Subject.Content.Should().Be(content);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> HaveContentEquivalentTo(T expectedContent)
	{
		return HaveContentEquivalentTo(expectedContent, config => config);
	}

	public FatWebResponseClosedOverAssertions<T> HaveContentEquivalentTo(
		T expectedContent,
		Func<EquivalencyOptions<T>, EquivalencyOptions<T>> config
	)
	{
		Subject.Should().NotBeNull("FatWebResponse should never be null");

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

	public FatWebResponseClosedOverAssertions<T> HaveContentTypeOf(string contentType)
	{
		Subject.Should().NotBeNull();

		Subject.ContentType.Should().Be(contentType);

		return this;
	}

	public FatWebResponseClosedOverAssertions<T> HaveNoContent()
	{
		return HaveStatusCode(HttpStatusCode.NoContent);
	}

	public FatWebResponseClosedOverAssertions<T> HaveStatusCode(
		HttpStatusCode statusCode,
		string because = null,
		params object[] becauseArgs
	)
	{
		return HaveOneOfStatusCode(new[] { statusCode }, because, becauseArgs);
	}

	public FatWebResponseClosedOverAssertions<T> WithMessage(
		string expectedMessage,
		string because = null,
		params object[] becauseArgs
	)
	{
		Subject.Content.Should().MatchEquivalentOf(expectedMessage, because, becauseArgs);

		return this;
	}

	private FatWebResponseClosedOverAssertions<T> HaveOneOfStatusCode(
		HttpStatusCode[] acceptableStatusCodes,
		string because = null,
		params object[] becauseArgs
	)
	{
		Subject.Should().NotBeNull();

		Subject.StatusCode.Should().BeOneOf(acceptableStatusCodes, because, becauseArgs);

		return this;
	}
}
