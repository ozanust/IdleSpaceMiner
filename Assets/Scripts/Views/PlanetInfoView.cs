using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Zenject;
using UnityEngine.UI;

public class PlanetInfoView : MonoBehaviour
{
	[Inject] PlanetSettings planetSettings;
	[Inject] IMiningController miningController;
    [Inject] IPlanetInfoController planetInfoController;
    [Inject] SignalBus signalBus;
    [Inject] ResourceSettings resourceSettings;

	[SerializeField] private GameObject panel;
	[SerializeField] private GameObject miningInfoPanel;
	[SerializeField] private GameObject updatePanel;
	[SerializeField] private MiningInfoItem miningInfoItemPrototype;
	[SerializeField] private TMP_Text planetNameText;
	[SerializeField] private Button closeButton;

	private Dictionary<ResourceType, MiningInfoItem> infoItems = new Dictionary<ResourceType, MiningInfoItem>();

	private MiningData data;
	private int openPlanetId = -1;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		signalBus.Subscribe<PlanetOpenSignal>(OnPlanetOpen);
	}

	private void OnPlanetOpen(PlanetOpenSignal signal)
	{
		if (openPlanetId == signal.PlanetId) 
		{
			return;
		}

		CloseView();

		MiningData tempData;
		if (miningController.TryGetMiningData(signal.PlanetId, out tempData))
		{
			data = tempData;
			ActivateMiningDataTypeViews(data.MineDatas);
		}

		PlanetDataSetting settings;
		if (data != null && planetSettings.TryGetPlanetSetting(signal.PlanetId, out settings))
		{
			planetNameText.text = settings.Name;
			openPlanetId = signal.PlanetId;
		}
		else
		{
			return;
		}
		
		panel.SetActive(true);
	}

	private void ActivateMiningDataTypeViews(PlanetMineData[] mineDatas)
	{
		foreach (PlanetMineData pmd in mineDatas)
		{
			if (!infoItems.ContainsKey(pmd.Type))
			{
				MiningInfoItem item = Instantiate(miningInfoItemPrototype, miningInfoPanel.transform);
				ResourceDataSetting resData = resourceSettings.GetResourceData(pmd.Type);
				item.Initialize(pmd, resData.Icon, resData.Name, pmd.Ratio, data.TotalMineRate * pmd.Ratio, pmd.MinedAmount);
				infoItems.Add(pmd.Type, item);
			}
			else
			{
				ResourceDataSetting resData = resourceSettings.GetResourceData(pmd.Type);
				infoItems[pmd.Type].Initialize(pmd, resData.Icon, resData.Name, pmd.Ratio, data.TotalMineRate * pmd.Ratio, pmd.MinedAmount);
			}
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
		openPlanetId = -1;
		data = null;

		foreach(MiningInfoItem mi in infoItems.Values)
		{
			mi.gameObject.SetActive(false);
		}
	}
}
