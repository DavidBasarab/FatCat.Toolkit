#nullable enable
using System.Text;
using FatCat.Toolkit.Extensions;

namespace FatCat.Toolkit.Utilities;

public interface IStringUtilities
{
	public bool Contains(string text, string toCheck, StringComparison comp);

	public string FirstLetterToUpper(string text, char delimiter = ' ');

	public string FixedLength(string text, int length);

	public string FormatWith(string text, params object[] formatArgs);

	public string FromBase64Encoded(string text);

	public string InsertSafeFileDate(string text, DateTime dateTime);

	public string InsertSafeFileDate(string text);

	public bool IsNotNullOrEmpty(string? text);

	public bool IsNullOrEmpty(string? text);

	public string MakeSafeFileName(string fileName);

	public string RemoveAllWhitespace(string text);

	public string RemoveSuffix(string text, string? suffix);

	public string ReplaceAllWhitespace(string text, string replacement);

	public List<string> SplitByLine(string? text);

	public string[] SplitByString(string? text, string separator);

	public byte[] ToAsciiByteArray(string text);

	public string ToBase64Encoded(string text);

	public bool ToBool(string? text);

	public byte ToByte(string text, byte? defaultValue = null);

	public byte[] ToByteArray(string text, Encoding? encoding = null);

	public double ToDouble(string? text, double? defaultValue = null);

	public Guid ToGuid(string text);

	public int ToInt(string? text, int? defaultValue = null);

	public long ToLong(string? text, long? defaultValue = null);

	public string ToSlug(string text);

	public Stream ToStream(string? text);

	public ushort ToUShort(string? text, ushort? defaultValue = null);

	public string TruncateString(string text, int maxLength);

	public byte[] WithEmbeddedHexCodesToByteArray(string? text);
}

public class StringUtilities : IStringUtilities
{
	public bool Contains(string text, string toCheck, StringComparison comp)
	{
		return text.Contains(toCheck, comp);
	}

	public string FirstLetterToUpper(string text, char delimiter = ' ')
	{
		return text.FirstLetterToUpper(delimiter);
	}

	public string FixedLength(string text, int length)
	{
		return text.FixedLength(length);
	}

	public string FormatWith(string text, params object[] formatArgs)
	{
		return text.FormatWith(text, formatArgs);
	}

	public string FromBase64Encoded(string text)
	{
		return text.FromBase64Encoded();
	}

	public string InsertSafeFileDate(string text, DateTime dateTime)
	{
		return text.InsertSafeFileDate(dateTime);
	}

	public string InsertSafeFileDate(string text)
	{
		return text.InsertSafeFileDate();
	}

	public bool IsNotNullOrEmpty(string? text)
	{
		return text.IsNotNullOrEmpty();
	}

	public bool IsNullOrEmpty(string? text)
	{
		return text.IsNullOrEmpty();
	}

	public string MakeSafeFileName(string fileName)
	{
		return fileName.MakeSafeFileName();
	}

	public string RemoveAllWhitespace(string text)
	{
		return text.RemoveAllWhitespace();
	}

	public string RemoveSuffix(string text, string? suffix)
	{
		return text.RemoveSuffix(suffix);
	}

	public string ReplaceAllWhitespace(string text, string replacement)
	{
		return text.ReplaceAllWhitespace(replacement);
	}

	public List<string> SplitByLine(string? text)
	{
		return text.SplitByLine();
	}

	public string[] SplitByString(string? text, string separator)
	{
		return text.SplitByString(separator);
	}

	public byte[] ToAsciiByteArray(string text)
	{
		return text.ToAsciiByteArray();
	}

	public string ToBase64Encoded(string text)
	{
		return text.ToBase64Encoded();
	}

	public bool ToBool(string? text)
	{
		return text.ToBool();
	}

	public byte ToByte(string text, byte? defaultValue = null)
	{
		return text.ToByte(defaultValue);
	}

	public byte[] ToByteArray(string text, Encoding? encoding = null)
	{
		return text.ToByteArray(encoding);
	}

	public double ToDouble(string? text, double? defaultValue = null)
	{
		return text.ToDouble(defaultValue);
	}

	public Guid ToGuid(string text)
	{
		return text.ToGuid();
	}

	public int ToInt(string? text, int? defaultValue = null)
	{
		return text.ToInt(defaultValue);
	}

	public long ToLong(string? text, long? defaultValue = null)
	{
		return text.ToLong();
	}

	public string ToSlug(string text)
	{
		return text.ToSlug();
	}

	public Stream ToStream(string? text)
	{
		return text.ToStream();
	}

	public ushort ToUShort(string? text, ushort? defaultValue = null)
	{
		return text.ToUShort(defaultValue);
	}

	public string TruncateString(string text, int maxLength)
	{
		return text.TruncateString(maxLength);
	}

	public byte[] WithEmbeddedHexCodesToByteArray(string? text)
	{
		return text.WithEmbeddedHexCodesToByteArray();
	}
}
