using System.Collections;

namespace FatCat.Toolkit.Enumerations;

public static class EnumerationExtensions
{
	public static bool Empty<T>(this IEnumerable<T> source) => !source.Any();

	public static bool Empty<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) => !source.Any(predicate);

	public static bool IsFlagNotSet<T>(this T value, T flag) where T : struct
	{
		var testValueNumber = Convert.ToInt64(value);
		var flagNumberValue = Convert.ToInt64(flag);

		return (testValueNumber & flagNumberValue) == 0;
	}

	public static bool IsFlagSet<T>(this T value, T flag) where T : struct
	{
		var testValueNumber = Convert.ToInt64(value);
		var flagNumberValue = Convert.ToInt64(flag);

		return (testValueNumber & flagNumberValue) != 0;
	}

	public static string ToDelimited(this IEnumerable list) => ToDelimited(list, ",");

	public static string ToDelimited(this IEnumerable list, string delimiter)
	{
		var returnValue = list.Cast<object>().Aggregate<object, string>(null!, (current, unknown) => current + (unknown + delimiter));

		if (!string.IsNullOrEmpty(delimiter)) returnValue = returnValue.Remove(returnValue.Length - delimiter.Length);

		return returnValue;
	}

	public static T ToEnum<T>(this string value) => (T)Enum.Parse(typeof(T), value, true);

	public static T ToEnum<T>(this string value, T errorValue)
	{
		try { return (T)Enum.Parse(typeof(T), value, true); }
		catch { return errorValue; }
	}

	public static IList<T> ToList<T>() => (from object value in Enum.GetValues(typeof(T))
											select (T)value).ToList();
}