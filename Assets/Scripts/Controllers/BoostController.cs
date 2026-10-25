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
		ShowAd(() =>
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

		ShowAd(() =>
		{
			IsArkAvailable = false;
			ScheduleArk();

			if (UnityEngine.Random.value < settings.ArkDarkMatterChance)
			{
				playerModel.AddCurrency(CurrencyType.DarkMatter, settings.ArkDarkMatter);
				signalBus.Fire(new RewardGrantedSignal() { Message = $"+{settings.ArkDarkMatter} Dark Matter" });
			}
			else
			{
				long cash = GetArkCash();
				playerModel.AddMoney(cash);
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

	private void ShowAd(Action onRewarded)
	{
		if (!IsAdReady)
		{
			signalBus.Fire(new PurchaseSignal() { Kind = PurchaseKind.Ad, Success = false, FailMessage = "No ad available right now, try again soon" });
			return;
		}

		waitingForAd = true;
		signalBus.Fire<BoostsUpdatedSignal>();
		adService.Show(earned =>
		{
			waitingForAd = false;
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
