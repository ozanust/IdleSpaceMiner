using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>
/// Managers window: hire managers with Dark Matter, buy slots, promote and unassign.
/// Opened from a planet panel it is in assign mode and puts the picked manager on that planet.
/// </summary>
public class ManagersView : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] IManagerController managerController;
	[Inject] IPlayerModel playerModel;
	[Inject] PlanetSettings planetSettings;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private Transform itemContainer;
	[SerializeField] private ManagerItem itemPrototype;
	[SerializeField] private TMP_Text emptyText;
	[SerializeField] private TMP_Text slotsText;
	[SerializeField] private TMP_Text statusText;
	[SerializeField] private Button buySlotButton;

	// One per school, in ManagerSchool order
	[SerializeField] private Button[] hireButtons;

	private readonly List<ManagerItem> items = new List<ManagerItem>();
	private int assignPlanetId = -1;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		buySlotButton.onClick.AddListener(OnClickBuySlot);
		for (int i = 0; i < hireButtons.Length; i++)
		{
			ManagerSchool school = (ManagerSchool)i;
			hireButtons[i].onClick.AddListener(() => OnClickHire(school));
		}

		itemPrototype.gameObject.SetActive(false);
	}

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<ManagerAssignRequestSignal>(OnAssignRequest);
		signalBus.Subscribe<ManagersUpdatedSignal>(OnChanged);
		signalBus.Subscribe<PlayerDarkMatterUpdatedSignal>(OnChanged);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Unsubscribe<ManagerAssignRequestSignal>(OnAssignRequest);
		signalBus.Unsubscribe<ManagersUpdatedSignal>(OnChanged);
		signalBus.Unsubscribe<PlayerDarkMatterUpdatedSignal>(OnChanged);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == MenuType.Managers)
		{
			assignPlanetId = -1;
			statusText.text = "";
			panel.SetActive(!panel.activeInHierarchy);
			Refresh();
		}
	}

	private void OnAssignRequest(ManagerAssignRequestSignal signal)
	{
		assignPlanetId = signal.PlanetId;
		statusText.text = $"Pick a manager for {GetPlanetName(signal.PlanetId)}";
		panel.SetActive(true);
		Refresh();
	}

	private void OnChanged()
	{
		if (panel.activeInHierarchy)
		{
			Refresh();
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
		assignPlanetId = -1;
	}

	private void OnClickHire(ManagerSchool school)
	{
		ManagerData hired;
		statusText.text = managerController.TryHire(school, out hired)
			? $"Hired a {hired.Stars} Star manager"
			: "Not enough Dark Matter";
	}

	private void OnClickBuySlot()
	{
		if (!managerController.TryBuySlot())
		{
			statusText.text = "Not enough Dark Matter";
		}
	}

	private void OnClickAction(ManagerData manager)
	{
		if (assignPlanetId < 0)
		{
			managerController.Unassign(manager.Id);
			return;
		}

		if (managerController.TryAssign(manager.Id, assignPlanetId))
		{
			CloseView();
		}
		else
		{
			statusText.text = "No free slot, buy one or unassign a manager";
		}
	}

	private void OnClickPromote(ManagerData manager)
	{
		if (managerController.TryPromote(manager.Id))
		{
			statusText.text = $"Promoted to {manager.Stars} Star";
		}
	}

	private void Refresh()
	{
		int darkMatter = playerModel.GetCurrency(CurrencyType.DarkMatter);

		slotsText.text = $"Slots {managerController.AssignedCount} / {managerController.Slots}";
		int slotCost = managerController.GetNextSlotCost();
		SetButton(buySlotButton, $"+1 Slot  {slotCost} DM", darkMatter >= slotCost);

		for (int i = 0; i < hireButtons.Length; i++)
		{
			ManagerSchoolSetting school = managerController.GetSchool((ManagerSchool)i);
			SetButton(hireButtons[i], $"{school.Name}\n{school.Cost} DM", darkMatter >= school.Cost);
		}

		IReadOnlyList<ManagerData> managers = managerController.Managers;
		while (items.Count < managers.Count)
		{
			ManagerItem item = Instantiate(itemPrototype, itemContainer);
			item.Init(OnClickAction, OnClickPromote);
			items.Add(item);
		}

		for (int i = 0; i < items.Count; i++)
		{
			bool used = i < managers.Count;
			items[i].gameObject.SetActive(used);
			if (used)
			{
				SetItem(items[i], managers[i]);
			}
		}

		emptyText.gameObject.SetActive(managers.Count == 0);
	}

	private void SetItem(ManagerItem item, ManagerData manager)
	{
		bool assigned = manager.PlanetId >= 0;
		string assignment = assigned ? $"Working on {GetPlanetName(manager.PlanetId)}" : "Idle";

		string actionText;
		bool actionEnabled = true;
		if (assignPlanetId >= 0)
		{
			actionText = manager.PlanetId == assignPlanetId ? "Assigned" : "Assign here";
			actionEnabled = manager.PlanetId != assignPlanetId;
		}
		else
		{
			actionText = assigned ? "Unassign" : null;
		}

		item.Set(manager, managerController.GetPrimaryText(manager), managerController.GetSecondaryText(manager),
			assignment, actionText, actionEnabled, managerController.CanPromote(manager.Id));
	}

	private string GetPlanetName(int planetId)
	{
		PlanetDataSetting setting;
		return planetSettings.TryGetPlanetSetting(planetId, out setting) ? setting.Name : $"Planet {planetId + 1}";
	}

	private static void SetButton(Button button, string label, bool interactable)
	{
		button.interactable = interactable;
		TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
		if (text != null)
		{
			text.text = label;
		}
	}
}
