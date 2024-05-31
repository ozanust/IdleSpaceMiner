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
	[SerializeField] private Button closeButton;

	[SerializeField] private TMP_Text panelTitle;
	[SerializeField] private Image researchIcon;
	[SerializeField] private TMP_Text researchDescription;
	[SerializeField] private ResearchRequirementInfoItem itemPrototype;
	[SerializeField] private VerticalLayoutGroup requirementsInfoContainer;

	private MenuType type = MenuType.ResarchInfo;

	private List<ResearchRequirementInfoItem> infoItemPool = new List<ResearchRequirementInfoItem>();

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
	}

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<ResearchInfoOpenSignal>(OnResearchInfoOpen);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(true);
		}
	}

	private void OnResearchInfoOpen(ResearchInfoOpenSignal signal)
	{
		ResearchSettings settings = resourceSettings.GetResearchSetting(signal.Type);
		panelTitle.text = settings.Name;
		researchIcon.sprite = settings.Icon;
		researchDescription.text = settings.Description;

		int itemCountToReuse = settings.NeededResources.Length < infoItemPool.Count ? settings.NeededResources.Length : infoItemPool.Count;

		for (int i = 0; i < itemCountToReuse; i++)
		{
			ResearchRequirementInfoItem item = infoItemPool[i];
			item.Initialize(resourceSettings.GetResourceData(settings.NeededResources[i].Type).Icon, playerModel.GetResource(settings.NeededResources[i].Type).ToString(), settings.NeededResources[i].Amount.ToString());
			item.gameObject.SetActive(true);
		}

		for (int i = infoItemPool.Count; i < settings.NeededResources.Length; i++)
		{
			ResearchRequirementInfoItem item = Instantiate(itemPrototype, requirementsInfoContainer.transform);
			item.Initialize(resourceSettings.GetResourceData(settings.NeededResources[i].Type).Icon, playerModel.GetResource(settings.NeededResources[i].Type).ToString(), settings.NeededResources[i].Amount.ToString());
			infoItemPool.Add(item);
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
		DeactivateAllItems();
	}

	private void DeactivateAllItems()
	{
		for (int i = 0; i < infoItemPool.Count; i++)
		{
			infoItemPool[i].gameObject.SetActive(false);
		}
	}
}
