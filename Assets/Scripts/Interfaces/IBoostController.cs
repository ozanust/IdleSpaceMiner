using System;

public interface IBoostController
{
	/// <summary>Mining speed from the ad Mine Boost, 1 when it isn't running.</summary>
	float MiningMultiplier { get; }

	bool IsMineBoostActive { get; }
	TimeSpan MineBoostRemaining { get; }
	long MineBoostEndUtcTicks { get; }

	bool IsArkAvailable { get; }

	/// <summary>Whether an ad can be shown right now.</summary>
	bool IsAdReady { get; }

	/// <summary>Shows an ad and starts or refreshes the Mine Boost when it was watched.</summary>
	void WatchMineBoostAd();

	/// <summary>Shows an ad and gives the Ark reward when it was watched.</summary>
	void ClaimArk();
}
