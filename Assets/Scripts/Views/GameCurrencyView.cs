using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

public class GameCurrencyView : MonoBehaviour
{
	[Inject] IPlayerModel playerModel;
	[Inject] SignalBus signalBus;
	[Inject] IMothershipController mothershipController;

	[SerializeField] private TMP_Text moneyText;
	[SerializeField] private TMP_Text darkMatterText;
	[SerializeField] private TMP_Text specialCurrencyText;
	[SerializeField] private TMP_Text galaxyValueText;

	private float nextGalaxyValueUpdate;

	private void Awake()
	{
		signalBus.Subscribe<PlayerMoneyUpdatedSignal>(OnCurrencyUpdated);
		signalBus.Subscribe<PlayerDarkMatterUpdatedSignal>(OnCurrencyUpdated);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<PlayerMoneyUpdatedSignal>(OnCurrencyUpdated);
		signalBus.Unsubscribe<PlayerDarkMatterUpdatedSignal>(OnCurrencyUpdated);
	}

	private void Start()
	{
		InitializeResourceView();
	}

	void InitializeResourceView()
	{
		OnCurrencyUpdated();
	}

	// Polled rather than tied to purchase signals, which can arrive before the value is updated
	private void Update()
	{
		if (Time.unscaledTime >= nextGalaxyValueUpdate)
		{
			nextGalaxyValueUpdate = Time.unscaledTime + 0.5f;
			galaxyValueText.text = "GV " + NumberFormatter.FormatMoney(mothershipController.GetGalaxyValue());
		}
	}

	private void OnCurrencyUpdated()
	{
		moneyText.text = NumberFormatter.Format(playerModel.GetMoney());
		darkMatterText.text = playerModel.GetCurrency(CurrencyType.DarkMatter).ToString();
		specialCurrencyText.text = playerModel.GetCurrency(CurrencyType.Special).ToString();
	}
}
