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

	[SerializeField] private TMP_Text moneyText;
	[SerializeField] private TMP_Text darkMatterText;
	[SerializeField] private TMP_Text specialCurrencyText;
	[SerializeField] private TMP_Text galaxyValueText;

	private void Awake()
	{
		signalBus.Subscribe<PlayerMoneyUpdatedSignal>(OnCurrencyUpdated);
		signalBus.Subscribe<PlayerDarkMatterUpdatedSignal>(OnCurrencyUpdated);
	}

	private void Start()
	{
		InitializeResourceView();
	}

	void InitializeResourceView()
	{
		OnCurrencyUpdated();
	}

	private void OnCurrencyUpdated()
	{
		moneyText.text = playerModel.GetMoney().ToString();
		darkMatterText.text = playerModel.GetCurrency(CurrencyType.DarkMatter).ToString();
		specialCurrencyText.text = playerModel.GetCurrency(CurrencyType.Special).ToString();
	}
}
