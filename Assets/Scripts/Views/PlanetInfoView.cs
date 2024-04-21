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
	[Inject] ISpaceModel spaceModel;

	[SerializeField] private GameObject panel;
	[SerializeField] private GameObject miningInfoPanel;
	[SerializeField] private GameObject updatePanel;
	[SerializeField] private MiningInfoItem miningInfoItemPrototype;
	[SerializeField] private TMP_Text planetNameText;
	[SerializeField] private Button closeButton;

	[SerializeField] private TMP_Text miningRateLevelText;
	[SerializeField] private TMP_Text miningRateText;
	[SerializeField] private TMP_Text miningRateUpdatePriceText;
	[SerializeField] private Button miningRateUpdateButton;

	[SerializeField] private TMP_Text shipSpeedLevelText;
	[SerializeField] private TMP_Text shipSpeedText;
	[SerializeField] private TMP_Text shipSpeedUpdatePriceText;
	[SerializeField] private Button shipSpeedUpdateButton;

	[SerializeField] private TMP_Text shipCargoLevelText;
	[SerializeField] private TMP_Text shipCargoText;
	[SerializeField] private TMP_Text shipCargoUpdatePriceText;
	[SerializeField] private Button shipCargoUpdateButton;

	private Dictionary<ResourceType, MiningInfoItem> infoItems = new Dictionary<ResourceType, MiningInfoItem>();

	private MiningData data;
	private int openPlanetId = -1;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		miningRateUpdateButton.onClick.AddListener(OnMiningRateUpdateButtonClick);
		shipSpeedUpdateButton.onClick.AddListener(OnShipSpeedUpdateButtonClick);
		shipCargoUpdateButton.onClick.AddListener(OnShipCargoUpdateButtonClick);
		signalBus.Subscribe<PlanetOpenSignal>(OnPlanetOpen);
		signalBus.Subscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
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

		PlanetData planetData;
		if (spaceModel.TryGetPlanetData(signal.PlanetId, out planetData))
		{
			miningRateLevelText.text = string.Format("{0} {1}", "Lv.", planetData.MiningRateLevel);
			miningRateText.text = string.Format("{0} / {1}", planetData.CurrentTotalMiningRate, "sec");
			miningRateUpdatePriceText.text = string.Format("{0}{1}", "$", planetData.TotalMiningRateUpdatePrice);

			shipSpeedLevelText.text = string.Format("{0} {1}", "Lv.", planetData.ShipSpeedLevel);
			shipSpeedText.text = string.Format("{0} {1}", planetData.CurrentShipSpeed, "mkph");
			shipSpeedUpdatePriceText.text = string.Format("{0}{1}", "$", planetData.ShipSpeedUpdatePrice);

			shipCargoLevelText.text = string.Format("{0} {1}", "Lv.", planetData.ShipCargoLevel);
			shipCargoText.text = planetData.CurrentShipCargo.ToString();
			shipCargoUpdatePriceText.text = string.Format("{0}{1}", "$", planetData.ShipCargoUpdatePrice);
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

	private void UpdateMiningDataTypeViews(PlanetMineData[] mineDatas)
	{
		foreach (PlanetMineData pmd in mineDatas)
		{
			ResourceDataSetting resData = resourceSettings.GetResourceData(pmd.Type);
			infoItems[pmd.Type].UpdateMiningRate(data.TotalMineRate * pmd.Ratio);
		}
	}

	private void OnPlanetUpdated(PlanetUpdatedSignal signal)
	{
		if (signal.PlanetId != openPlanetId)
		{
			return;
		}

		PlanetData planetData;
		if (spaceModel.TryGetPlanetData(signal.PlanetId, out planetData))
		{
			miningRateLevelText.text = string.Format("{0} {1}", "Lv.", planetData.MiningRateLevel);
			miningRateText.text = string.Format("{0} / {1}", planetData.CurrentTotalMiningRate, "sec");
			miningRateUpdatePriceText.text = string.Format("{0}{1}", "$", planetData.TotalMiningRateUpdatePrice);

			shipSpeedLevelText.text = string.Format("{0} {1}", "Lv.", planetData.ShipSpeedLevel);
			shipSpeedText.text = string.Format("{0} {1}", planetData.CurrentShipSpeed, "mkph");
			shipSpeedUpdatePriceText.text = string.Format("{0}{1}", "$", planetData.ShipSpeedUpdatePrice);

			shipCargoLevelText.text = string.Format("{0} {1}", "Lv.", planetData.ShipCargoLevel);
			shipCargoText.text = planetData.CurrentShipCargo.ToString();
			shipCargoUpdatePriceText.text = string.Format("{0}{1}", "$", planetData.ShipCargoUpdatePrice);
		}

		MiningData tempData;
		if (miningController.TryGetMiningData(signal.PlanetId, out tempData))
		{
			data = tempData;
			UpdateMiningDataTypeViews(data.MineDatas);
		}
	}

	private void OnMiningRateUpdateButtonClick()
	{
		planetInfoController.UpdateMiningRate(openPlanetId);
	}

	private void OnShipSpeedUpdateButtonClick()
	{
		planetInfoController.UpdateShipSpeed(openPlanetId);
	}

	private void OnShipCargoUpdateButtonClick()
	{
		planetInfoController.UpdateCargoSize(openPlanetId);
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
