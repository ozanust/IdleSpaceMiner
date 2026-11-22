using System.Collections.Generic;
using System.Text;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using UnityEngine;
using Zenject;

/// <summary>
/// Firebase Analytics. Firebase also measures sessions, engagement time and retention by itself;
/// the game's own events add what players do. Events logged before Firebase is ready are kept
/// and sent once it is. Without Firebase set up (no google-services.json / GoogleService-Info.plist)
/// events only go to the console in the editor and development builds.
/// </summary>
public class FirebaseAnalyticsService : IAnalyticsService, IInitializable
{
	private enum State { Starting, Ready, Unavailable }

	// A single instance for the whole app run, the game scene is reloaded when a galaxy is sold
	private static State state = State.Starting;
	private static bool started;
	private static readonly List<KeyValuePair<string, AnalyticsParams>> pendingEvents = new List<KeyValuePair<string, AnalyticsParams>>();
	private static readonly Dictionary<string, string> pendingProperties = new Dictionary<string, string>();

	private static bool LogToConsole => Application.isEditor || Debug.isDebugBuild;

	public void Initialize()
	{
		if (started)
		{
			return;
		}

		started = true;
		FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
		{
			if (task.Result != DependencyStatus.Available)
			{
				Debug.LogWarning($"[Analytics] Firebase is not available ({task.Result}), events are not sent");
				state = State.Unavailable;
				pendingEvents.Clear();
				pendingProperties.Clear();
				return;
			}

			try
			{
				FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
				state = State.Ready;
				foreach (KeyValuePair<string, string> property in pendingProperties)
				{
					FirebaseAnalytics.SetUserProperty(property.Key, property.Value);
				}

				foreach (KeyValuePair<string, AnalyticsParams> pending in pendingEvents)
				{
					Send(pending.Key, pending.Value);
				}
			}
			catch (System.Exception exception)
			{
				Debug.LogWarning($"[Analytics] Firebase failed to start: {exception.Message}");
				state = State.Unavailable;
			}

			pendingEvents.Clear();
			pendingProperties.Clear();
		});
	}

	public void LogEvent(string name, AnalyticsParams parameters = null)
	{
		if (LogToConsole)
		{
			Debug.Log($"[Analytics] {name} {Describe(parameters)}");
		}

		switch (state)
		{
			case State.Ready:
				Send(name, parameters);
				break;
			case State.Starting:
				pendingEvents.Add(new KeyValuePair<string, AnalyticsParams>(name, parameters));
				break;
		}
	}

	public void SetUserProperty(string name, string value)
	{
		switch (state)
		{
			case State.Ready:
				FirebaseAnalytics.SetUserProperty(name, value);
				break;
			case State.Starting:
				pendingProperties[name] = value;
				break;
		}
	}

	private static void Send(string name, AnalyticsParams parameters)
	{
		try
		{
			if (parameters == null || parameters.Values.Count == 0)
			{
				FirebaseAnalytics.LogEvent(name);
				return;
			}

			List<Parameter> list = new List<Parameter>();
			foreach (KeyValuePair<string, object> pair in parameters.Values)
			{
				switch (pair.Value)
				{
					case long number:
						list.Add(new Parameter(pair.Key, number));
						break;
					case double number:
						list.Add(new Parameter(pair.Key, number));
						break;
					default:
						list.Add(new Parameter(pair.Key, pair.Value.ToString()));
						break;
				}
			}

			FirebaseAnalytics.LogEvent(name, list.ToArray());
		}
		catch (System.Exception exception)
		{
			Debug.LogWarning($"[Analytics] Couldn't send {name}: {exception.Message}");
		}
	}

	private static string Describe(AnalyticsParams parameters)
	{
		if (parameters == null)
		{
			return "";
		}

		StringBuilder text = new StringBuilder();
		foreach (KeyValuePair<string, object> pair in parameters.Values)
		{
			text.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
		}

		return text.ToString();
	}
}
