#nullable enable
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace FatCat.Toolkit.Extensions;

internal static class ObjectEquals
{
	private const string DateFormat = "MM/dd/yyyy HH:mm:ss";

	public static bool AreEqual(object? value1, object? value2)
	{
		if (IsOneNull(value1, value2))
		{
			return BothAreNull(value1, value2);
		}

		if (ReferenceEquals(value1, value2))
		{
			return true;
		}

		if (value1!.GetType() != value2!.GetType())
		{
			return false;
		}

		var propertyInfos = value1.GetType().GetProperties();

		var result = true;

		foreach (var propertyInfo in propertyInfos)
		{
			if (!result)
			{
				break;
			}

			if (ExcludeFromCompare(propertyInfo))
			{
				continue;
			}

			object? compare1 = null;
			object? compare2 = null;

			try
			{
				compare1 = propertyInfo.GetValue(value1, null);
				compare2 = propertyInfo.GetValue(value2, null);
			}
			catch (Exception)
			{
				// ignored
			}

			if (BothAreNull(compare1, compare2))
			{
				continue;
			}

			if (OneValueNull(compare2, compare1))
			{
				return false;
			}

			if (compare1 is string)
			{
				result = compare1.Equals(compare2);

				continue;
			}

			if (ImplementsIDictionary(compare1!.GetType()))
			{
				result = CompareDictionaries(propertyInfo, compare1, compare2);

				continue;
			}

			if (ImplementsIEnumerable(compare1.GetType()))
			{
				var list1 = compare1 as IEnumerable<object>;
				var list2 = compare2 as IEnumerable<object>;

				result = list1.ListsAreEqual(list2);

				continue;
			}

			if (compare1 is DateTime time)
			{
				// Do not want less than a second precision for comparisons
				var shortCompare1 = time.ToString(DateFormat);
				var shortCompare2 = ((DateTime)compare2!).ToString(DateFormat);

				result = shortCompare1.Equals(shortCompare2);

				continue;
			}

			if (compare1 is Color color1)
			{
				var color2 = (Color)compare2!;

				result = color1.Name.Equals(color2.Name);

				continue;
			}

			result = compare1.Equals(compare2);
		}

		return result;
	}

	public static bool EqualsOperator(object? rightHandSide, object? leftHandSide)
	{
		if (BothAreNull(rightHandSide, leftHandSide))
		{
			return true;
		}

		return !IsOneNull(rightHandSide, leftHandSide) && rightHandSide!.Equals(leftHandSide);
	}

	public static int GenerateHashCode(object? value)
	{
		if (value == null)
		{
			return 0;
		}

		var hash = new StringBuilder();

		var propertyInfos = value.GetType().GetProperties();

		foreach (var propertyInfo in propertyInfos)
		{
			var currentValue = propertyInfo.GetValue(value, null);

			if (currentValue == null)
			{
				continue;
			}

			if (ImplementsIEnumerable(currentValue.GetType()))
			{
				hash.Append(GenericHashBuilder((IEnumerable)currentValue));
			}
			else if (currentValue is DateTime dateValue)
			{
				hash.Append(dateValue.ToString(DateFormat));
			}
			else
			{
				hash.Append(currentValue);
			}
		}

		return hash.ToString().GetHashCode();
	}

	private static bool AreDictionariesTheSame(IDictionary dictionary1, IDictionary dictionary2)
	{
		if (dictionary1.Count != dictionary2.Count)
		{
			return false;
		}

		var result = true;

		foreach (var currentKey in dictionary1.Keys)
		{
			if (dictionary2.Contains(currentKey))
			{
				var currentValue1 = dictionary1[currentKey];
				var currentValue2 = dictionary2[currentKey];

				result = currentValue1!.Equals(currentValue2);

				if (!result)
				{
					break;
				}
			}
			else
			{
				result = false;

				break;
			}
		}

		return result;
	}

	private static bool BothAreNull(object? rhs, object? lhs)
	{
		return ReferenceEquals(rhs, null) && ReferenceEquals(lhs, null);
	}

	private static bool CompareDictionaries(PropertyInfo propertyInfo, object? compare1, object? compare2)
	{
		var dictionary1 = CreateDictionary(propertyInfo, compare1);
		var dictionary2 = CreateDictionary(propertyInfo, compare2);

		return AreDictionariesTheSame(dictionary1, dictionary2);
	}

	private static IDictionary CreateDictionary(PropertyInfo propertyInfo, object? compare1)
	{
		var dictionary = typeof(Dictionary<,>);
		var typeArgs = propertyInfo.PropertyType.GetGenericArguments();

		var constructed = dictionary.MakeGenericType(typeArgs);

		var dictionary1 = (IDictionary)Activator.CreateInstance(constructed)!;

		var compare1Dict = compare1 as IDictionary;

		foreach (var currentKey in compare1Dict!.Keys)
		{
			dictionary1.Add(currentKey, compare1Dict[currentKey]);
		}

		return dictionary1;
	}

	private static bool ExcludeFromCompare(PropertyInfo propertyInfo)
	{
		return propertyInfo.GetCustomAttributes(typeof(CompareExclude), false).Any();
	}

	private static string GenericHashBuilder(IEnumerable list)
	{
		var hashBuilder = new StringBuilder();

		foreach (var item in list)
		{
			if (item != null)
			{
				hashBuilder.Append(item.GetHashCode());
			}
		}

		return hashBuilder.ToString();
	}

	private static bool ImplementsIDictionary(Type type)
	{
		return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));
	}

	private static bool ImplementsIEnumerable(Type type)
	{
		return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
	}

	private static bool IsOneNull(object? rhs, object? lhs)
	{
		return ReferenceEquals(rhs, null) || ReferenceEquals(lhs, null);
	}

	private static bool OneValueNull(object? compare2, object? compare1)
	{
		return !BothAreNull(compare1, compare2) && IsOneNull(compare1, compare2);
	}
}
