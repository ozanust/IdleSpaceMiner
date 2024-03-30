using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResourceBox : MonoBehaviour
{
    [SerializeField] TMP_Text resourceNameText;
    [SerializeField] TMP_Text resourceAmountText;
    [SerializeField] Image resourceIconImage;
    private ResourceType type;

    public ResourceType Type => type;

    public void Initialize(ResourceType type, string name, int amount, Sprite icon)
	{
        this.type = type;
        resourceNameText.text = name;
        resourceAmountText.text = amount.ToString();
        resourceIconImage.sprite = icon;
	}

    public void UpdateAmount(int amount)
	{
        resourceAmountText.text = amount.ToString();
	}
}
