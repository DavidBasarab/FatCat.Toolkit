using System.Collections;
using System.Reflection;

namespace FatCat.Toolkit.Extensions;

public class ReflectionDeepCopy : IDeepCopy
{
	private static readonly MethodInfo memberwiseCloneMethod = typeof(object).GetMethod(
		"MemberwiseClone",
		BindingFlags.Instance | BindingFlags.NonPublic
	);

	public T Copy<T>(T objectToCopy)
		where T : class
	{
		if (objectToCopy == null)
		{
			return null;
		}

		var visited = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);

		return (T)CloneObject(objectToCopy, visited);
	}

	private object CloneObject(object source, Dictionary<object, object> visited)
	{
		if (source == null)
		{
			return null;
		}

		var sourceType = source.GetType();

		if (IsImmutableType(sourceType))
		{
			return source;
		}

		if (visited.TryGetValue(source, out var existing))
		{
			return existing;
		}

		if (sourceType.IsArray)
		{
			return CloneArray((Array)source, visited);
		}

		var clone = memberwiseCloneMethod.Invoke(source, null);

		visited[source] = clone;

		CloneProperties(source, clone, sourceType, visited);

		return clone;
	}

	private void CloneProperties(object source, object clone, Type sourceType, Dictionary<object, object> visited)
	{
		var properties = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

		foreach (var property in properties)
		{
			if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
			{
				continue;
			}

			var propertyType = property.PropertyType;

			if (propertyType.IsValueType || propertyType == typeof(string))
			{
				continue;
			}

			var value = property.GetValue(source);

			if (value == null)
			{
				continue;
			}

			var clonedValue = CloneObject(value, visited);

			property.SetValue(clone, clonedValue);
		}
	}

	private object CloneArray(Array sourceArray, Dictionary<object, object> visited)
	{
		var elementType = sourceArray.GetType().GetElementType();
		var length = sourceArray.Length;
		var clonedArray = Array.CreateInstance(elementType, length);

		visited[sourceArray] = clonedArray;

		if (elementType.IsValueType || elementType == typeof(string))
		{
			Array.Copy(sourceArray, clonedArray, length);
		}
		else
		{
			for (var i = 0; i < length; i++)
			{
				var element = sourceArray.GetValue(i);

				clonedArray.SetValue(CloneObject(element, visited), i);
			}
		}

		return clonedArray;
	}

	private bool IsImmutableType(Type type)
	{
		return type.IsValueType || type == typeof(string) || typeof(Delegate).IsAssignableFrom(type) || type == typeof(Type);
	}
}
