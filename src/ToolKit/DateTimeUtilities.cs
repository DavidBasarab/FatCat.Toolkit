using System.Diagnostics.CodeAnalysis;

namespace FatCat.Toolkit;

public interface IDateTimeUtilities
{
	DateTime ConvertToUtc(DateTime localDateTime);

	DateTime LocalNow();

	DateTime UtcNow();
}

[ExcludeFromCodeCoverage(Justification = "A time wrapper")]
public class DateTimeUtilities : IDateTimeUtilities
{
	public DateTime ConvertToUtc(DateTime localDateTime)
	{
		return TimeZoneInfo.ConvertTimeToUtc(localDateTime);
	}

	public DateTime LocalNow()
	{
		return DateTime.Now;
	}

	public DateTime UtcNow()
	{
		return DateTime.UtcNow;
	}
}
