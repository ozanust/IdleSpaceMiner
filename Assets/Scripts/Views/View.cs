using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class View : MonoBehaviour
{
    [Inject] protected SignalBus signalBus;

	[SerializeField] private GameObject panel;
	[SerializeField] protected Button closeButton;
	[SerializeField] protected MenuType type = MenuType.None;

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
		// Opening another window closes this one
		if (MenuTypes.ClosesFor(signal.Type, type))
		{
			panel.SetActive(false);
			return;
		}

		if (signal.Type == type)
		{
			panel.SetActive(!panel.activeInHierarchy);
		}
	}
	
	public virtual void CloseView()
	{
		panel.SetActive(false);
	}
}
