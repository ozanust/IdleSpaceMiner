using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UnityEngine.UI;
using TMPro;

public class ResearchInfoView : MonoBehaviour
{
	[Inject] IPlayerModel playerModel;
	[Inject] SignalBus signalBus;
	[Inject] ResourceSettings resourceSettings;
	[Inject] IProductionController productionController;

	[SerializeField] private GameObject panel;
	[SerializeField] private GameObject projectAlreadyResearchedPanel;
	[SerializeField] private Button closeButton;
	[SerializeField] private Button researchButton;
	[SerializeField] private TMP_Text researchButtonText;

	[SerializeField] private TMP_Text panelTitle;
	[SerializeField] private Image researchIcon;
	[SerializeField] private TMP_Text researchDescription;
	[SerializeField] private ResearchRequirementInfoItem itemPrototype;
	[SerializeField] private VerticalLayoutGroup requirementsInfoContainer;

	private MenuType type = MenuType.ResarchInfo;
	private ResearchType researchType;
	private ResearchSettings settings;

	private Dictionary<ResourceType, ResearchRequirementInfoItem> items = new Dictionary<ResourceType, ResearchRequirementInfoItem>();
	private List<ResearchRequirementInfoItem> infoItemPool = new List<ResearchRequirementInfoItem>();

	private bool isOpen = false;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		researchButton.onClick.AddListener(OnClickUnlock);
	}

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<ResearchInfoOpenSignal>(OnResearchInfoOpen);
		signalBus.Subscribe<PlayerModelUpdatedSignal>(OnPlayerModelUpdated);
		signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(true);
			isOpen = true;
		}
	}

	private void OnResearchInfoOpen(ResearchInfoOpenSignal signal)
	{
		researchType = signal.Type;
		settings = resourceSettings.GetResearchSetting(signal.Type);
		panelTitle.text = settings.Name;
		researchIcon.sprite = settings.Icon;
		researchDescription.text = settings.Description;

		if (playerModel.IsResearchUnlocked(settings.Type))
		{
			projectAlreadyResearchedPanel.SetActive(true);
			researchButton.gameObject.SetActive(false);
			researchType = signal.Type;
			return;
		}

		bool isResearchUnlockable = playerModel.IsResearchUnlocked(settings.RequiredResearch) || settings.RequiredResearch == researchType;
		researchButtonText.text = isResearchUnlockable ? "Research" : "Not Available";

		if (isResearchUnlockable && playerModel.HasResources(settings.NeededResources))
		{
			researchButton.interactable = true;
		}

		int itemCountToReuse = settings.NeededResources.Length < infoItemPool.Count ? settings.NeededResources.Length : infoItemPool.Count;

		for (int i = 0; i < itemCountToReuse; i++)
		{
			ResearchRequirementInfoItem item = infoItemPool[i];
			item.Initialize(resourceSettings.GetResourceData(settings.NeededResources[i].Type).Icon, playerModel.GetResource(settings.NeededResources[i].Type), settings.NeededResources[i].Amount);
			item.gameObject.SetActive(true);
			items.Add(settings.NeededResources[i].Type, item);
		}

		for (int i = infoItemPool.Count; i < settings.NeededResources.Length; i++)
		{
			ResearchRequirementInfoItem item = Instantiate(itemPrototype, requirementsInfoContainer.transform);
			item.Initialize(resourceSettings.GetResourceData(settings.NeededResources[i].Type).Icon, playerModel.GetResource(settings.NeededResources[i].Type), settings.NeededResources[i].Amount);
			infoItemPool.Add(item);
			items.Add(settings.NeededResources[i].Type, item);
		}
	}

	private void OnClickUnlock()
	{
		// One more check just in case
		if (playerModel.HasResources(settings.NeededResources))
		{
			playerModel.UnlockResearch(researchType);
		}
	}

	private void OnPlayerModelUpdated(PlayerModelUpdatedSignal signal)
	{
		if (!isOpen)
		{
			return;
		}

		if (items.ContainsKey(signal.UpdatedResourceType))
		{
			items[signal.UpdatedResourceType].SetHoldingAmount(playerModel.GetResource(signal.UpdatedResourceType));
		}

		if (settings != null)
		{
			bool isResearchUnlockable = playerModel.IsResearchUnlocked(settings.RequiredResearch) || settings.RequiredResearch == researchType;
			researchButtonText.text = isResearchUnlockable ? "Research" : "Not Available";

			if (isResearchUnlockable && playerModel.HasResources(settings.NeededResources))
			{
				researchButton.interactable = true;
			}
			else
			{
				researchButton.interactable = false;
			}
		}
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (!isOpen)
		{
			return;
		}

		if (signal.ResearchType == researchType)
		{
			CloseView();
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
		isOpen = false;
		DeactivateAllItems();
	}

	private void DeactivateAllItems()
	{
		for (int i = 0; i < infoItemPool.Count; i++)
		{
			infoItemPool[i].gameObject.SetActive(false);
		}

		projectAlreadyResearchedPanel.SetActive(false);
		researchButton.gameObject.SetActive(true);
		researchButton.interactable = false;
		items.Clear();
	}
}
