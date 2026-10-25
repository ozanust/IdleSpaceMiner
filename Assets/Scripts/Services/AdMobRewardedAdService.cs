#if GOOGLE_MOBILE_ADS
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;
using Zenject;

/// <summary>
/// Rewarded ads through Google AdMob. Keeps one ad loaded, retries after a failed load and
/// loads the next ad as soon as one is shown. Compiled only with the GOOGLE_MOBILE_ADS define.
/// </summary>
public class AdMobRewardedAdService : IRewardedAdService, IInitializable, ITickable, IDisposable
{
	private const float RetryDelay = 30f;

	private readonly AdRewardSettings settings;

	private RewardedAd ad;
	private bool initialized;
	private bool loading;
	private float retryAt = -1f;
	private Action<bool> pendingCallback;
	private bool rewardEarned;

	public bool IsReady => ad != null && ad.CanShowAd();

	private string AdUnitId => Application.platform == RuntimePlatform.IPhonePlayer
		? settings.IosRewardedAdUnitId
		: settings.AndroidRewardedAdUnitId;

	public AdMobRewardedAdService(GameSettings gameSettings)
	{
		settings = gameSettings.AdRewardSettings;
	}

	public void Initialize()
	{
		// AdMob calls back on its own threads, so every callback hops to Unity's main thread first
		MobileAds.Initialize(status => MobileAdsEventExecutor.ExecuteInUpdate(() =>
		{
			initialized = true;
			Load();
		}));
	}

	public void Tick()
	{
		if (retryAt >= 0f && Time.unscaledTime >= retryAt)
		{
			retryAt = -1f;
			Load();
		}
	}

	public void Dispose()
	{
		ad?.Destroy();
		ad = null;
	}

	public void Show(Action<bool> onFinished)
	{
		if (!IsReady)
		{
			Load();
			onFinished?.Invoke(false);
			return;
		}

		pendingCallback = onFinished;
		rewardEarned = false;
		ad.Show(reward => MobileAdsEventExecutor.ExecuteInUpdate(() => rewardEarned = true));
	}

	private void Load()
	{
		if (!initialized || loading || IsReady)
		{
			return;
		}

		loading = true;
		ad?.Destroy();
		ad = null;

		RewardedAd.Load(AdUnitId, new AdRequest(), (RewardedAd loaded, LoadAdError error) => MobileAdsEventExecutor.ExecuteInUpdate(() =>
		{
			loading = false;
			if (error != null || loaded == null)
			{
				Debug.LogWarning("Rewarded ad failed to load: " + error);
				retryAt = Time.unscaledTime + RetryDelay;
				return;
			}

			ad = loaded;
			ad.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(() => Finish(rewardEarned));
			ad.OnAdFullScreenContentFailed += failure => MobileAdsEventExecutor.ExecuteInUpdate(() =>
			{
				Debug.LogWarning("Rewarded ad failed to show: " + failure);
				Finish(false);
			});
		}));
	}

	private void Finish(bool earned)
	{
		Action<bool> callback = pendingCallback;
		pendingCallback = null;
		ad?.Destroy();
		ad = null;
		Load();
		callback?.Invoke(earned);
	}
}
#endif
