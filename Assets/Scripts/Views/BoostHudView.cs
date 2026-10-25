using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>
/// HUD parts of the ad boosts: the Ark button that pops up when the Ark arrives, and a small
/// timer while the Mine Boost is running.
/// </summary>
public class BoostHudView : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] IBoostController boostController;
	[Inject] GameSettings gameSettings;

	[SerializeField] private Button arkButton;
	[SerializeField] private TMP_Text boostTimerText;

	private RectTransform arkRect;
	private float nextTimerUpdate;

	private void Awake()
	{
		arkRect = (RectTransform)arkButton.transform;
		arkButton.onClick.AddListener(() => boostController.ClaimArk());
		signalBus.Subscribe<BoostsUpdatedSignal>(Refresh);
	}

	private void Start()
	{
		Refresh();
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<BoostsUpdatedSignal>(Refresh);
	}

	private void Refresh()
	{
		arkButton.gameObject.SetActive(boostController.IsArkAvailable);
		arkButton.interactable = boostController.IsAdReady;
		UpdateTimer();
	}

	private void Update()
	{
		// A gentle bounce so the Ark gets noticed
		if (arkButton.gameObject.activeSelf)
		{
			arkRect.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f));
		}

		if (Time.unscaledTime >= nextTimerUpdate)
		{
			UpdateTimer();
		}
	}

	private void UpdateTimer()
	{
		nextTimerUpdate = Time.unscaledTime + 1f;
		bool active = boostController.IsMineBoostActive;
		boostTimerText.gameObject.SetActive(active);
		if (active)
		{
			boostTimerText.text = $"Mining x{gameSettings.AdRewardSettings.MineBoostMultiplier:0.##}  "
				+ NumberFormatter.FormatDuration((float)boostController.MineBoostRemaining.TotalSeconds, true);
		}
	}
}
