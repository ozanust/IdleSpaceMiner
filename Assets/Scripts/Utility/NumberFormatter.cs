using System.Globalization;

/// <summary>Formats large numbers with short suffixes, e.g. 1.25K, 3.4M, 12B.</summary>
public static class NumberFormatter
{
	// q = quadrillion, Q = quintillion (same convention as Idle Planet Miner)
	private static readonly string[] suffixes = { "", "K", "M", "B", "T", "q", "Q" };

	public static string Format(double value)
	{
		if (value < 0)
		{
			return "-" + Format(-value);
		}

		if (value < 1000)
		{
			// Whole numbers stay as is, fractions (rates) keep up to 2 decimals
			return value.ToString(value % 1 == 0 ? "0" : "0.##", CultureInfo.InvariantCulture);
		}

		int suffixIndex = 0;
		while (value >= 1000 && suffixIndex < suffixes.Length - 1)
		{
			value /= 1000;
			suffixIndex++;
		}

		// 3 significant digits: 1.25K, 12.5K, 125K
		string format = value < 10 ? "0.##" : value < 100 ? "0.#" : "0";
		return value.ToString(format, CultureInfo.InvariantCulture) + suffixes[suffixIndex];
	}

	public static string FormatMoney(double value)
	{
		return "$" + Format(value);
	}

	/// <summary>Durations like 45s, 12.5s (with decimals), 2m 30s or 1h 5m.</summary>
	public static string FormatDuration(double seconds, bool decimals = false)
	{
		if (seconds < 0)
		{
			seconds = 0;
		}

		if (seconds < 60)
		{
			return seconds.ToString(decimals ? "0.0" : "0", CultureInfo.InvariantCulture) + "s";
		}

		int total = (int)System.Math.Round(seconds);
		if (total < 3600)
		{
			return $"{total / 60}m {total % 60}s";
		}

		return $"{total / 3600}h {total % 3600 / 60}m";
	}
}
