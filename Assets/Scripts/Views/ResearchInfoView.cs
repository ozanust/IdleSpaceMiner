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

		foreach (ResearchNeededResource resource in settings.NeededResources)
		{
			ResearchRequirementInfoItem item = Instantiate(itemPrototype, requirementsInfoContainer.transform);
			item.Initialize(resourceSettings.GetResourceData(resource.Type).Icon, playerModel.GetResource(resource.Type).ToString(), resource.Amount.ToString());
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
	}
}
