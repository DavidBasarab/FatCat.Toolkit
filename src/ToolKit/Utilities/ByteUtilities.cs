using FatCat.Toolkit.Extensions;

namespace FatCat.Toolkit.Utilities;

public interface IByteUtilities
{
	string ToReadableString(byte[] bytes);
}

public class ByteUtilities : IByteUtilities
{
	public const int BytesInMegaBytes = 1048576;

	public string ToReadableString(byte[] bytes) => bytes.ToReadableString();
}
