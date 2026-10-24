using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>
/// Mothership window: sell the galaxy for Dark Matter, and upgrade the rooms it buys.
/// Selling asks for confirmation first.
/// </summary>
public class MothershipRoomsView : MonoBehaviour
{
	private const float RefreshInterval = 1f;

	[Inject] SignalBus signalBus;
	[Inject] IMothershipController mothershipController;
	[Inject] IPlayerModel playerModel;
	[Inject] GameSettings gameSettings;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private Transform itemContainer;
	[SerializeField] private RoomItem itemPrototype;
	[SerializeField] private TMP_Text galaxyValueText;
	[SerializeField] private TMP_Text galaxiesText;
	[SerializeField] private Button sellButton;
	[SerializeField] private GameObject confirmPanel;
	[SerializeField] private TMP_Text confirmText;
	[SerializeField] private Button confirmSellButton;
	[SerializeField] private Button cancelSellButton;

	private readonly List<RoomItem> items = new List<RoomItem>();
	private float nextRefresh;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		sellButton.onClick.AddListener(OpenConfirm);
		cancelSellButton.onClick.AddListener(() => confirmPanel.SetActive(false));
		confirmSellButton.onClick.AddListener(() => mothershipController.SellGalaxy());
		itemPrototype.gameObject.SetActive(false);

		foreach (RoomSetting room in gameSettings.PrestigeSettings.Rooms)
		{
			RoomItem item = Instantiate(itemPrototype, itemContainer);
			item.gameObject.SetActive(true);
			item.Init(room.Type, room.Name, OnUpgrade);
			items.Add(item);
		}

		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<MothershipUpdatedSignal>(Refresh);
		signalBus.Subscribe<PlayerDarkMatterUpdatedSignal>(Refresh);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Unsubscribe<MothershipUpdatedSignal>(Refresh);
		signalBus.Unsubscribe<PlayerDarkMatterUpdatedSignal>(Refresh);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == MenuType.Mothership)
		{
			panel.SetActive(!panel.activeInHierarchy);
			confirmPanel.SetActive(false);
			Refresh();
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
	}

	// Galaxy value grows with every purchase, so keep it current while open
	private void Update()
	{
		if (panel.activeInHierarchy && Time.unscaledTime >= nextRefresh)
		{
			Refresh();
		}
	}

	private void OpenConfirm()
	{
		confirmText.text = $"Sell this galaxy for {mothershipController.GetSellReward()} Dark Matter?\n"
			+ "Planets, money, resources and research start over. Dark Matter, rooms, managers and achievements stay.";
		confirmPanel.SetActive(true);
	}

	private void OnUpgrade(RoomType type)
	{
		mothershipController.TryUpgradeRoom(type);
	}

	private void Refresh()
	{
		nextRefresh = Time.unscaledTime + RefreshInterval;

		galaxyValueText.text = "Galaxy value: " + NumberFormatter.FormatMoney(mothershipController.GetGalaxyValue());
		galaxiesText.text = "Galaxies sold: " + mothershipController.GalaxiesSold;

		int reward = mothershipController.GetSellReward();
		bool canSell = mothershipController.CanSellGalaxy();
		sellButton.interactable = canSell;
		SetLabel(sellButton, canSell
			? $"Sell galaxy  +{reward} DM"
			: $"Sell galaxy at {gameSettings.PrestigeSettings.MinimumReward} DM ({reward} now)");

		int darkMatter = playerModel.GetCurrency(CurrencyType.DarkMatter);
		foreach (RoomItem item in items)
		{
			RoomSetting room = mothershipController.GetRoom(item.Type);
			int level = mothershipController.GetRoomLevel(item.Type);
			int cost = mothershipController.GetUpgradeCost(item.Type);
			string bonus = $"{room.Effect} +{Mathf.RoundToInt(room.BonusPerLevel * level * 100)}%";
			if (cost >= 0)
			{
				bonus += $"  >  +{Mathf.RoundToInt(room.BonusPerLevel * (level + 1) * 100)}%";
			}

			item.Set($"Lv. {level} / {room.MaxLevel}", bonus, cost >= 0 ? $"Upgrade  {cost} DM" : "Max level", cost >= 0 && darkMatter >= cost);
		}
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
