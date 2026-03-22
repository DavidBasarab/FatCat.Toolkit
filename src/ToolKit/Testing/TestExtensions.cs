using FakeItEasy;
using FakeItEasy.Configuration;
using FatCat.Toolkit.Threading;
using FluentAssertions;

namespace FatCat.Toolkit.Testing;

public static class TestExtensions
{
	public static T Is<T>(this IArgumentConstraintManager<T> manager, T expectedItem)
	{
		bool Matcher(T matchItem)
		{
			matchItem.Should().Be(expectedItem);

			return true;
		}

		return manager.Matches(Matcher);
	}

	public static T Matches<T>(this IArgumentConstraintManager<T> manager, Action<T> predicate)
	{
		bool Matcher(T item)
		{
			predicate(item);

			return true;
		}

		return manager.Matches(Matcher);
	}

	/// <summary>
	///  Configures the call to return the next value from the specified sequence each time it's called.
	///  After the sequence has been exhausted, the call will revert to the previously configured behavior.
	/// </summary>
	/// <typeparam name="T">The type of return value.</typeparam>
	/// <param name="configuration">The call configuration to extend.</param>
	/// <param name="values">The values to return in sequence.</param>
	public static void ReturnsNextFromSequence<T>(this IReturnValueConfiguration<T> configuration, List<T> values)
	{
		configuration.ReturnsNextFromSequence(values.ToArray());
	}

	public static void ReturnsNextFromSequence<T>(this IReturnValueConfiguration<Task<T>> configuration, List<T> values)
	{
		configuration.ReturnsNextFromSequence(values.ToArray());
	}

	public static void RunThreadAction(this IThread thread)
	{
		A.CallTo(() => thread.Run(A<Action>._))
			.Invokes(call =>
			{
				var action = call.Arguments[0] as Action;

				action!();
			});

		A.CallTo(() => thread.Run(A<Func<Task>>._))
			.Invokes(call =>
			{
				var func = call.Arguments[0] as Func<Task>;

				func!().Wait();
			});
	}

	public static UnorderedCallAssertion ShouldMatch<T>(this IAssertConfiguration configuration, Action<T> matcher)
	{
		return configuration.MustHaveHappened(1, Times.OrMore);
	}

	private static T Matches<T>(this IArgumentConstraintManager<T> manager, Func<T, bool> predicate)
	{
		return manager.Matches(predicate, x => x.Write("Test Matches fails"));
	}
}
