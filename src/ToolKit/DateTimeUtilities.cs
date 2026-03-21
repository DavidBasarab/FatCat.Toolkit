using System.Diagnostics.CodeAnalysis;

namespace FatCat.Toolkit;

public interface IDateTimeUtilities
{
	public DateTime ConvertToUtc(DateTime localDateTime);

	public DateTime LocalNow();

	public DateTime UtcNow();
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
