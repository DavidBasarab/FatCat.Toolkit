#nullable enable
using System.Net;
using FatCat.Toolkit.Web;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Primitives;

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

public class FatWebResponseAssertions(FatWebResponse result)
	: ReferenceTypeAssertions<FatWebResponse, FatWebResponseAssertions>(result)
{
	protected override string Identifier
	{
		get => "Web Results assertions";
	}

	public FatWebResponseAssertions Be(FatWebResponse expectedResult)
	{
		new ObjectAssertions(Subject).BeEquivalentTo(expectedResult);

		return this;
	}

	public FatWebResponseAssertions Be<T>(T expectedValue)
	{
		Subject.Should().NotBeNull();

		Subject.To<T>().Should().BeEquivalentTo(expectedValue);

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
		Subject.Should().NotBeNull();

		var list = Subject.To<List<T>>();

		list.Should().BeEmpty();

		return this;
	}

	public FatWebResponseAssertions BeEquivalentTo(FatWebResponse expectedResult)
	{
		new ObjectAssertions(Subject).BeEquivalentTo(expectedResult);

		return this;
	}

	public FatWebResponseAssertions BeEquivalentTo<T>(T expectedValue)
	{
		Subject.To<T>().Should().BeEquivalentTo(expectedValue);

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
		return HaveOneOfStatusCode(new[] { HttpStatusCode.OK, HttpStatusCode.NoContent });
	}

	public FatWebResponseAssertions BeSuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsSuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseAssertions BeUnauthorized()
	{
		return HaveStatusCode(HttpStatusCode.Unauthorized);
	}

	public FatWebResponseAssertions BeUnsuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsUnsuccessful.Should().BeTrue(Subject.Content);

		return this;
	}

	public FatWebResponseAssertions For<T>(Action<T> action)
	{
		Subject.Should().NotBeNull();

		action(Subject.To<T>()!);

		return this;
	}

	public FatWebResponseAssertions ForList<T>(Action<List<T>> action)
	{
		Subject.Should().NotBeNull();

		action(Subject.To<List<T>>()!);

		return this;
	}

	public FatWebResponseAssertions HaveContent(string content)
	{
		Subject.Should().NotBeNull();

		Subject.Content.Should().Be(content);

		return this;
	}

	public FatWebResponseAssertions HaveContentEquivalentTo<TContentType>(TContentType expectedContent)
	{
		return HaveContentEquivalentTo(expectedContent, config => config);
	}

	public FatWebResponseAssertions HaveContentEquivalentTo<TContentType>(
		TContentType expectedContent,
		Func<EquivalencyAssertionOptions<TContentType>, EquivalencyAssertionOptions<TContentType>> config
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

		var actualContent = Subject.To<TContentType>();

		actualContent.Should().BeEquivalentTo(expectedContent);

		return this;
	}

	public FatWebResponseAssertions HaveContentTypeOf(string contentType)
	{
		Subject.Should().NotBeNull();

		Subject.ContentType.Should().Be(contentType);

		return this;
	}

	public FatWebResponseAssertions HaveNoContent()
	{
		return HaveStatusCode(HttpStatusCode.NoContent);
	}

	public FatWebResponseAssertions HaveStatusCode(
		HttpStatusCode statusCode,
		string? because = null,
		params object[] becauseArgs
	)
	{
		return HaveOneOfStatusCode(new[] { statusCode }, because, becauseArgs);
	}

	public FatWebResponseAssertions WithMessage(
		string expectedMessage,
		string? because = null,
		params object[] becauseArgs
	)
	{
		Subject.Content.Should().MatchEquivalentOf(expectedMessage, because, becauseArgs);

		return this;
	}

	private FatWebResponseAssertions HaveOneOfStatusCode(
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
