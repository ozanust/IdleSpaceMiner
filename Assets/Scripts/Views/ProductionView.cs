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

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private TMP_Text panelTitle;

	private MenuType type = MenuType.Production;

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
			panel.SetActive(true);
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
		signalBus.Fire<ResourcesViewClosedSignal>();
	}
}
