using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>Boosts window: watch an ad for the Mine Boost, and claim the Ark when it has arrived.</summary>
public class BoostsView : MonoBehaviour
{
	private const float RefreshInterval = 1f;

	[Inject] SignalBus signalBus;
	[Inject] IBoostController boostController;
	[Inject] GameSettings gameSettings;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private TMP_Text mineBoostDescriptionText;
	[SerializeField] private TMP_Text mineBoostStatusText;
	[SerializeField] private Button mineBoostButton;
	[SerializeField] private TMP_Text arkStatusText;
	[SerializeField] private Button arkButton;

	private float nextRefresh;

	private void Awake()
	{
		closeButton.onClick.AddListener(() => panel.SetActive(false));
		mineBoostButton.onClick.AddListener(() => boostController.WatchMineBoostAd());
		arkButton.onClick.AddListener(() => boostController.ClaimArk());
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<BoostsUpdatedSignal>(Refresh);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Unsubscribe<BoostsUpdatedSignal>(Refresh);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == MenuType.Boosts)
		{
			panel.SetActive(!panel.activeInHierarchy);
			Refresh();
		}
	}

	// The boost timer counts down while open
	private void Update()
	{
		if (panel.activeInHierarchy && Time.unscaledTime >= nextRefresh)
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		nextRefresh = Time.unscaledTime + RefreshInterval;
		AdRewardSettings settings = gameSettings.AdRewardSettings;
		bool adReady = boostController.IsAdReady;

		mineBoostDescriptionText.text = $"Mining x{settings.MineBoostMultiplier:0.##} on all planets for {settings.MineBoostMinutes:0} minutes";
		mineBoostStatusText.text = boostController.IsMineBoostActive
			? "Active, " + NumberFormatter.FormatDuration((float)boostController.MineBoostRemaining.TotalSeconds, true) + " left"
			: "Not active";
		mineBoostButton.interactable = adReady;
		SetLabel(mineBoostButton, !adReady ? "Loading ad..." : boostController.IsMineBoostActive ? "Watch ad to refresh" : "Watch ad");

		bool arkReady = boostController.IsArkAvailable;
		arkStatusText.text = arkReady ? "The Ark has arrived!" : "On its way, it arrives every few minutes";
		arkButton.interactable = arkReady && adReady;
		SetLabel(arkButton, arkReady ? "Watch ad to claim" : "Not here yet");
	}

	private static void SetLabel(Button button, string text)
	{
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			label.text = text;
		}
	}
}
