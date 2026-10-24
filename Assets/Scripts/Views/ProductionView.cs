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
	[Inject] private DiContainer container;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private TMP_Text panelTitle;

	[SerializeField] private SmelterItem smelterItemPrototype;
	[SerializeField] private GameObject smelterItemContainer;
	[SerializeField] private CrafterItem crafterItemPrototype;
	[SerializeField] private GameObject crafterItemContainer;

	[SerializeField] private Button smeltButton;
	[SerializeField] private Button craftButton;

	[SerializeField] private GameObject smeltLayout;
	[SerializeField] private GameObject craftLayout;

	private MenuType type = MenuType.Production;
	
	[Inject]
	public void Construct(SignalBus sb)
	{
		signalBus = sb;
        
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<SmelterUnlockedSignal>(OnSmelterUnlocked);
		signalBus.Subscribe<CrafterUnlockedSignal>(OnCrafterUnlocked);
		signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
		
		smeltButton.onClick.AddListener(OnSmeltSelected);
		craftButton.onClick.AddListener(OnCraftSelected);
		closeButton.onClick.AddListener(CloseView);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Unsubscribe<SmelterUnlockedSignal>(OnSmelterUnlocked);
		signalBus.Unsubscribe<CrafterUnlockedSignal>(OnCrafterUnlocked);
		signalBus.Unsubscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(!panel.activeInHierarchy);
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
	}

	// Each unlock adds a locked slot for the next smelter/crafter, unless it was the last one
	private void OnSmelterUnlocked(SmelterUnlockedSignal signal)
	{
		SmelterSetting nextSmelter = resourceSettings.GetSmelterSetting(signal.SmelterId + 1);
		if (nextSmelter == null)
		{
			return;
		}

		SmelterItem item = container.InstantiatePrefabForComponent<SmelterItem>(smelterItemPrototype, smelterItemContainer.transform);
		item.SetInjections(signalBus, resourceSettings, productionController, playerModel);
		item.SetSmelterUnlockPrice(nextSmelter.Price);
	}

	private void OnCrafterUnlocked(CrafterUnlockedSignal signal)
	{
		CrafterSetting nextCrafter = resourceSettings.GetCrafterSetting(signal.CrafterId + 1);
		if (nextCrafter == null)
		{
			return;
		}

		CrafterItem item = container.InstantiatePrefabForComponent<CrafterItem>(crafterItemPrototype, crafterItemContainer.transform);
		item.SetInjections(signalBus, resourceSettings, productionController, playerModel);
		item.SetSmelterUnlockPrice(nextCrafter.Price);
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (signal.ResearchType == ResearchType.Crafter)
		{
			craftButton.interactable = true;
		}
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
