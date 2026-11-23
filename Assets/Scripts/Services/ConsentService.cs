using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
#endif

/// <summary>What the player agreed to, read from the consent form's answers.</summary>
public struct ConsentChoices
{
	public bool Analytics;
	public bool AdStorage;
	public bool AdUserData;
	public bool AdPersonalization;

	public static ConsentChoices All => new ConsentChoices { Analytics = true, AdStorage = true, AdUserData = true, AdPersonalization = true };
}

public interface IConsentService
{
	bool IsResolved { get; }

	/// <summary>Ads may be requested: consent given, or not needed where the player lives.</summary>
	bool CanRequestAds { get; }

	/// <summary>The player must be able to reopen the consent choices (EEA, UK and similar).</summary>
	bool PrivacyOptionsRequired { get; }

	ConsentChoices Choices { get; }

	/// <summary>Runs the action once consent is known, right away if it already is, and again after the choices change.</summary>
	void OnConsent(Action action);

	void RemoveOnConsent(Action action);

	void ShowPrivacyOptions();
}

/// <summary>
/// Google's User Messaging Platform: asks for consent where the law requires it (EEA, UK, Switzerland)
/// before ads and analytics start, and lets the player change it later from Settings.
/// Runs once per app launch; the game scene reloads after a galaxy sale and keeps the result.
/// </summary>
public class ConsentService : IConsentService, IInitializable
{
	private static bool started;
	private static bool resolved;
	private static bool canRequestAds;
	private static bool privacyOptionsRequired;
	private static ConsentChoices choices;
	private static readonly List<Action> listeners = new List<Action>();

	public bool IsResolved => resolved;
	public bool CanRequestAds => canRequestAds;
	public bool PrivacyOptionsRequired => privacyOptionsRequired;
	public ConsentChoices Choices => choices;

	public void Initialize()
	{
		if (started)
		{
			return;
		}

		started = true;
#if GOOGLE_MOBILE_ADS
		ConsentInformation.Update(new ConsentRequestParameters(), updateError => OnMainThread(() =>
		{
			if (updateError != null)
			{
				// Offline or misconfigured: a previous answer still counts, otherwise ads wait for the next launch
				Debug.LogWarning($"[Consent] Couldn't check consent: {updateError.Message}");
				Finish();
				return;
			}

			ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMainThread(() =>
			{
				if (formError != null)
				{
					Debug.LogWarning($"[Consent] Consent form failed: {formError.Message}");
				}

				Finish();
			}));
		}));
#else
		Finish();
#endif
	}

	public void OnConsent(Action action)
	{
		if (!listeners.Contains(action))
		{
			listeners.Add(action);
		}

		if (resolved)
		{
			action();
		}
	}

	public void RemoveOnConsent(Action action)
	{
		listeners.Remove(action);
	}

	public void ShowPrivacyOptions()
	{
#if GOOGLE_MOBILE_ADS
		ConsentForm.ShowPrivacyOptionsForm(error => OnMainThread(() =>
		{
			if (error != null)
			{
				Debug.LogWarning($"[Consent] Privacy options failed: {error.Message}");
			}

			Finish();
		}));
#endif
	}

	private static void Finish()
	{
#if GOOGLE_MOBILE_ADS
		canRequestAds = ConsentInformation.CanRequestAds();
		privacyOptionsRequired = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
		choices = ReadChoices(ConsentInformation.ConsentStatus == ConsentStatus.NotRequired);
#else
		canRequestAds = true;
		privacyOptionsRequired = false;
		choices = ConsentChoices.All;
#endif
		resolved = true;
		Debug.Log($"[Consent] ads={canRequestAds} analytics={choices.Analytics} personalized={choices.AdPersonalization} options={privacyOptionsRequired}");
		foreach (Action listener in listeners.ToArray())
		{
			listener();
		}
	}

#if GOOGLE_MOBILE_ADS
	private static void OnMainThread(Action action)
	{
		MobileAdsEventExecutor.ExecuteInUpdate(action);
	}

	/// <summary>
	/// The consent form stores the answers as IAB TCF strings; purpose 1 is storing data on the device,
	/// 3 and 4 personalised ads, 7 measuring ads. Where GDPR doesn't apply everything is allowed.
	/// </summary>
	private static ConsentChoices ReadChoices(bool notRequired)
	{
		if (notRequired)
		{
			return ConsentChoices.All;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		try
		{
			using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
			using (AndroidJavaClass manager = new AndroidJavaClass("android.preference.PreferenceManager"))
			using (AndroidJavaObject prefs = manager.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity))
			{
				if (prefs.Call<int>("getInt", "IABTCF_gdprApplies", 0) != 1)
				{
					return ConsentChoices.All;
				}

				string purposes = prefs.Call<string>("getString", "IABTCF_PurposeConsents", "");
				bool Purpose(int number) => purposes.Length >= number && purposes[number - 1] == '1';
				return new ConsentChoices
				{
					Analytics = Purpose(1),
					AdStorage = Purpose(1),
					AdUserData = Purpose(1) && Purpose(7),
					AdPersonalization = Purpose(3) && Purpose(4)
				};
			}
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[Consent] Couldn't read consent choices: {exception.Message}");
		}
#endif
		// Choices unknown (iOS, editor): only what was clearly agreed to, nothing personalised
		bool obtained = ConsentInformation.ConsentStatus == ConsentStatus.Obtained;
		return new ConsentChoices { Analytics = obtained, AdStorage = obtained, AdUserData = false, AdPersonalization = false };
	}
#endif
}
