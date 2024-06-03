using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ResearchItem : MonoBehaviour
{
	[Inject] IPlayerModel playerModel;
	[Inject] SignalBus signalBus;
	[Inject] ResourceSettings resourceSettings;
	[SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private Button button;

    [SerializeField] ResearchType type;

	bool isResearchUnlockable;
	private ResearchSettings settings;

	private void Awake()
	{
		button.onClick.AddListener(OnClickButton);
	}

	private void Start()
	{
		settings = resourceSettings.GetResearchSetting(type);

		isResearchUnlockable = settings.RequiredResearch == type;
		if (isResearchUnlockable)
		{
			borderImage.color = Color.white;
			iconImage.color = Color.white;
		}

		signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
		signalBus.Subscribe<PlayerModelUpdatedSignal>(OnPlayerModelUpdated);
		Init();
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (signal.ResearchType == type)
		{
			borderImage.color = Color.green;
			return;
		}

		if (isResearchUnlockable)
		{
			return;
		}

		isResearchUnlockable = signal.ResearchType == settings.RequiredResearch;

		if (isResearchUnlockable)
		{
			borderImage.color = Color.white;
			iconImage.color = Color.white;
		}
	}

	private void OnPlayerModelUpdated(PlayerModelUpdatedSignal signal)
	{
		if (isResearchUnlockable)
		{
			if (playerModel.HasResources(settings.NeededResources))
			{
				borderImage.color = Color.cyan;
			}
		}
	}

	private void Init()
	{
		if (playerModel.IsResearchUnlocked(type))
		{
			borderImage.color = Color.green;
			iconImage.color = Color.white;
		}
		else if (playerModel.IsResearchUnlocked(resourceSettings.GetResearchSetting(type).RequiredResearch))
		{
			borderImage.color = Color.white;
			iconImage.color = Color.white;
		}
	}

	private void OnClickButton()
	{
		signalBus.Fire(new MenuOpenSignal() { Type = MenuType.ResarchInfo });
		signalBus.Fire(new ResearchInfoOpenSignal() { Type = type });
	}
}
