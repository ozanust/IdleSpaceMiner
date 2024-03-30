using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MiningInfoItem : MonoBehaviour
{
    [SerializeField] private Image planetImage;
    [SerializeField] private TMP_Text resourceNameText;
    [SerializeField] private TMP_Text yieldPercentageText;
    [SerializeField] private TMP_Text miningRateText;
    [SerializeField] private TMP_Text minedAmountText;

    private PlanetMineData data;

	private void Update()
	{
        UpdateMinedAmount(data.MinedAmount);
	}

	public void Initialize(PlanetMineData data, Sprite planetIcon, string planetName, float yieldPercentage, float miningRate, float minedAmount)
	{
        this.data = data;
        planetImage.sprite = planetIcon;
        resourceNameText.text = planetName;
        yieldPercentageText.text = Mathf.RoundToInt(yieldPercentage * 100).ToString() + "%";
        miningRateText.text = miningRate.ToString("F2") + "/sec";
        minedAmountText.text = Mathf.FloorToInt(minedAmount).ToString();
        gameObject.SetActive(true);
	}

    public void UpdateMinedAmount(float minedAmount)
	{
        minedAmountText.text = Mathf.FloorToInt(minedAmount).ToString();
    }

	public void UpdateMiningRate(float miningRate)
	{
        miningRateText.text = miningRate.ToString("F2") + "/sec";
    }
}
