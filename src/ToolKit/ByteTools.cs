using System.Text;
using FatCat.Toolkit.Extensions;

namespace FatCat.Toolkit;

public interface IByteTools
{
	public byte[] FromBase64Encoded(string text);

	public byte[] FromBase64String(string text);

	public string ToBase64Encoded(byte[] bytes);

	public string ToBase64String(byte[] bytes);
}

public class ByteTools : IByteTools
{
	public byte[] FromBase64Encoded(string text)
	{
		if (text.IsNullOrEmpty())
		{
			return Array.Empty<byte>();
		}

		var plainTextBytes = Encoding.UTF8.GetBytes(text);

		var base64Text = Convert.ToBase64String(plainTextBytes);

		return Convert.FromBase64String(base64Text);
	}

	public byte[] FromBase64String(string text)
	{
		return Convert.FromBase64String(text);
	}

	public string ToBase64Encoded(byte[] bytes)
	{
		if (bytes.Length == 0)
		{
			return string.Empty;
		}

		var base64Text = Convert.ToBase64String(bytes);

		var plainTextBytes = Convert.FromBase64String(base64Text);

		return Encoding.UTF8.GetString(plainTextBytes);
	}

	public string ToBase64String(byte[] bytes)
	{
		return Convert.ToBase64String(bytes);
	}
}
