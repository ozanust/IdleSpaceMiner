using System;
using UnityEngine;
using Zenject;

/// <summary>
/// Rewarded ad boosts, as in Idle Planet Miner. The Mine Boost speeds up mining on all planets for
/// an hour after an ad. The Ark shows up every few minutes and an ad claims it for Dark Matter or
/// cash. The boost runs on real time, so it also covers time away and carries into a new galaxy.
/// </summary>
public class BoostController : IBoostController, IInitializable, ITickable
{
	private readonly SignalBus signalBus;
	private readonly IPlayerModel playerModel;
	private readonly IRewardedAdService adService;
	private readonly IMothershipController mothershipController;
	// Mining asks this controller for the boost, so it is resolved lazily the other way round
	private readonly LazyInject<IMiningController> miningController;
	private readonly ISaveService saveService;
	private readonly IAudioController audioController;
	private readonly AdRewardSettings settings;
	[Inject] private IAnalyticsService analytics;

	private long mineBoostEndUtcTicks;
	private bool wasMineBoostActive;
	private float nextArkTime;
	private bool waitingForAd;

	public bool IsArkAvailable { get; private set; }

	public long MineBoostEndUtcTicks => mineBoostEndUtcTicks;

	public bool IsMineBoostActive => DateTime.UtcNow.Ticks < mineBoostEndUtcTicks;

	public TimeSpan MineBoostRemaining => IsMineBoostActive ? new TimeSpan(mineBoostEndUtcTicks - DateTime.UtcNow.Ticks) : TimeSpan.Zero;

	public float MiningMultiplier => IsMineBoostActive ? settings.MineBoostMultiplier : 1f;

	public bool IsAdReady => !waitingForAd && adService.IsReady;

	public BoostController(SignalBus signalBus, IPlayerModel playerModel, IRewardedAdService adService, IMothershipController mothershipController,
		LazyInject<IMiningController> miningController, ISaveService saveService, IAudioController audioController, GameSettings gameSettings)
	{
		this.audioController = audioController;
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.adService = adService;
		this.mothershipController = mothershipController;
		this.miningController = miningController;
		this.saveService = saveService;
		settings = gameSettings.AdRewardSettings;
	}

	public void Initialize()
	{
		SaveData save = saveService.Load();
		if (save != null)
		{
			mineBoostEndUtcTicks = save.MineBoostEndUtcTicks;
		}

		wasMineBoostActive = IsMineBoostActive;
		ScheduleArk();
	}

	public void Tick()
	{
		if (!IsArkAvailable && Time.unscaledTime >= nextArkTime)
		{
			IsArkAvailable = true;
			audioController.Play(SoundType.Unlock);
			signalBus.Fire<BoostsUpdatedSignal>();
		}

		// Let views know when the boost runs out
		bool active = IsMineBoostActive;
		if (active != wasMineBoostActive)
		{
			wasMineBoostActive = active;
			signalBus.Fire<BoostsUpdatedSignal>();
		}
	}

	public void WatchMineBoostAd()
	{
		ShowAd("mine_boost", () =>
		{
			// A new ad gives a full boost again, it doesn't add up
			mineBoostEndUtcTicks = DateTime.UtcNow.AddMinutes(settings.MineBoostMinutes).Ticks;
			wasMineBoostActive = true;
			signalBus.Fire(new RewardGrantedSignal() { Message = $"Mining x{settings.MineBoostMultiplier:0.##} for {settings.MineBoostMinutes:0} minutes" });
		});
	}

	public void ClaimArk()
	{
		if (!IsArkAvailable)
		{
			return;
		}

		ShowAd("ark", () =>
		{
			IsArkAvailable = false;
			ScheduleArk();

			if (UnityEngine.Random.value < settings.ArkDarkMatterChance)
			{
				playerModel.AddCurrency(CurrencyType.DarkMatter, settings.ArkDarkMatter);
				analytics.EarnDarkMatter(settings.ArkDarkMatter, "ark");
				signalBus.Fire(new RewardGrantedSignal() { Message = $"+{settings.ArkDarkMatter} Dark Matter" });
			}
			else
			{
				long cash = GetArkCash();
				playerModel.AddMoney(cash);
				analytics.LogEvent("ark_cash", AnalyticsParams.With("value", cash));
				signalBus.Fire(new RewardGrantedSignal() { Message = "+" + NumberFormatter.FormatMoney(cash) });
			}
		});
	}

	// A share of the galaxy value, but never less than a couple of minutes of income
	private long GetArkCash()
	{
		double fromValue = mothershipController.GetGalaxyValue() * (double)settings.ArkCashShareOfGalaxyValue;
		double fromIncome = miningController.Value.GetValuePerSecond() * (double)settings.ArkMinIncomeSeconds;
		return Math.Max(10L, (long)Math.Round(Math.Max(fromValue, fromIncome)));
	}

	private void ShowAd(string placement, Action onRewarded)
	{
		if (!IsAdReady)
		{
			analytics.LogEvent("ad_unavailable", AnalyticsParams.With("placement", placement));
			signalBus.Fire(new PurchaseSignal() { Kind = PurchaseKind.Ad, Success = false, FailMessage = "No ad available right now, try again soon" });
			return;
		}

		waitingForAd = true;
		signalBus.Fire<BoostsUpdatedSignal>();
		analytics.LogEvent("ad_requested", AnalyticsParams.With("placement", placement));
		adService.Show(earned =>
		{
			waitingForAd = false;
			analytics.LogEvent(earned ? "ad_rewarded" : "ad_closed_early", AnalyticsParams.With("placement", placement));
			if (earned)
			{
				onRewarded();
			}

			signalBus.Fire<BoostsUpdatedSignal>();
		});
	}

	private void ScheduleArk()
	{
		nextArkTime = Time.unscaledTime + UnityEngine.Random.Range(settings.ArkMinMinutes, settings.ArkMaxMinutes) * 60f;
	}
}
