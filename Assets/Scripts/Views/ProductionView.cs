using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

public class ProductionView : MonoBehaviour
{
    [Inject] IPlayerModel playerModel;
    [Inject] SignalBus signalBus;
    [Inject] ResourceSettings resourceSettings;
	[Inject] IProductionController productionController;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private TMP_Text panelTitle;

	[SerializeField] private SmelterItem smelterItemPrototype;
	[SerializeField] private GameObject smelterItemContainer;

	[SerializeField] private Button smeltButton;
	[SerializeField] private Button craftButton;

	[SerializeField] private GameObject smeltLayout;
	[SerializeField] private GameObject craftLayout;

	private MenuType type = MenuType.Production;

	private void Awake()
	{
		smeltButton.onClick.AddListener(OnSmeltSelected);
		craftButton.onClick.AddListener(OnCraftSelected);
		closeButton.onClick.AddListener(CloseView);
	}

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<SmelterUnlockedSignal>(OnSmelterUnlocked);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(true);
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
	}

	private void OnSmelterUnlocked(SmelterUnlockedSignal signal)
	{
		SmelterItem item = Instantiate(smelterItemPrototype, smelterItemContainer.transform);
		item.SetInjections(signalBus, resourceSettings, productionController, playerModel);
		item.SetSmelterUnlockPrice(resourceSettings.GetSmelterSetting(signal.SmelterId + 1).Price);
	}

	private void OnSmeltSelected()
	{
		smeltLayout.SetActive(true);
		craftLayout.SetActive(false);
	}

	private void OnCraftSelected()
	{
		smeltLayout.SetActive(false);
		craftLayout.SetActive(true);
	}
}
