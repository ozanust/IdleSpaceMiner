using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class MothershipView : MonoBehaviour, IPointerClickHandler
{
	[Inject] SignalBus signalBus;

	public void OnPointerClick(PointerEventData eventData)
	{
		signalBus.Fire(new MenuOpenSignal() { Type = MenuType.Resources });
	}
}
