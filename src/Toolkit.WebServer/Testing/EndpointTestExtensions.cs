using FatCat.Testing;
using FatCat.Testing.Comparers;
using FatCat.Testing.Exceptions;
using FatCat.Toolkit.Extensions;
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

public class EndpointAssertions(Endpoint endpoint) : ComparerBase<Endpoint, EndpointAssertions>(endpoint)
{
	private readonly Endpoint endpoint = endpoint;

	public EndpointAssertions BeDelete(string methodName, string template = null!)
	{
		return HaveHttpAttribute<HttpDeleteAttribute>(methodName, template);
	}

	public EndpointAssertions BeGet(string methodName, string template = null!)
	{
		return HaveHttpAttribute<HttpGetAttribute>(methodName, template);
	}

	public EndpointAssertions BePost(string methodName, string template = null!)
	{
		return HaveHttpAttribute<HttpPostAttribute>(methodName, template);
	}

	public EndpointAssertions HaveHttpAttribute<THttpAttribute>(string methodName, string expectedTemplate = null!)
		where THttpAttribute : HttpMethodAttribute, IActionHttpMethodProvider
	{
		var attributes = GetAttributes<THttpAttribute>(methodName, endpoint.GetType());

		if (attributes == null)
		{
			CompareException.New($"did not find Http attribute {typeof(THttpAttribute).Name}");
		}

		attributes!.Count.Should().BeGreaterThan(0, $"did not find Http attribute {typeof(THttpAttribute).Name}");

		if (!expectedTemplate.IsNotNullOrEmpty())
		{
			return this;
		}

		var templates = attributes.Select(i => i.Template).ToList();

		if (templates.Count == 1)
		{
			var actualTemplate = templates.Single();

			actualTemplate.Should().Be(expectedTemplate, because: "route on template should match");

			return this;
		}

		if (!templates.Contains(expectedTemplate))
		{
			CompareException.New(
				$@"
Expected to find: {expectedTemplate} in:
{templates.ToDelimited(Environment.NewLine)}"
			);
		}

		return this;
	}

	private static List<TAttribute> GetAttributes<TAttribute>(string methodName, Type controllerType)
		where TAttribute : Attribute
	{
		var method = controllerType.GetMethod(methodName);

		return method?.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>().ToList();
	}
}
