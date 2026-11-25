using System;

/// <summary>Shows rewarded video ads. AdMob in builds with the SDK, a simulated ad otherwise.</summary>
public interface IRewardedAdService
{
	bool IsReady { get; }

	/// <summary>Shows an ad; the callback gets true only if the player earned the reward.</summary>
	void Show(Action<bool> onFinished);
}
