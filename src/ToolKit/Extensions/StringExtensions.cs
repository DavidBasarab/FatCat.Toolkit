#nullable enable
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FatCat.Toolkit.Extensions;

public static class StringExtensions
{
	public static bool Contains(this string source, string toCheck, StringComparison comp)
	{
		return source.IndexOf(toCheck, comp) >= 0;
	}

	public static string FirstLetterToUpper(this string input, char delimiter = ' ')
	{
		if (string.IsNullOrEmpty(input))
		{
			return string.Empty;
		}

		var words = input.Split(' ');

		var cleanWords = words.Select(word => word.CapitalizeFirstLetterOfWord()).ToList();

		return string.Join(delimiter.ToString(), cleanWords);
	}

	public static string FixedLength(this string text, int length)
	{
		if (text.Length > length)
		{
			text = text.Substring(0, length);
		}

		return text.PadRight(length);
	}

	public static string FormatWith(this string formatString, params object[] formatArgs)
	{
		return string.Format(formatString, formatArgs);
	}

	public static string FromBase64Encoded(this string base64EncodedData)
	{
		if (base64EncodedData.IsNullOrEmpty())
		{
			return string.Empty;
		}

		var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);

		return Encoding.UTF8.GetString(base64EncodedBytes);
	}

	public static string InsertSafeFileDate(this string stringWithFormat, DateTime dateTimeToInsert)
	{
		return stringWithFormat.FormatWith($"{dateTimeToInsert:yyyy-MM-ddTHH.mm.ss}");
	}

	public static string InsertSafeFileDate(this string stringWithFormat)
	{
		return stringWithFormat.InsertSafeFileDate(DateTime.Now);
	}

	public static bool IsNotNullOrEmpty(this string? value)
	{
		return !string.IsNullOrWhiteSpace(value);
	}

	public static bool IsNullOrEmpty(this string? value)
	{
		return string.IsNullOrWhiteSpace(value);
	}

	public static string MakeSafeFileName(this string fileName)
	{
		var sb = new StringBuilder();
		var invalidFileNameChars = Path.GetInvalidFileNameChars();

		foreach (var character in fileName)
		{
			if (invalidFileNameChars.Contains(character))
			{
				sb.Append("-");
			}
			else
			{
				sb.Append(character);
			}
		}

		return sb.ToString();
	}

	public static string RemoveAllWhitespace(this string value)
	{
		return Regex.Replace(value, @"\s+", "");
	}

	public static string RemoveSuffix(this string item, string? suffix)
	{
		if (suffix == null)
		{
			return item;
		}

		return !item.EndsWith(suffix) ? item : item.Remove(item.Length - suffix.Length, suffix.Length);
	}

	public static string ReplaceAllWhitespace(this string value, string replacement)
	{
		return Regex.Replace(value, @"\s+", replacement);
	}

	public static List<string> SplitByLine(this string? data)
	{
		return SplitByString(data, Environment.NewLine).ToList();
	}

	public static string[] SplitByString(this string? data, string separator)
	{
		if (data == null)
		{
			return new[] { string.Empty };
		}

		return data.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
	}

	public static byte[] ToAsciiByteArray(this string value)
	{
		return Encoding.ASCII.GetBytes(value);
	}

	public static string ToBase64Encoded(this string plainText)
	{
		if (plainText.IsNullOrEmpty())
		{
			return string.Empty;
		}

		var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

		return Convert.ToBase64String(plainTextBytes);
	}

	public static bool ToBool(this string? value)
	{
		if (value == null)
		{
			return false;
		}

		var lowerValue = value.ToLower();

		return lowerValue is "t" or "1" or "y" or "true" or "yes";
	}

	public static byte ToByte(this string value, byte? defaultValue = null)
	{
		if (!defaultValue.HasValue)
		{
			return byte.Parse(value);
		}

		byte parsedNumber;

		return byte.TryParse(value, out parsedNumber) ? parsedNumber : defaultValue.Value;
	}

	/// <param name="value">String to convert to bytes</param>
	/// <param name="encoding">
	///  Encoding type, UTF8, Unicode, etc. If unspecified, the default is UTF8.
	///  And really, that's what we should be using everywhere. Seriously.
	/// </param>
	public static byte[] ToByteArray(this string value, Encoding? encoding = null)
	{
		return encoding?.GetBytes(value) ?? Encoding.UTF8.GetBytes(value);
	}

	public static double ToDouble(this string? value, double? defaultValue = null)
	{
		if (!defaultValue.HasValue)
		{
			return value == null ? default : double.Parse(value);
		}

		return double.TryParse(value, out var parsedNumber) ? parsedNumber : defaultValue.Value;
	}

	public static Guid ToGuid(this string value)
	{
		return Guid.Parse(value);
	}

	public static int ToInt(this string? value, int? defaultValue = null)
	{
		if (!defaultValue.HasValue)
		{
			return value == null ? default : int.Parse(value);
		}

		return int.TryParse(value, out var parsedNumber) ? parsedNumber : defaultValue.Value;
	}

	public static long ToLong(this string? value, long? defaultValue = null)
	{
		if (!defaultValue.HasValue)
		{
			return value == null ? default : long.Parse(value);
		}

		return long.TryParse(value, out var parsedNumber) ? parsedNumber : defaultValue.Value;
	}

	public static string ToSlug(this string value)
	{
		return value.ToLowerInvariant().Replace(" ", "_");
	}

	public static Stream ToStream(this string? value)
	{
		return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
	}

	public static Uri ToUri(this string text)
	{
		return new Uri(text);
	}

	public static ushort ToUShort(this string? value, ushort? defaultValue = null)
	{
		if (!defaultValue.HasValue)
		{
			return value == null ? default : ushort.Parse(value);
		}

		return ushort.TryParse(value, out var parsedNumber) ? parsedNumber : defaultValue.Value;
	}

	public static string TruncateString(this string value, int maxLength)
	{
		return value.IsNullOrEmpty() ? string.Empty : value.Substring(0, Math.Min(value.Length, maxLength));
	}

	/// <summary>
	///  Allows a string such as "0x02AB÷0x04" to be transformed into a byte array. Wacky, no?
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static byte[] WithEmbeddedHexCodesToByteArray(this string? value)
	{
		if (value == null)
		{
			return [];
		}

		var returnValue = new List<byte>();

		for (var currentPosition = 0; currentPosition < value.Length; currentPosition++)
		{
			var possibleValueToAdd = Convert.ToByte(value[currentPosition]);
			var startingHereWithHexDoesNotGoPastEndOfString = currentPosition + 3 < value.Length;

			if (startingHereWithHexDoesNotGoPastEndOfString)
			{
				var nextTwoCharactersPossiblyIndicateHex =
					value[currentPosition] == '0' && value[currentPosition + 1] == 'x';

				if (nextTwoCharactersPossiblyIndicateHex)
				{
					var twoCharactersAfterIndicator = value.Substring(currentPosition + 2, 2);

					var twoCharactersAfterIndicatorAreHexValues = int.TryParse(
						twoCharactersAfterIndicator,
						NumberStyles.HexNumber,
						null,
						out var hexValue
					);

					if (twoCharactersAfterIndicatorAreHexValues)
					{
						possibleValueToAdd = Convert.ToByte(hexValue);
						currentPosition += 3;
					}
				}
			}

			returnValue.Add(possibleValueToAdd);
		}

		return returnValue.ToArray();
	}

	private static string CapitalizeFirstLetterOfWord(this string word)
	{
		return word.First().ToString().ToUpper() + string.Join("", word.ToLower().Skip(1));
	}
}
