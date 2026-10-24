using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Zenject;
using UnityEngine.UI;

/// <summary>Planet panel: image and name, ores mined, assigned manager and the three upgrades.</summary>
public class PlanetInfoView : MonoBehaviour
{
	[Inject] PlanetSettings planetSettings;
	[Inject] IMiningController miningController;
    [Inject] IPlanetInfoController planetInfoController;
    [Inject] IManagerController managerController;
    [Inject] SignalBus signalBus;
    [Inject] ResourceSettings resourceSettings;
	[Inject] ISpaceModel spaceModel;

	[SerializeField] private GameObject panel;
	[SerializeField] private GameObject miningInfoPanel;
	[SerializeField] private GameObject updatePanel;
	[SerializeField] private MiningInfoItem miningInfoItemPrototype;
	[SerializeField] private Image planetImage;
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

	[Header("Optional")]
	[SerializeField] private TMP_Text managerText;
	[SerializeField] private Button managerButton;

	private Dictionary<ResourceType, MiningInfoItem> infoItems = new Dictionary<ResourceType, MiningInfoItem>();

	private MiningData data;
	private int openPlanetId = -1;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		miningRateUpdateButton.onClick.AddListener(OnMiningRateUpdateButtonClick);
		TutorialTarget.Mark(miningRateUpdateButton.gameObject, TutorialTargetId.MiningUpgradeButton);
		shipSpeedUpdateButton.onClick.AddListener(OnShipSpeedUpdateButtonClick);
		shipCargoUpdateButton.onClick.AddListener(OnShipCargoUpdateButtonClick);
		if (managerButton != null)
		{
			managerButton.onClick.AddListener(OnManagerButtonClick);
		}

		signalBus.Subscribe<PlanetOpenSignal>(OnPlanetOpen);
		signalBus.Subscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
		signalBus.Subscribe<ManagersUpdatedSignal>(OnManagersUpdated);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<PlanetOpenSignal>(OnPlanetOpen);
		signalBus.Unsubscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
		signalBus.Unsubscribe<ManagersUpdatedSignal>(OnManagersUpdated);
	}

	private void OnPlanetOpen(PlanetOpenSignal signal)
	{
		if (openPlanetId == signal.PlanetId)
		{
			return;
		}

		CloseView();

		PlanetDataSetting settings;
		if (!miningController.TryGetMiningData(signal.PlanetId, out data) || !planetSettings.TryGetPlanetSetting(signal.PlanetId, out settings))
		{
			return;
		}

		openPlanetId = signal.PlanetId;
		planetNameText.text = settings.Name;
		planetImage.sprite = settings.Icon;
		Refresh();
		panel.SetActive(true);
	}

	private void OnPlanetUpdated(PlanetUpdatedSignal signal)
	{
		if (signal.PlanetId == openPlanetId)
		{
			Refresh();
		}
	}

	private void OnManagersUpdated()
	{
		if (openPlanetId >= 0)
		{
			Refresh();
		}
	}

	/// <summary>Shows the planet's values including research and manager bonuses.</summary>
	private void Refresh()
	{
		PlanetData planetData;
		if (!spaceModel.TryGetPlanetData(openPlanetId, out planetData))
		{
			return;
		}

		float mining = miningController.GetMiningMultiplier(openPlanetId);
		miningRateLevelText.text = string.Format("{0} {1}", "Lv.", planetData.MiningRateLevel);
		miningRateText.text = string.Format("{0} / {1}", NumberFormatter.Format(planetData.CurrentTotalMiningRate * mining), "sec");
		miningRateUpdatePriceText.text = NumberFormatter.FormatMoney(planetData.TotalMiningRateUpdatePrice);

		shipSpeedLevelText.text = string.Format("{0} {1}", "Lv.", planetData.ShipSpeedLevel);
		shipSpeedText.text = string.Format("{0} {1}", NumberFormatter.Format(planetData.CurrentShipSpeed * managerController.GetShipSpeedMultiplier(openPlanetId)), "mkph");
		shipSpeedUpdatePriceText.text = NumberFormatter.FormatMoney(planetData.ShipSpeedUpdatePrice);

		shipCargoLevelText.text = string.Format("{0} {1}", "Lv.", planetData.ShipCargoLevel);
		shipCargoText.text = NumberFormatter.Format(Mathf.RoundToInt(planetData.CurrentShipCargo * managerController.GetCargoMultiplier(openPlanetId)));
		shipCargoUpdatePriceText.text = NumberFormatter.FormatMoney(planetData.ShipCargoUpdatePrice);

		miningController.TryGetMiningData(openPlanetId, out data);
		foreach (ResourceMiningData pmd in data.MineDatas)
		{
			ResourceDataSetting resData = resourceSettings.GetResourceData(pmd.Type);
			float rate = data.TotalMineRate * mining * pmd.Ratio;
			if (!infoItems.ContainsKey(pmd.Type))
			{
				infoItems.Add(pmd.Type, Instantiate(miningInfoItemPrototype, miningInfoPanel.transform));
			}

			infoItems[pmd.Type].Initialize(pmd, resData.Icon, resData.Name, pmd.Ratio, rate, pmd.MinedAmount);
		}

		RefreshManager();
	}

	private void RefreshManager()
	{
		if (managerText == null)
		{
			return;
		}

		ManagerData manager = managerController.GetPlanetManager(openPlanetId);
		managerText.text = manager != null
			? $"{manager.Stars} Star  {managerController.GetPrimaryText(manager)}"
			: "No manager assigned";

		if (managerButton != null)
		{
			TMP_Text label = managerButton.GetComponentInChildren<TMP_Text>(true);
			if (label != null)
			{
				label.text = manager != null ? "Change" : "Assign";
			}
		}
	}

	private void OnManagerButtonClick()
	{
		signalBus.Fire(new ManagerAssignRequestSignal() { PlanetId = openPlanetId });
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
