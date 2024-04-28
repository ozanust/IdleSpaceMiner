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

	private void Awake()
	{
		
	}

	private void Start()
	{
		// subscribe to signals
	}
}
