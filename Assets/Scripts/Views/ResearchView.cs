using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UnityEngine.UI;
using TMPro;

public class ResearchView : MonoBehaviour
{
	[Inject] IPlayerModel playerModel;
	[Inject] SignalBus signalBus;
	[Inject] ResourceSettings resourceSettings;
	[Inject] IProductionController productionController;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private TMP_Text panelTitle;

	private MenuType type = MenuType.Research;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
	}

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
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
}
