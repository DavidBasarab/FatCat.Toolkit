using FatCat.Toolkit.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FatCat.Toolkit.WebServer.Testing;

public static class EndpointTestExtensions
{
	public static EndpointAssertions Should(this Endpoint controller)
	{
		return new EndpointAssertions(controller);
	}
}

public class EndpointAssertions(Endpoint endpoint)
	: ReferenceTypeAssertions<Endpoint, EndpointAssertions>(endpoint)
{
	private readonly Endpoint endpoint = endpoint;

	protected override string Identifier
	{
		get => "Fog Endpoint";
	}

	public AndConstraint<EndpointAssertions> BeDelete(string methodName, string template = null!)
	{
		return HaveHttpAttribute<HttpDeleteAttribute>(methodName, template);
	}

	public AndConstraint<EndpointAssertions> BeGet(string methodName, string template = null!)
	{
		return HaveHttpAttribute<HttpGetAttribute>(methodName, template);
	}

	public AndConstraint<EndpointAssertions> BePost(string methodName, string template = null!)
	{
		return HaveHttpAttribute<HttpPostAttribute>(methodName, template);
	}

	public AndConstraint<EndpointAssertions> HaveHttpAttribute<THttpAttribute>(
		string methodName,
		string expectedTemplate = null!
	)
		where THttpAttribute : HttpMethodAttribute, IActionHttpMethodProvider
	{
		var attributes = GetAttributes<THttpAttribute>(methodName, endpoint.GetType());

		attributes.Should().NotBeNull($"did not find Http attribute {typeof(THttpAttribute).Name}");

		if (attributes == null)
		{
			return new AndConstraint<EndpointAssertions>(this);
		}

		attributes.Count.Should().BeGreaterThan(0, $"did not find Http attribute {typeof(THttpAttribute).Name}");

		if (!expectedTemplate.IsNotNullOrEmpty())
		{
			return new AndConstraint<EndpointAssertions>(this);
		}

		var templates = attributes.Select(i => i.Template).ToList();
		var hasExpectedTemplate = templates.Contains(expectedTemplate);

		if (templates.Count == 1)
		{
			var actualTemplate = templates.Single();

			actualTemplate.Should().Be(expectedTemplate, "route on template should match");
		}
		else
		{
			Execute.Assertion
				.ForCondition(hasExpectedTemplate)
				.FailWith(
					$@"
Expected to find: {expectedTemplate} in:
{templates.ToDelimited(Environment.NewLine)}"
				);
		}

		return new AndConstraint<EndpointAssertions>(this);
	}

	private static List<TAttribute> GetAttributes<TAttribute>(string methodName, Type controllerType)
		where TAttribute : Attribute
	{
		var method = controllerType.GetMethod(methodName);

		return method?.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>().ToList();
	}
}
