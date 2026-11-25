using System.Collections.Generic;

/// <summary>
/// Sends analytics events. Event and parameter names follow Firebase rules: up to 40 characters,
/// letters, digits and underscores, starting with a letter. Values are strings, whole numbers or decimals.
/// </summary>
public interface IAnalyticsService
{
	void LogEvent(string name, AnalyticsParams parameters = null);

	void SetUserProperty(string name, string value);
}

/// <summary>Event parameters, built with chained Add calls.</summary>
public class AnalyticsParams
{
	public readonly Dictionary<string, object> Values = new Dictionary<string, object>();

	public AnalyticsParams Add(string name, string value)
	{
		// Firebase cuts string values at 100 characters
		Values[name] = value != null && value.Length > 100 ? value.Substring(0, 100) : value ?? "";
		return this;
	}

	public AnalyticsParams Add(string name, long value)
	{
		Values[name] = value;
		return this;
	}

	public AnalyticsParams Add(string name, double value)
	{
		Values[name] = value;
		return this;
	}

	public AnalyticsParams Add(string name, bool value)
	{
		Values[name] = value ? 1L : 0L;
		return this;
	}

	public static AnalyticsParams With(string name, string value) => new AnalyticsParams().Add(name, value);

	public static AnalyticsParams With(string name, long value) => new AnalyticsParams().Add(name, value);
}

/// <summary>Events logged from several places, kept in one shape.</summary>
public static class AnalyticsEvents
{
	/// <summary>Dark Matter coming in, for Firebase's earn_virtual_currency report.</summary>
	public static void EarnDarkMatter(this IAnalyticsService analytics, long amount, string source)
	{
		analytics.LogEvent("earn_virtual_currency", new AnalyticsParams()
			.Add("virtual_currency_name", "dark_matter")
			.Add("value", amount)
			.Add("source", source));
	}

	/// <summary>Dark Matter spent, for Firebase's spend_virtual_currency report.</summary>
	public static void SpendDarkMatter(this IAnalyticsService analytics, long amount, string item)
	{
		analytics.LogEvent("spend_virtual_currency", new AnalyticsParams()
			.Add("virtual_currency_name", "dark_matter")
			.Add("value", amount)
			.Add("item_name", item));
	}
}
