using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Zenject;
using UnityEngine.UI;

public class PlanetPropertyUpdateView : MonoBehaviour
{
    [Inject] PlanetSettings planetSettings;
    [Inject] IPlanetInfoController planetInfoController;
    [Inject] SignalBus signalBus;
    [Inject] ResourceSettings resourceSettings;

	private void Awake()
	{
		
	}

	void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void RefreshData()
	{

	}
}
