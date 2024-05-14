using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SmeltRecipeItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text durationText;

	AlloyType alloyType;
	public Action<AlloyType> OnClick;

	private void Awake()
	{
		button.onClick.AddListener(OnClickButton);
	}

	public void SetTitle(string title)
	{
        titleText.text = title;
	}

    public void SetDuration(int smeltDuration)
	{
        durationText.text = smeltDuration.ToString() + "s";
	}

	public void SetAlloyType(AlloyType alloyType)
	{
		this.alloyType = alloyType;
	}

	private void OnClickButton()
	{
		OnClick?.Invoke(alloyType);
	}
}
