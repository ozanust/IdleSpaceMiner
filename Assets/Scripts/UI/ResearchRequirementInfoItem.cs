using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchRequirementInfoItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text holdingAmountText;
    [SerializeField] private TMP_Text neededAmountText;

    // V2
    // implement icon image onClick method to show what the icon means (eg. iron bar)

    public void Initialize(Sprite icon, string holdingAmount, string neededAmount)
	{
        iconImage.sprite = icon;
        holdingAmountText.text = holdingAmount;
        neededAmountText.text = neededAmount;
	}
}
